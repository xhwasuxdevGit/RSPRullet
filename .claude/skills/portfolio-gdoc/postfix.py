"""HTML 변환 직후 문서에 남는 서식 차이를 보정하는 update_doc 요청을 만든다.

사용: python postfix.py <read_doc 결과 JSON> [style-spec.json] > req.json
- 입력은 Docs read_doc 결과를 저장한 파일({"content": {...}} 또는 문서 JSON 그대로).
- 출력은 update_doc의 requests 배열과 writeControl. 스타일 요청을 먼저, 삽입은 뒤에서 앞으로 둔다.
- 처리 항목: 페이지 없음, 단락 간격 모드, 전체 글꼴, 문서 제목(TITLE), 제목 간격,
  하위 목록 들여쓰기, 표 열 너비·머리행 고정, 구분선 단락 분리, 표 뒤 빈 줄.
"""
import json
import os
import sys

here = os.path.dirname(os.path.abspath(__file__))
doc = json.load(open(sys.argv[1], encoding="utf-8"))
doc = doc.get("content", doc)
spec = json.load(open(sys.argv[2] if len(sys.argv) > 2 else os.path.join(here, "style-spec.json"), encoding="utf-8"))
body = doc["tabs"][0]["documentTab"]["body"]["content"] if "tabs" in doc else doc["body"]["content"]
end = body[-1]["endIndex"]


def pt(v):
    return {"magnitude": v, "unit": "PT"}


def rgb(h):
    return {"color": {"rgbColor": {"red": round(int(h[0:2], 16) / 255, 4), "green": round(int(h[2:4], 16) / 255, 4), "blue": round(int(h[4:6], 16) / 255, 4)}}}


def text(p):
    return "".join(e.get("textRun", {}).get("content", "") for e in p["elements"])


def para_style(s, e, st, keep=None):
    ps = {"lineSpacing": st["line"], "spaceAbove": pt(st["before"]), "spaceBelow": pt(st["after"])}
    fields = "lineSpacing,spaceAbove,spaceBelow"
    if keep is not None:
        ps["keepWithNext"] = keep
        fields += ",keepWithNext"
    return {"updateParagraphStyle": {"range": {"startIndex": s, "endIndex": e}, "paragraphStyle": ps, "fields": fields}}


R = [
    {"updateDocumentStyle": {"documentStyle": {"documentFormat": {"documentMode": spec["doc"]["mode"]}}, "fields": "documentFormat.documentMode"}},
    {"updateParagraphStyle": {"range": {"startIndex": 1, "endIndex": end}, "paragraphStyle": {"spacingMode": spec["doc"]["spacing_mode"]}, "fields": "spacingMode"}},
    {"updateTextStyle": {"range": {"startIndex": 1, "endIndex": end - 1}, "textStyle": {"weightedFontFamily": {"fontFamily": spec["doc"]["font"]}}, "fields": "weightedFontFamily"}},
]
inserts = []  # (위치, 종류)
# 이미 TITLE 단락이 있으면(보정을 다시 돌리는 경우) 첫 제목1을 TITLE로 바꾸지 않는다
title_done = any("paragraph" in el and el["paragraph"]["paragraphStyle"].get("namedStyleType") == "TITLE" for el in body)
nested = []
for i, el in enumerate(body):
    if "table" in el:
        t = el["table"]
        si = el["startIndex"]
        n = t["columns"]
        widths = spec["table"]["widths"].get(str(n))
        if not widths:
            cur = [c.get("width", {}).get("magnitude", 1) for c in t["tableStyle"]["tableColumnProperties"]]
            widths = [round(w * spec["table"]["width_total"] / sum(cur)) for w in cur]
        for ci, w in enumerate(widths):
            R.append({"updateTableColumnProperties": {"tableStartLocation": {"index": si}, "columnIndices": [ci], "tableColumnProperties": {"widthType": "FIXED_WIDTH", "width": pt(w)}, "fields": "width,widthType"}})
        R.append({"pinTableHeaderRows": {"tableStartLocation": {"index": si}, "pinnedHeaderRowsCount": 1}})
        nxt = body[i + 1] if i + 1 < len(body) else None
        if nxt and "paragraph" in nxt and text(nxt["paragraph"]) != "\n":
            inserts.append((nxt["startIndex"], "spacer"))
        continue
    if "paragraph" not in el:
        continue
    p = el["paragraph"]
    s, e = el["startIndex"], el["endIndex"]
    st = p["paragraphStyle"].get("namedStyleType")
    if any("horizontalRule" in x for x in p["elements"]) and text(p).strip():
        inserts.append((s, "hr"))
    if st == "HEADING_1" and not title_done:
        title_done = True
        ts = spec["title"]
        R.append({"updateParagraphStyle": {"range": {"startIndex": s, "endIndex": e}, "paragraphStyle": {"namedStyleType": "TITLE"}, "fields": "namedStyleType"}})
        R.append({"updateTextStyle": {"range": {"startIndex": s, "endIndex": e - 1}, "textStyle": {"fontSize": pt(ts["size"]), "bold": True, "foregroundColor": rgb(ts["color"])}, "fields": "fontSize,bold,foregroundColor"}})
        R.append(para_style(s, e, ts, True))
    elif st in ("HEADING_1", "HEADING_2"):
        R.append(para_style(s, e, spec["h1" if st == "HEADING_1" else "h2"], True))
    if "bullet" in p:
        lv = p["bullet"].get("nestingLevel", 0)
        if lv > 0:
            if nested and nested[-1][1] == s and nested[-1][2] == lv:
                nested[-1][1] = e
            else:
                nested.append([s, e, lv])
for s, e, lv in nested:
    a, f = spec["list"]["indent"][min(lv, 2)]
    R.append({"updateParagraphStyle": {"range": {"startIndex": s, "endIndex": e}, "paragraphStyle": {"indentStart": pt(a), "indentFirstLine": pt(f)}, "fields": "indentStart,indentFirstLine"}})

# 삽입은 뒤에서 앞으로. 구분선: s+1(구분선 뒤 분리) → s(앞 빈 줄)
for pos, kind in sorted(inserts, reverse=True):
    if kind == "hr":
        R.append({"insertText": {"location": {"index": pos + 1}, "text": "\n"}})
    R.append({"insertText": {"location": {"index": pos}, "text": "\n"}})
shift = 0
sp = spec["spacer"]
for pos, kind in sorted(inserts):
    n = pos + shift
    length = 3 if kind == "hr" else 1
    rng = {"startIndex": n, "endIndex": n + length}
    R.append({"deleteParagraphBullets": {"range": rng}})
    R.append({"updateParagraphStyle": {"range": rng, "paragraphStyle": {"namedStyleType": "NORMAL_TEXT"}, "fields": "namedStyleType"}})
    R.append({"updateParagraphStyle": {"range": rng, "paragraphStyle": {"lineSpacing": sp["line"], "spaceAbove": pt(sp["before"]), "spaceBelow": pt(sp["after"]), "indentStart": pt(0), "indentFirstLine": pt(0), "keepWithNext": False}, "fields": "lineSpacing,spaceAbove,spaceBelow,indentStart,indentFirstLine,keepWithNext"}})
    shift += 2 if kind == "hr" else 1

json.dump({"requests": R, "writeControl": {"requiredRevisionId": doc["revisionId"]}}, sys.stdout, ensure_ascii=False, separators=(",", ":"))

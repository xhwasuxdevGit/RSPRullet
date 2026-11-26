"""HTML 변환 직후 문서에 남는 서식 차이를 보정하는 update_doc 요청을 만든다.

사용: python postfix.py <read_doc 결과 JSON> [style-spec.json] [--extra-only] [--bold-src 원고.html] [--max N] > req.json
- --max N: 한 번에 보낼 요청을 N개 이하로 자른다. 남은 요청은 문서를 다시 읽고 다시 실행하면 나온다
  (이미 적용된 값은 빠지므로 같은 명령을 남은 수가 0이 될 때까지 반복).
- 입력은 Docs read_doc 결과를 저장한 파일({"content": {...}} 또는 문서 JSON 그대로).
- 출력은 update_doc의 requests 배열과 writeControl. 스타일 요청을 먼저, 삽입은 뒤에서 앞으로 둔다.
- 처리 항목: 페이지 없음, 단락 간격 모드, 전체 글꼴, 문서 제목(TITLE),
  표 열 너비·머리행 고정, 구분선 단락 분리, 표 뒤 빈 줄.
- 문서 현재 값과 같은 스타일·표 요청은 빼고 출력한다(판단할 수 없으면 남긴다).
"""
import json
import os
import sys

sys.stderr.reconfigure(encoding="utf-8")

here = os.path.dirname(os.path.abspath(__file__))
# 위치 인자만 고른다(옵션과 옵션 값은 제외)
VALUE_OPTS = ("--bold-src", "--max")
args = [a for i, a in enumerate(sys.argv[1:], 1) if not a.startswith("--") and sys.argv[i - 1] not in VALUE_OPTS]
doc = json.load(open(args[0], encoding="utf-8"))
doc = doc.get("content", doc)
spec = json.load(open(args[1] if len(args) > 1 else os.path.join(here, "style-spec.json"), encoding="utf-8"))
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
for i, el in enumerate(body):
    if "table" in el:
        t = el["table"]
        si = el["startIndex"]
        n = t["columns"]
        widths = spec["table"]["widths"].get(str(n))
        if not widths:
            cur = [c.get("width", {}).get("magnitude", 1) for c in t["tableStyle"]["tableColumnProperties"]]
            widths = [round(w * spec["table"]["width_total"] / sum(cur)) for w in cur]
        # 같은 너비 열은 한 요청으로 묶는다. 모든 열이 같으면 columnIndices를 빼서 표 전체에 적용
        groups = {}
        for ci, w in enumerate(widths):
            groups.setdefault(w, []).append(ci)
        for w, cols in groups.items():
            req = {"tableStartLocation": {"index": si}, "tableColumnProperties": {"widthType": "FIXED_WIDTH", "width": pt(w)}, "fields": "width,widthType"}
            if len(cols) < n:
                req["columnIndices"] = cols
            R.append({"updateTableColumnProperties": req})
        R.append({"pinTableHeaderRows": {"tableStartLocation": {"index": si}, "pinnedHeaderRowsCount": 1}})
        nxt = body[i + 1] if i + 1 < len(body) else None
        # 다음 단락이 구분선이면 구분선 처리에서 빈 줄이 생기므로 표 뒤 빈 줄을 따로 넣지 않는다
        if nxt and "paragraph" in nxt and text(nxt["paragraph"]) != "\n" and not any("horizontalRule" in x for x in nxt["paragraph"]["elements"]):
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
# 장·절 제목 간격과 목록 들여쓰기는 아래 추가 보정(X)에서 한 번만 만든다

# ── 추가 보정 (HTML로 안 되는 것) ──────────────────────────────
# 위치가 바뀌지 않는 요청만 쓴다(같은 길이 치환, 스타일). 삽입보다 앞에 둔다.
X = []
toc = False  # "목차" 제목부터 첫 번호 장 제목 전까지만 목차 구간
prev = None
last_lv = None  # 직전 글머리 항목의 단계(연속 단락 들여쓰기용)
HSPEC = {"TITLE": "title", "HEADING_1": "h1", "HEADING_2": "h2"}
for el in body:
    if "table" in el:
        # 표 글자 크기
        X.append({"updateTextStyle": {"range": {"startIndex": el["startIndex"], "endIndex": el["endIndex"]}, "textStyle": {"fontSize": pt(spec["table"]["cell_size"])}, "fields": "fontSize"}})
        # 셀 안 단락: 줄간격·앞뒤 간격. 머리행은 1.0·앞뒤 0(세로 가운데 정렬 유지), 본문 행은 1.5·앞 간격 = 셀 글자 크기
        T = spec["table"]
        rows = el["table"]["tableRows"]
        X.append({"updateParagraphStyle": {"range": {"startIndex": rows[0]["tableCells"][0]["content"][0]["startIndex"], "endIndex": rows[0]["tableCells"][-1]["content"][-1]["endIndex"]}, "paragraphStyle": {"lineSpacing": T["cell_line"], "spaceAbove": pt(T["cell_before"]), "spaceBelow": pt(T["cell_after"])}, "fields": "lineSpacing,spaceAbove,spaceBelow"}})
        if len(rows) > 1:
            X.append({"updateParagraphStyle": {"range": {"startIndex": rows[1]["tableCells"][0]["content"][0]["startIndex"], "endIndex": rows[-1]["tableCells"][-1]["content"][-1]["endIndex"]}, "paragraphStyle": {"lineSpacing": T["body_cell_line"], "spaceAbove": pt(T["body_cell_before"]), "spaceBelow": pt(T["body_cell_after"])}, "fields": "lineSpacing,spaceAbove,spaceBelow"}})
        # 표 안 <b>는 변환에서 사라지므로 머리행·첫 열을 굵게
        for ri, row in enumerate(el["table"]["tableRows"]):
            for ci, cell in enumerate(row["tableCells"]):
                if ri == 0 or ci == 0:
                    for ce in cell["content"]:
                        if "paragraph" in ce and ce["endIndex"] - 1 > ce["startIndex"]:
                            X.append({"updateTextStyle": {"range": {"startIndex": ce["startIndex"], "endIndex": ce["endIndex"] - 1}, "textStyle": {"bold": True}, "fields": "bold"}})
        prev = None
        last_lv = None
        continue
    if "paragraph" not in el:
        continue
    p = el["paragraph"]
    st = p["paragraphStyle"].get("namedStyleType")
    t = text(p)
    s, e = el["startIndex"], el["endIndex"]
    if st == "HEADING_1":
        toc = t.strip().startswith(spec["toc"]["heading"]) and not any(ch.isdigit() for ch in t)
    if st in HSPEC:  # 제목 간격 = 제목 글자 크기
        X.append(para_style(s, e, spec[HSPEC[st]], True))
        # HTML 변환이 제목 글자에 굵게 해제를 박아 두므로 다시 굵게
        if spec[HSPEC[st]].get("bold") and e - 1 > s:
            X.append({"updateTextStyle": {"range": {"startIndex": s, "endIndex": e - 1}, "textStyle": {"bold": True}, "fields": "bold"}})
    # 절 제목 바로 앞 빈 줄은 절 제목 크기
    if st == "HEADING_2" and prev is not None and not text(prev["paragraph"]).strip():
        X.append({"updateTextStyle": {"range": {"startIndex": prev["startIndex"], "endIndex": prev["endIndex"]}, "textStyle": {"fontSize": pt(spec["h2"]["size"])}, "fields": "fontSize"}})
    # 목록 들여쓰기: 글머리 항목은 단계별 값, 바로 뒤 연속 단락(글머리 없음·들여쓰기 있음)은 그 항목의 글자 위치
    ind0 = p["paragraphStyle"].get("indentStart", {}).get("magnitude", 0)
    if "bullet" in p:
        last_lv = p["bullet"].get("nestingLevel", 0)
        a, f = spec["list"]["indent"][min(last_lv, 2)]
        X.append({"updateParagraphStyle": {"range": {"startIndex": s, "endIndex": e}, "paragraphStyle": {"indentStart": pt(a), "indentFirstLine": pt(f)}, "fields": "indentStart,indentFirstLine"}})
    elif st == "NORMAL_TEXT" and t.strip() and last_lv is not None and ind0 > 0:
        a = spec["list"]["indent"][min(last_lv, 2)][0]
        X.append({"updateParagraphStyle": {"range": {"startIndex": s, "endIndex": e}, "paragraphStyle": {"indentStart": pt(a), "indentFirstLine": pt(a)}, "fields": "indentStart,indentFirstLine"}})
    else:
        last_lv = None
    if st == "NORMAL_TEXT" and t.strip() and not toc and not any("horizontalRule" in x for x in p["elements"]):
        # 본문·목록 앞뒤 간격 = 글자 크기
        if True:
            size = next((x["textRun"]["textStyle"].get("fontSize", {}).get("magnitude") for x in p["elements"] if "textRun" in x and x["textRun"]["content"].strip() and x["textRun"]["textStyle"].get("fontSize")), spec["body"]["size"])
            X.append({"updateParagraphStyle": {"range": {"startIndex": s, "endIndex": e}, "paragraphStyle": {"spaceAbove": pt(size), "spaceBelow": pt(size)}, "fields": "spaceAbove,spaceBelow"}})
        # 마침표 단락 분리: ". " 의 공백을 줄바꿈으로(같은 길이). 목록이면 뒤 문장은 글머리 없는 연속 단락
        cuts = []
        quoted = t.lstrip().startswith(('"', "“"))  # 따옴표로 시작하는 문구(슬로건)는 나누지 않는다
        for x in ([] if quoted else p["elements"]):
            tr = x.get("textRun")
            if not tr:
                continue
            c = tr["content"]
            for k in range(len(c) - 2):
                if c[k] == "." and c[k + 1] == " " and not c[k + 2].isspace():
                    cuts.append(x["startIndex"] + k + 1)
        for pos in cuts:
            X.append({"deleteContentRange": {"range": {"startIndex": pos, "endIndex": pos + 1}}})
            X.append({"insertText": {"location": {"index": pos}, "text": "\n"}})
        if "bullet" in p and cuts:
            lv = p["bullet"].get("nestingLevel", 0)
            ind = spec["list"]["indent"][min(lv, 2)][0]
            X.append({"deleteParagraphBullets": {"range": {"startIndex": cuts[0] + 1, "endIndex": e}}})
            X.append({"updateParagraphStyle": {"range": {"startIndex": cuts[0] + 1, "endIndex": e}, "paragraphStyle": {"indentStart": pt(ind), "indentFirstLine": pt(ind)}, "fields": "indentStart,indentFirstLine"}})
    prev = el
# 굵게 복원: HTML 변환에서 <b>가 사라지므로 원고의 <b> 문구를 문서 순서대로 찾아 굵게 한다
if "--bold-src" in sys.argv:
    import html as _html
    import re as _re
    src = open(sys.argv[sys.argv.index("--bold-src") + 1], encoding="utf-8").read()
    src = src[src.find("<body"):]
    phrases = [_html.unescape(_re.sub(r"<[^>]+>", "", m)).strip() for m in _re.findall(r"<b>(.*?)</b>", src, _re.S)]
    chars = []  # (문자, 문서 위치)

    def walk(content):
        for el in content:
            if "paragraph" in el:
                for x in el["paragraph"]["elements"]:
                    tr = x.get("textRun")
                    if tr:
                        chars.extend((ch, x["startIndex"] + k) for k, ch in enumerate(tr["content"]))
            elif "table" in el:
                for row in el["table"]["tableRows"]:
                    for cell in row["tableCells"]:
                        walk(cell["content"])
    walk(body)
    linear = "".join(ch for ch, _ in chars)
    cur = 0
    missed = []
    for ph in phrases:
        if not ph:
            continue
        k = linear.find(ph, cur)
        if k < 0:
            missed.append(ph)
            continue
        X.append({"updateTextStyle": {"range": {"startIndex": chars[k][1], "endIndex": chars[k + len(ph) - 1][1] + 1}, "textStyle": {"bold": True}, "fields": "bold"}})
        cur = k + len(ph)
    if missed:
        sys.stderr.write("굵게 위치를 못 찾음: %s\n" % missed)

# 압축: 굵게 범위 중복 제거·병합, 같은 단락 스타일이 이어지는 범위 병합 (요청 수·크기 줄이기)
def _key(r):
    k = list(r)[0]
    v = dict(r[k])
    v.pop("range", None)
    return k, json.dumps(v, sort_keys=True)


bold = sorted({(r["updateTextStyle"]["range"]["startIndex"], r["updateTextStyle"]["range"]["endIndex"]) for r in X if "updateTextStyle" in r and r["updateTextStyle"]["fields"] == "bold"})
merged_bold = []
for s0, e0 in bold:
    # 간격이 3자 이하면 이어 붙인다(셀 경계·줄바꿈 같은 구조 문자뿐이라 다른 글자가 굵어지지 않음)
    if merged_bold and s0 <= merged_bold[-1][1] + 3:
        merged_bold[-1][1] = max(merged_bold[-1][1], e0)
    else:
        merged_bold.append([s0, e0])
rest = [r for r in X if not ("updateTextStyle" in r and r["updateTextStyle"]["fields"] == "bold")]
out = []
last = {}  # 같은 단락 스타일 요청의 마지막 것. 다른 요청이 사이에 끼어 있어도 단락이 이어지면 합친다
for r in rest:
    k = list(r)[0]
    if k == "updateParagraphStyle":
        key = _key(r)
        prev_r = last.get(key)
        if prev_r and prev_r[k]["range"]["endIndex"] == r[k]["range"]["startIndex"]:
            prev_r[k]["range"]["endIndex"] = r[k]["range"]["endIndex"]
            continue
        last[key] = r
    out.append(r)
X = out + [{"updateTextStyle": {"range": {"startIndex": s0, "endIndex": e0}, "textStyle": {"bold": True}, "fields": "bold"}} for s0, e0 in merged_bold]

if "--extra-only" in sys.argv:
    R = X
    inserts = []
else:
    R.extend(X)

# 이미 문서 값과 같은 스타일 요청은 빼서 보낼 양을 줄인다.
# 판단할 수 없는 필드가 있거나, 같은 묶음의 글자 삽입·삭제 위치를 포함하는 요청은 그대로 둔다.
named = {s["namedStyleType"]: s for s in (doc["tabs"][0]["documentTab"] if "tabs" in doc else doc).get("namedStyles", {}).get("styles", [])}
paras = []  # (시작, 끝, 단락) — 표 안 단락 포함


def _walk(content):
    for el in content:
        if "paragraph" in el:
            paras.append((el["startIndex"], el["endIndex"], el["paragraph"]))
        elif "table" in el:
            for row in el["table"]["tableRows"]:
                for cell in row["tableCells"]:
                    _walk(cell["content"])


_walk(body)
tables = {el["startIndex"]: el["table"] for el in body if "table" in el}
edits = [r["insertText"]["location"]["index"] for r in R if "insertText" in r] + [r["deleteContentRange"]["range"]["startIndex"] for r in R if "deleteContentRange" in r]


def _norm(v):
    # 크기는 소수 둘째 자리, 색은 0~255 정수로 맞춰 비교
    if isinstance(v, dict):
        if "magnitude" in v or "unit" in v:
            return round(v.get("magnitude", 0), 2)
        if "rgbColor" in v:
            c = v["rgbColor"]
            return tuple(round(c.get(k, 0) * 255) for k in ("red", "green", "blue"))
        if "fontFamily" in v:
            return v["fontFamily"]
        return {k: _norm(x) for k, x in v.items()}
    return v


def _cur(own, kind, ptype, field):
    # 단락·글자에 직접 지정된 값 → 단락의 이름 스타일 → 본문 스타일 순으로 찾는다
    for src in (own, named.get(ptype, {}).get(kind, {}), named.get("NORMAL_TEXT", {}).get(kind, {})):
        if field in src:
            return _norm(src[field])
    return {"bold": False, "fontSize": 11}.get(field)  # Docs 기본값: 굵게 아님, 11pt


def _same(r):
    k = list(r)[0]
    if k == "updateDocumentStyle":
        return (doc["tabs"][0]["documentTab"] if "tabs" in doc else doc).get("documentStyle", {}).get("documentFormat", {}).get("documentMode") == r[k]["documentStyle"]["documentFormat"]["documentMode"]
    if k in ("updateTableColumnProperties", "pinTableHeaderRows"):
        t = tables.get(r[k]["tableStartLocation"]["index"])
        if not t:
            return False
        if k == "pinTableHeaderRows":
            return r[k]["pinnedHeaderRowsCount"] == 1 and t["tableRows"][0].get("tableRowStyle", {}).get("tableHeader", False) and not (len(t["tableRows"]) > 1 and t["tableRows"][1].get("tableRowStyle", {}).get("tableHeader", False))
        cps = t["tableStyle"]["tableColumnProperties"]
        want = r[k]["tableColumnProperties"]
        return all(cps[ci].get("widthType") == want["widthType"] and _norm(cps[ci].get("width", {})) == _norm(want["width"]) for ci in r[k].get("columnIndices", range(len(cps))))
    if k not in ("updateParagraphStyle", "updateTextStyle"):
        return False
    s, e = r[k]["range"]["startIndex"], r[k]["range"]["endIndex"]
    if any(s <= x <= e for x in edits):
        return False
    want = r[k]["paragraphStyle" if k == "updateParagraphStyle" else "textStyle"]
    fields = r[k]["fields"].split(",")
    if any("." in f for f in fields):
        return False
    hit = [p for p in paras if p[0] < e and s < p[1]]
    if not hit:
        return False
    for ps, pe, p in hit:
        ptype = p["paragraphStyle"].get("namedStyleType", "NORMAL_TEXT")
        if k == "updateParagraphStyle":
            own = [(p["paragraphStyle"], "paragraphStyle")]
        else:  # 범위에 걸친 글자 조각마다 확인 (줄바꿈만 있는 조각은 제외. 단, 빈 줄 한 칸만 겨냥한 요청은 그 줄바꿈으로 확인)
            runs = [x for x in p["elements"] if "textRun" in x and x["startIndex"] < e and s < x["endIndex"]]
            if e - s > 1:
                runs = [x for x in runs if x["textRun"]["content"].strip("\n")]
            own = [(x["textRun"].get("textStyle", {}), "textStyle") for x in runs]
        for st, kind in own:
            for f in fields:
                w = _norm(want[f]) if f in want else None
                if _cur(st, kind, ptype, f) != w:
                    return False
    return True


before = len(R)
R = [r for r in R if not _same(r)]
sys.stderr.write("이미 같은 값이라 뺀 요청: %d / %d\n" % (before - len(R), before))

# 삽입은 뒤에서 앞으로. 구분선: s+1(구분선 뒤 분리) → s(앞 빈 줄)
tail_start = len(R)  # 여기부터는 위치가 바뀌는 삽입 묶음이라 나누지 않고 항상 마지막에 보낸다
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

if "--max" in sys.argv:
    cap = int(sys.argv[sys.argv.index("--max") + 1])
    head, tail = R[:tail_start], R[tail_start:]
    if len(head) + len(tail) <= cap or len(head) == 0:
        rest = 0
    else:
        # 마침표 분리 묶음(삭제·삽입 쌍, 뒤따르는 글머리 해제·들여쓰기) 중간에서는 자르지 않는다.
        # 나뉘면 다음 실행에서 ". "가 없어 글머리 해제가 다시 나오지 않는다.
        cut = min(cap, len(head))
        glued = ("deleteContentRange", "insertText", "deleteParagraphBullets")
        while cut > 0 and (list(head[cut - 1])[0] in glued or (cut < len(head) and list(head[cut])[0] in glued[1:])):
            cut -= 1
        R = head[:cut]
        rest = len(head) - cut + len(tail)
    sys.stderr.write("남은 요청: %d%s\n" % (rest, " (보낸 뒤 read_doc → 같은 명령 다시 실행)" if rest else ""))

json.dump({"requests": R, "writeControl": {"requiredRevisionId": doc["revisionId"]}}, sys.stdout, ensure_ascii=False, separators=(",", ":"))

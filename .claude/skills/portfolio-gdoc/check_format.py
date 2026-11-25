"""구글 문서를 docx로 내보낸 파일을 style-spec.json과 비교해 다른 항목만 출력한다.

사용: python -I check_format.py <내보낸 docx> [style-spec.json]
- 문제가 없으면 "OK" 한 줄과 요약만 나온다. 항목별 예시는 최대 3개만 보여준다.
- Google의 docx 내보내기 기준: 간격 twip(1/20pt), 줄간격 line/240, 글자 크기 half-point.
"""
import html
import json
import os
import re
import sys
import zipfile
from collections import defaultdict

sys.stdout.reconfigure(encoding="utf-8")
here = os.path.dirname(os.path.abspath(__file__))
spec = json.load(open(sys.argv[2] if len(sys.argv) > 2 else os.path.join(here, "style-spec.json"), encoding="utf-8"))
x = zipfile.ZipFile(sys.argv[1]).read("word/document.xml").decode("utf-8")
bad = defaultdict(list)
count = defaultdict(int)


def txt(p):
    return "".join(re.findall(r"<w:t[^>]*>([^<]*)</w:t>", p))[:20]


def attr(p, name, default=0.0):
    m = re.search(r'<w:spacing [^>]*w:%s="(\d+)"' % name, p)
    return float(m.group(1)) if m else default


def check(cat, p, key, got, want):
    if abs(got - want) > 0.5:
        bad[cat + " " + key].append("%r %s≠%s" % (txt(p), round(got, 1), want))


def fonts(p, cat):
    for f in set(re.findall(r'w:ascii="([^"]+)"', p)):
        if f != spec["doc"]["font"]:
            bad[cat + " 글꼴"].append("%r %s" % (txt(p), f))


# 표
T = spec["table"]
for ti, t in enumerate(re.findall(r"<w:tbl>.*?</w:tbl>", x, re.S)):
    rows = re.findall(r"<w:tr[ >].*?</w:tr>", t, re.S)
    for ri, row in enumerate(rows):
        cells = re.findall(r"<w:tc>.*?</w:tc>", row, re.S)
        for ci, tc in enumerate(cells):
            count["표 셀"] += 1
            pr = re.search(r"<w:tcPr>.*?</w:tcPr>", tc, re.S)
            pr = pr.group(0) if pr else ""
            b = {k: (c.lower(), int(s), v) for k, c, s, v in re.findall(r'<w:(top|left|bottom|right) w:color="(\w+)" w:space="\d+" w:sz="(\d+)" w:val="(\w+)"', pr)}
            want = {
                "top": T["outer_top_bottom"] if ri == 0 else T["inner"],
                "bottom": T["outer_top_bottom"] if ri == len(rows) - 1 else T["inner"],
                "left": None if ci == 0 else T["inner"],
                "right": None if ci == len(cells) - 1 else T["inner"],
            }
            for side, w in want.items():
                g = b.get(side)
                ok = (g is None or g[2] == "nil" or g[1] == 0) if w is None else (g is not None and g[0] == w.lower() and g[2] == "single" and g[1] == T["border_width"] * 8)
                if not ok:
                    bad["표%d 테두리 %s" % (ti + 1, side)].append("r%dc%d %s" % (ri, ci, g))
            fill = re.findall(r'w:fill="(\w+)"', pr)
            wf = T["header_bg"] if ri == 0 else T["body_bg"]
            if not fill or fill[0].lower() != wf.lower():
                bad["표%d 음영" % (ti + 1)].append("r%dc%d %s" % (ri, ci, fill))
            if "vAlign w:val=\"center\"" not in pr:
                bad["표%d 세로정렬" % (ti + 1)].append("r%dc%d" % (ri, ci))
            mar = dict(re.findall(r'<w:(top|left|bottom|right) w:w="([\d.]+)"', re.search(r"<w:tcMar>.*?</w:tcMar>", pr, re.S).group(0))) if "<w:tcMar>" in pr else {}
            if float(mar.get("top", 0)) != T["pad_top_bottom"] * 20 or float(mar.get("left", 0)) != T["pad_left_right"] * 20:
                bad["표%d 셀 여백" % (ti + 1)].append("r%dc%d %s" % (ri, ci, mar))
            for sz in re.findall(r'w:sz w:val="(\d+)"', tc):
                if int(sz) != T["cell_size"] * 2:
                    bad["표%d 글자 크기" % (ti + 1)].append("r%dc%d %s" % (ri, ci, int(sz) / 2))
                    break
            runs = [r for r in re.findall(r"<w:r[ >].*?</w:r>", tc, re.S) if txt(r).strip()]
            if (ri == 0 or ci == 0) and runs and not re.search(r'<w:b w:val="1"|<w:b/>', runs[0]):
                bad["표%d 굵게(머리행·첫 열)" % (ti + 1)].append("r%dc%d %r" % (ri, ci, txt(tc)))
            fonts(tc, "표")

# 단락
import unicodedata

W = spec["label_wrap"]


def em(s):
    return sum(W["wide"] if unicodedata.east_asian_width(c) in "WF" else (W["space"] if c == " " else W["narrow"]) for c in s)


body = re.sub(r"<w:tbl>.*?</w:tbl>", "", x, flags=re.S)
listbefore = defaultdict(int)
toc = True
prev_sty, prev_blank, prev_p = None, False, None
# 절 제목 앞 빈 줄 검사 (표 뒤 빈 줄은 표 제거 후에도 남는 빈 단락으로 잡힌다)
for p in re.findall(r"<w:p[ >].*?</w:p>", body, re.S):
    s = re.search(r'<w:pStyle w:val="(\w+)"', p)
    s = s.group(1) if s else "Normal"
    blank = s == "Normal" and not txt(p).strip() and "<w:numPr>" not in p
    if s == "Heading2" and prev_sty not in ("Heading1", None) and not prev_blank:
        bad["절 제목 앞 빈 줄 누락"].append("%r" % txt(p))
    if s == "Heading2" and prev_blank and prev_p:
        m = re.search(r'<w:sz w:val="(\d+)"', prev_p)
        if not m or int(m.group(1)) != spec["h2"]["size"] * 2:
            bad["절 제목 앞 빈 줄 글자 크기(절 제목과 같아야 함)"].append("%r 앞 %s" % (txt(p), int(m.group(1)) / 2 if m else None))
    prev_sty, prev_blank, prev_p = s, blank and "<w:drawing" not in p, p
for p in re.findall(r"<w:p[ >].*?</w:p>", body, re.S):
    sty = re.search(r'<w:pStyle w:val="(\w+)"', p)
    sty = sty.group(1) if sty else "Normal"
    full = html.unescape("".join(re.findall(r"<w:t[^>]*>([^<]*)</w:t>", p)))
    if sty == "Heading1" and re.search(r"\d\.", full):
        toc = False
    if sty == "Heading1" and full.startswith(spec["toc"]["heading"]) and full.strip() != spec["toc"]["heading"]:
        bad["목차 제목"].append("%r (영문 표기 등 제거)" % full)
    if "[도식 삽입:" in full:
        bad["도식 미삽입(표시 남음)"].append("%r" % full[:40])
    jc = re.search(r'<w:jc w:val="(\w+)"', p)
    if jc and jc.group(1) not in ("left", "start", "both"):
        bad["정렬(왼쪽이어야 함)"].append("%r %s" % (full[:20], jc.group(1)))
    if sty == "Normal" and not toc and re.search(r"\.\s(?=\S)", full.replace("\n", " ")):
        bad["마침표 단락 분리 누락"].append("%r" % full[:20])
    if "<w:br" in p and sty == "Normal":
        bad["단락 안 줄바꿈(Shift+Enter) 남음"].append("%r" % full[:20])
    before, after, line = attr(p, "before") / 20, attr(p, "after") / 20, attr(p, "line", 240) / 240 * 100
    if sty in ("Title", "Heading1", "Heading2"):
        k = {"Title": "title", "Heading1": "h1", "Heading2": "h2"}[sty]
        count[k] += 1
        for key, got in (("before", before), ("after", after), ("line", line)):
            check(k, p, key, got, spec[k][key])
        continue
    fonts(p, "본문")
    if "<w:numPr>" in p:
        count["목록"] += 1
        lv = int(re.search(r'<w:ilvl w:val="(\d)"', p).group(1))
        a, f = spec["list"]["indent"][min(lv, 2)]
        ind = re.search(r'<w:ind w:left="(\d+)"[^>]*w:hanging="(\d+)"', p)
        if not ind or float(ind.group(1)) != a * 20 or float(ind.group(2)) != (a - f) * 20:
            bad["목록 들여쓰기 L%d" % lv].append("%r %s" % (txt(p), ind.groups() if ind else None))
        listbefore[before] += 1
        ref = spec["toc"] if toc else spec["list"]
        for key, got in (("before", before), ("after", after)):
            check("목차" if toc else "목록", p, key, got, ref[key])
        check("목록", p, "line", line, spec["list"]["line"])
        # 긴 "항목명: 내용"은 항목명과 내용을 줄바꿈으로 분리해야 한다
        m = re.match(r"([^:]{1,30}): ", full)
        cap = (W["text_width"] - a) / W["font_size"]
        if not toc and "<w:br" not in p and m and em(full) > cap:  # 줄바꿈은 <w:br w:type="textWrapping"/>
            bad["긴 항목 줄바꿈 누락"].append("%r" % m.group(1))
        continue
    if not txt(p).strip():
        count["빈 줄"] += 1
        continue
    count["본문"] += 1
    # 간격 = 그 단락 글자 크기 (첫 글자 run의 크기, 없으면 본문 크기)
    runs = [r for r in re.findall(r"<w:r[ >].*?</w:r>", p, re.S) if txt(r).strip()]  # 단락 기호(pPr)의 크기는 제외
    sz = re.search(r'<w:sz w:val="(\d+)"', runs[0]) if runs else None
    size = int(sz.group(1)) / 2 if sz else spec["body"]["size"]
    for key, got in (("before", before), ("after", after)):
        check("본문", p, key, got, size)
    check("본문", p, "line", line, spec["body"]["line"])

print("요약:", dict(count), "| 목록 위 간격 분포:", dict(listbefore))
if not bad:
    print("OK: 기준 값과 다른 항목 없음")
for k, v in bad.items():
    print("✗ %s (%d건): %s" % (k, len(v), "; ".join(v[:3])))

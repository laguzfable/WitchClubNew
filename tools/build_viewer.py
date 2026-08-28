# -*- coding: utf-8 -*-
"""把 Assets/NaniScripts 的內容烤進檢視器，產生「打開就有資料」的版本。

用法（在專案根目錄）：
    python tools/build_viewer.py

改過劇本之後重跑一次就好。原本那支 劇本檢視器.html 不會被動到，
它仍然是「拖資料夾進去」的通用版。
"""
import glob, io, json, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'tools', '劇本檢視器.html')
OUT = os.path.join(ROOT, 'tools', '劇本檢視器_已載入.html')
SCRIPTS = os.path.join(ROOT, 'Assets', 'NaniScripts', '*.nani')

data = {}
for path in sorted(glob.glob(SCRIPTS)):
    name = os.path.splitext(os.path.basename(path))[0]
    data[name] = io.open(path, encoding='utf-8-sig', errors='ignore').read()

html = io.open(SRC, encoding='utf-8').read()
blob = json.dumps(data, ensure_ascii=False).replace('</', r'<\/')  # 避免提早關掉 <script>
html = html.replace('const EMBEDDED = null;', 'const EMBEDDED = ' + blob + ';', 1)

io.open(OUT, 'w', encoding='utf-8', newline='').write(html)
print(f'{len(data)} 支腳本 → {OUT}')
print(f'{os.path.getsize(OUT) / 1024 / 1024:.1f} MB')

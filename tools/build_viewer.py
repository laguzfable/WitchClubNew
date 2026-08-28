# -*- coding: utf-8 -*-
"""把 Assets/NaniScripts 的內容烤進檢視器，產生「打開就有資料」的版本。

用法（在專案根目錄）：
    python tools/build_viewer.py

改過劇本之後重跑一次就好。原本那支 劇本檢視器.html 不會被動到，
它仍然是「拖資料夾進去」的通用版。
"""
import codecs, glob, io, json, os

BACKSLASH = chr(92)

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'tools', '劇本檢視器.html')
OUT = os.path.join(ROOT, 'tools', '劇本檢視器_已載入.html')
SCRIPTS = os.path.join(ROOT, 'Assets', 'NaniScripts', '*.nani')

data = {}
for path in sorted(glob.glob(SCRIPTS)):
    name = os.path.splitext(os.path.basename(path))[0]
    data[name] = io.open(path, encoding='utf-8-sig', errors='ignore').read()


def build_assets():
    """把 EditorResources 裡的背景與音樂對到實際檔案，路徑寫成 tools/ 的相對路徑。

    Naninovel 只記 guid，所以要掃一遍 .meta 才知道 guid 是哪個檔案。
    """
    import re

    guid_to_file = {}
    for root, _dirs, files in os.walk(os.path.join(ROOT, 'Assets')):
        for f in files:
            if not f.endswith('.meta'):
                continue
            full = os.path.join(root, f)
            try:
                text = io.open(full, encoding='utf-8', errors='ignore').read()
            except OSError:
                continue
            m = re.search(r'guid: (\w+)', text)
            if m:
                guid_to_file[m.group(1)] = full[:-5]

    res = io.open(os.path.join(ROOT, 'Assets', 'NaninovelData', 'EditorResources.asset'),
                  encoding='utf-8', errors='ignore').read()

    def unescape(x):
        # EditorResources 把中文寫成 uXXXX 逃脫碼，還原回來才對得上劇本裡的 @char
        x = x.strip('"')
        if BACKSLASH + 'u' in x:
            try:
                return codecs.decode(x, 'unicode_escape')
            except Exception:
                return x
        return x

    backs, audio, chars = {}, {}, set()
    pattern = r'- name: (\S+)\s+pathPrefix: (\S+)\s+guid: (\w+)'
    for m in re.finditer(pattern, res):
        name, prefix, guid = (unescape(g) for g in m.groups())
        path = guid_to_file.get(guid)
        if not path:
            continue
        rel = os.path.relpath(path, os.path.join(ROOT, 'tools')).replace(os.sep, '/')
        name = name.strip('"')
        if prefix.startswith('Backgrounds'):
            # 劇本裡 CG 是寫 @back CG/cg01，但 EditorResources 只記 cg01，
            # 前綴在 pathPrefix 那邊（Backgrounds/MainBackground/CG），要補回去
            sub = prefix.split('MainBackground/')[-1] if 'MainBackground/' in prefix else ''
            backs[(sub + '/' + name) if sub else name] = rel
        elif prefix == 'Audio':
            audio[name] = rel
        elif prefix == 'Characters':
            chars.add(name)
        elif prefix.startswith('Characters/'):
            # 多外觀的角色：id 在前綴後面，name 是外觀
            chars.add(prefix.split('Characters/', 1)[1])

    return {'backgrounds': backs, 'audio': audio, 'characters': sorted(chars)}


assets = build_assets()
print(f"背景 {len(assets['backgrounds'])} 張、音樂 {len(assets['audio'])} 首")

html = io.open(SRC, encoding='utf-8').read()
blob = json.dumps(data, ensure_ascii=False).replace('</', r'<\/')  # 避免提早關掉 <script>
html = html.replace('const EMBEDDED = null;', 'const EMBEDDED = ' + blob + ';', 1)
html = html.replace('const ASSETS = null;',
                    'const ASSETS = ' + json.dumps(assets, ensure_ascii=False) + ';', 1)

io.open(OUT, 'w', encoding='utf-8', newline='').write(html)
print(f'{len(data)} 支腳本 → {OUT}')
print(f'{os.path.getsize(OUT) / 1024 / 1024:.1f} MB')

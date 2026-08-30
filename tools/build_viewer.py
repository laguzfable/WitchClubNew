# -*- coding: utf-8 -*-
"""把 Assets/NaniScripts 的內容烤進檢視器，產生「打開就有資料」的版本。

用法（在專案根目錄）：
    python tools/build_viewer.py

改過劇本之後重跑一次就好。原本那支 劇本檢視器.html 不會被動到，
它仍然是「拖資料夾進去」的通用版。
"""
import codecs, glob, io, json, os

BACKSLASH = chr(92)
char_prefab = {}
audio_kind = {}

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

    backs, audio, chars, exprs = {}, {}, set(), {}
    audio_kind.clear()
    char_prefab.clear()
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
            # BGM 跟音效混在同一個 Audio 前綴底下，只有資料夾分得出來
            audio[name] = rel
            audio_kind[name] = 'bgm' if '/Sound/BGM/' in rel.replace(os.sep, '/') else 'sfx'
        elif prefix == 'Characters' or prefix.startswith('Characters/'):
            cid = name if prefix == 'Characters' else prefix.split('Characters/', 1)[1]
            chars.add(cid)
            # 表情＝角色 prefab 旁邊那些 .anim（Live2D 的動作檔）。
            # 靜圖角色（那些女巫 png）沒有表情可挑，就不要給選單。
            if path.endswith('.prefab'):
                char_prefab[cid] = path
                folder = os.path.dirname(path)
                exprs[cid] = sorted(f[:-5] for f in os.listdir(folder) if f.endswith('.anim'))

    return {'backgrounds': backs, 'audio': audio, 'audioKind': dict(audio_kind),
            'characters': sorted(chars), 'expressions': exprs}


def build_extra():
    """戰鬥用的圖跟 Naninovel 的資源是兩套：怪物和對戰背景走 Resources.Load，
    所以要另外掃 Assets/Resources。角色是 Live2D，沒有立繪可以預覽，
    只能拿 prefab 旁邊的貼圖集當個示意。"""
    def folder(sub, exts=('.png', '.jpg')):
        d = os.path.join(ROOT, 'Assets', 'Resources', sub)
        out = {}
        if os.path.isdir(d):
            for f in sorted(os.listdir(d)):
                if f.lower().endswith(exts):
                    out[os.path.splitext(f)[0]] = os.path.relpath(
                        os.path.join(d, f), os.path.join(ROOT, 'tools')).replace(os.sep, '/')
        return out

    atlas = {}
    for cid, path in char_prefab.items():
        # Live2D 的貼圖有兩種放法：資料夾裡的 TextureAtlas.png，
        # 或是 <prefab名>.2048/texture_00.png。名字最接近 prefab 的那張優先。
        d = os.path.dirname(path)
        stem = os.path.splitext(os.path.basename(path))[0].lower()
        found = []
        for root, _dirs, files in os.walk(d):
            for f in files:
                if f.lower().endswith('.png'):
                    found.append(os.path.join(root, f))
        if not found:
            continue
        found.sort(key=lambda f: (stem not in os.path.basename(os.path.dirname(f)).lower(), len(f)))
        atlas[cid] = os.path.relpath(found[0], os.path.join(ROOT, 'tools')).replace(os.sep, '/')

    # 其他要一起登記的圖庫。key 要跟 尺度標註.json 裡的分組名字一致。
    def gather(dirs):
        out = {}
        for d in dirs:
            full = os.path.join(ROOT, *d.split('/'))
            if not os.path.isdir(full):
                continue
            for f in sorted(os.listdir(full)):
                if not f.lower().endswith(('.png', '.jpg')):
                    continue
                name = os.path.splitext(f)[0]
                out[name] = os.path.relpath(os.path.join(full, f),
                                            os.path.join(ROOT, 'tools')).replace(os.sep, '/')
        return out

    return {'monsters': folder('monsters'), 'battleBacks': folder('background'), 'atlas': atlas,
            'runePortraits': gather(['Assets/Resources/RunePortraits']),
            'runeCards':     gather(['Assets/Sprites/RuneCards']),
            'witches':       gather(['Assets/Resources/Witches', 'Assets/character/witches',
                                     'Assets/Resources/AltSkin_Witch']),
            'combo':         gather(['Assets/Sprites/combo']),
            'tutorial':      gather(['Assets/Sprites/Tutorial',
                                     'Assets/Sprites/Tutorial/rune_tutorial'])}


assets = build_assets()
assets.update(build_extra())

# 尺度標註（人工看過的結果，可以直接編輯 tools/尺度標註.json）
try:
    review = json.load(io.open(os.path.join(ROOT, 'tools', '尺度標註.json'), encoding='utf-8'))
    review.pop('_說明', None)
    assets['review'] = review
    n = sum(len(v) for v in review.values())
    print(f'尺度標註 {n} 筆')
except Exception as e:
    print(f'（沒有尺度標註：{e}）')
    assets['review'] = {}
print(f"背景 {len(assets['backgrounds'])} 張、音樂 {len(assets['audio'])} 首、"
      f"{len(assets['expressions'])} 個角色有表情、怪物 {len(assets['monsters'])} 隻")
print(f"  音樂裡 BGM {sum(1 for v in assets['audioKind'].values() if v == 'bgm')} 首、"
      f"音效 {sum(1 for v in assets['audioKind'].values() if v == 'sfx')} 個")

import time
BUILD = str(int(time.time()))

html = io.open(SRC, encoding='utf-8').read()
html = html.replace("const BUILD = '';", "const BUILD = '" + BUILD + "';", 1)
blob = json.dumps(data, ensure_ascii=False).replace('</', r'<\/')  # 避免提早關掉 <script>
html = html.replace('const EMBEDDED = null;', 'const EMBEDDED = ' + blob + ';', 1)
html = html.replace('const ASSETS = null;',
                    'const ASSETS = ' + json.dumps(assets, ensure_ascii=False) + ';', 1)

io.open(OUT, 'w', encoding='utf-8', newline='').write(html)
print(f'{len(data)} 支腳本 → {OUT}')
print(f'{os.path.getsize(OUT) / 1024 / 1024:.1f} MB')

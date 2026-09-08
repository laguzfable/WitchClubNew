# -*- coding: utf-8 -*-
"""把 Assets/NaniScripts 的內容烤進檢視器，產生「打開就有資料」的版本。

用法（在專案根目錄）：
    python tools/build_viewer.py

改過劇本之後重跑一次就好。原本那支 劇本檢視器.html 不會被動到，
它仍然是「拖資料夾進去」的通用版。
"""
import codecs, glob, hashlib, io, json, os

BACKSLASH = chr(92)
char_prefab = {}
guid_to_file = {}   # guid → 檔案路徑，build_assets 掃出來，build_extra 也要用
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

    guid_to_file.clear()
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

    # guid → 圖檔路徑：怪物編輯器要靠它把 MobData 的 sprite guid 對回圖。
    # 只收兩種：怪物圖庫（換立繪的選單要用），以及 MobData 現在指到的那些
    # （有幾隻的立繪不在 monsters 資料夾，例如教學那隻）。全部圖都收的話檔案會爆掉。
    import re as _re2
    wanted = set()
    for a in glob.glob(os.path.join(ROOT, 'Assets', 'Resources', 'MobData', '*.asset')):
        m = _re2.search(r'sprite: \{fileID: \d+, guid: (\w+)',
                        io.open(a, encoding='utf-8', errors='ignore').read())
        if m:
            wanted.add(m.group(1))

    g2p = {}
    for guid, path in guid_to_file.items():
        if not path.lower().endswith(('.png', '.jpg')):
            continue
        if 'monsters' in path.replace(os.sep, '/') or guid in wanted:
            g2p[guid] = os.path.relpath(path, os.path.join(ROOT, 'tools')).replace(os.sep, '/')

    return {'monsters': folder('monsters'), 'battleBacks': folder('background'), 'atlas': atlas,
            'guidToPath': g2p,
            'runePortraits': gather(['Assets/Resources/RunePortraits']),
            'runeCards':     gather(['Assets/Sprites/RuneCards']),
            'witches':       gather(['Assets/Resources/Witches', 'Assets/character/witches',
                                     'Assets/Resources/AltSkin_Witch']),
            'combo':         gather(['Assets/Sprites/combo']),
            'tutorial':      gather(['Assets/Sprites/Tutorial',
                                     'Assets/Sprites/Tutorial/rune_tutorial'])}


def build_mobs():
    """把 Assets/Resources/MobData 的 .asset 讀成可編輯的資料。

    Unity 的 .asset 是 YAML，但欄位固定、格式規律，所以用 regex 讀就夠了——
    寫回去的時候也只動指定的那幾行，其餘原封不動（見 serve.py 的 /savemob）。
    """
    import re as _re

    def un(x):
        x = x.strip()
        if x.startswith('"') and x.endswith('"'):
            x = x[1:-1]
            if BACKSLASH + 'u' in x:
                try:
                    return codecs.decode(x, 'unicode_escape')
                except Exception:
                    return x
        return x

    mobs = {}
    for path in sorted(glob.glob(os.path.join(ROOT, 'Assets', 'Resources', 'MobData', '*.asset'))):
        name = os.path.splitext(os.path.basename(path))[0]
        text = io.open(path, encoding='utf-8', errors='ignore').read()

        def num(key, default=0):
            m = _re.search(r'^  ' + key + r': (-?[\d.]+)', text, _re.M)
            return float(m.group(1)) if m else default

        def word(key):
            m = _re.search(r'^  ' + key + r': (.*)$', text, _re.M)
            return un(m.group(1)) if m else ''

        # 四個屬性各一組（0 藍 1 紅 2 黃 3 綠）
        elements = []
        for block in _re.finditer(
                r'  - element: (\d+)\s+attribute:\s+ATK: (-?[\d.]+)\s+DEF: (-?[\d.]+)'
                r'\s+HEAL: (-?[\d.]+)\s+EN: (-?[\d.]+)\s+randomWeight: (-?\d+)'
                r'\s+decisionWeight: (-?\d+)', text):
            g = block.groups()
            elements.append({'element': int(g[0]), 'ATK': float(g[1]), 'DEF': float(g[2]),
                             'HEAL': float(g[3]), 'EN': float(g[4]),
                             'randomWeight': int(g[5]), 'decisionWeight': int(g[6])})

        # 台詞（可能整段不存在）
        talk = {}
        tm = _re.search(r'^  talk:' + '\\n' + r'((?:    .*' + '\\n' + r'?)*)', text, _re.M)
        if tm:
            group = None
            for line in tm.group(1).split(chr(10)):
                gm = _re.match(r'    (\w+):\s*$', line)
                if gm:
                    group = gm.group(1)
                    talk[group] = []
                    continue
                im = _re.match(r'    - (.*)$', line)
                if im and group:
                    talk[group].append(un(im.group(1)))

        sprite_guid = ''
        sm = _re.search(r'^  sprite: \{fileID: \d+, guid: (\w+)', text, _re.M)
        if sm:
            sprite_guid = sm.group(1)

        mobs[name] = {
            'displayName': word('displayName'),
            'HP': num('HP'), 'EN': num('EN'),
            'levelUpMaxBonusValue': num('levelUpMaxBonusValue'),
            'maxSelectCardCount': num('maxSelectCardCount'),
            'isBoss': '  isBoss: 1' in text,
            'spriteGuid': sprite_guid,
            'elements': elements,
            'talk': talk,
        }
    return mobs

assets = build_assets()
assets.update(build_extra())

# 尺度標註（人工看過的結果，可以直接編輯 tools/尺度標註.json）
#
# 每一筆會記下當時那張圖的內容雜湊。圖片重畫之後雜湊就對不上，
# 資源頁會把它標成「已更新，待重看」——這樣改完圖不用記得回來改標註，
# 它自己會提醒。重看過覺得沒問題就把那一筆刪掉，還是有問題就更新 note，
# 然後跑 tools/尺度標註.py 重新蓋章。
REVIEW_PATH = os.path.join(ROOT, 'tools', '尺度標註.json')
try:
    review = json.load(io.open(REVIEW_PATH, encoding='utf-8'))
    review.pop('_說明', None)

    stale = 0
    for group, items in review.items():
        paths = assets.get(group, {})
        for name, item in items.items():
            rel = paths.get(name)
            if not rel:
                continue
            full = os.path.normpath(os.path.join(ROOT, 'tools', rel))
            try:
                now = hashlib.sha1(open(full, 'rb').read()).hexdigest()[:12]
            except OSError:
                continue
            item['now'] = now
            if item.get('hash') and item['hash'] != now:
                item['stale'] = True
                stale += 1

    assets['review'] = review
    n = sum(len(v) for v in review.values())
    print(f'尺度標註 {n} 筆' + (f'，其中 {stale} 張圖已經被改過（待重看）' if stale else ''))
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
# 音樂分類（人工聽完標的，可以直接編輯 tools/音樂分類.json）
try:
    tags = json.load(io.open(os.path.join(ROOT, 'tools', '音樂分類.json'), encoding='utf-8'))
    assets['musicTag'] = tags.get('tags', {})
    print(f"音樂分類 {len(assets['musicTag'])} 首")
except Exception as e:
    print(f'（沒有音樂分類：{e}）')
    assets['musicTag'] = {}

mobs = build_mobs()
print(f'怪物資料 {len(mobs)} 隻')
html = html.replace('const MOBS = null;', 'const MOBS = ' + json.dumps(mobs, ensure_ascii=False) + ';', 1)

# 星塵的提示（遊戲跟檢視器讀同一份，不另外抄一次）
try:
    guide = json.load(io.open(os.path.join(ROOT, 'Assets', 'Resources', 'Guide', 'GuideHints.json'),
                              encoding='utf-8-sig'))
    print(f"星塵提示 {len(guide.get('hints', []))} 則")
except Exception as e:
    print(f'（沒有星塵提示：{e}）')
    guide = {'hints': []}
html = html.replace('const GUIDE = null;',
                    'const GUIDE = ' + json.dumps(guide, ensure_ascii=False) + ';', 1)

# 戰鬥對照（人工標的「這一場對手該是誰」，遊戲不讀）
try:
    battles = json.load(io.open(os.path.join(ROOT, 'tools', '戰鬥對照.json'), encoding='utf-8-sig'))
    print(f"戰鬥標註 {len(battles.get('notes', {}))} 筆")
except Exception as e:
    print(f'（沒有戰鬥標註：{e}）')
    battles = {'notes': {}}
html = html.replace('const BATTLENOTES = null;',
                    'const BATTLENOTES = ' + json.dumps(battles, ensure_ascii=False) + ';', 1)

html = html.replace('const ASSETS = null;',
                    'const ASSETS = ' + json.dumps(assets, ensure_ascii=False) + ';', 1)

io.open(OUT, 'w', encoding='utf-8', newline='').write(html)
print(f'{len(data)} 支腳本 → {OUT}')
print(f'{os.path.getsize(OUT) / 1024 / 1024:.1f} MB')

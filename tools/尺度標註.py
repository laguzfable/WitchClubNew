# -*- coding: utf-8 -*-
"""替尺度標註蓋章：把每一筆對應圖片現在的內容雜湊寫進去。

用法（在專案根目錄）：
    python tools/尺度標註.py

什麼時候要跑：
  ‧ 新增了一筆標註（剛寫完 note，還沒有 hash）
  ‧ 重看過一張「已更新，待重看」的圖，確認標註還是對的

改圖之後不用跑這支——雜湊對不上正是我們要的提醒。
確認那張圖沒問題了，就直接把那一筆從 json 裡刪掉。
"""
import hashlib
import io
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REVIEW = os.path.join(ROOT, 'tools', '尺度標註.json')

# 分組名字 → 去哪些資料夾找圖（跟 build_viewer.py 的 gather 一致）
FOLDERS = {
    'backgrounds':   ['Assets/Resources/background'],
    'monsters':      ['Assets/Resources/monsters'],
    'witches':       ['Assets/Resources/Witches', 'Assets/character/witches',
                      'Assets/Resources/AltSkin_Witch'],
    'combo':         ['Assets/Sprites/combo'],
    'tutorial':      ['Assets/Sprites/Tutorial', 'Assets/Sprites/Tutorial/rune_tutorial'],
    'runePortraits': ['Assets/Resources/RunePortraits'],
    'runeCards':     ['Assets/Sprites/RuneCards'],
}


def find(group, name):
    """名字可能帶前綴（背景的 CG/cg19），只取檔名去找。"""
    base = name.split('/')[-1]
    for d in FOLDERS.get(group, []):
        for ext in ('.png', '.jpg', '.jpeg'):
            for sub in ('', 'CG/'):
                p = os.path.join(ROOT, *d.split('/'), *(sub + base + ext).split('/'))
                if os.path.isfile(p):
                    return p
    return None


def main():
    data = json.load(io.open(REVIEW, encoding='utf-8'))
    note = data.pop('_說明', None)

    stamped, missing = 0, []
    for group, items in data.items():
        for name, item in items.items():
            p = find(group, name)
            if not p:
                missing.append(f'{group}/{name}')
                continue
            h = hashlib.sha1(open(p, 'rb').read()).hexdigest()[:12]
            if item.get('hash') != h:
                item['hash'] = h
                stamped += 1
            item.pop('now', None)
            item.pop('stale', None)

    if note:
        data = {'_說明': note, **data}
    io.open(REVIEW, 'w', encoding='utf-8', newline='').write(
        json.dumps(data, ensure_ascii=False, indent=2) + '\n')

    print(f'蓋章 {stamped} 筆')
    if missing:
        print('找不到對應圖片（打錯字或圖已刪除）：' + '、'.join(missing))
    print('接下來跑 python tools/build_viewer.py 就會更新資源頁。')


if __name__ == '__main__':
    main()

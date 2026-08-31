# -*- coding: utf-8 -*-
"""把檢視器跑成本機網頁，這樣它才寫得回 .nani。

用法（在專案根目錄）：
    python tools/serve.py

然後瀏覽器會自己打開。關掉終端機就結束。

★ 為什麼需要這個 ★
用 file:// 打開網頁時，瀏覽器不給它寫硬碟——拖名牌改站位只能停在畫面上，
存不回去。這支只做兩件事：把檔案送出去，以及把改好的 .nani 寫回原位。

★ 安全 ★
只聽 127.0.0.1（別台機器連不到），而且只寫得了 Assets/NaniScripts 底下的 .nani。
"""
import http.server
import json
import os
import posixpath
import re
import threading
import sys
import urllib.parse
import webbrowser

# Windows 終端機預設 cp950，印到中文以外的符號會丟例外，
# 而那個例外會讓 HTTP 回應斷在一半。統一轉成 UTF-8，印不出來的字換成問號。
for stream in (sys.stdout, sys.stderr):
    try:
        stream.reconfigure(encoding='utf-8', errors='replace')
    except Exception:
        pass

BS = chr(92)
NEWLINE = chr(10)
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCRIPTS = os.path.join(ROOT, 'Assets', 'NaniScripts')
PORT = 8777


class _Slice:
    """只讓 copyfile 讀到指定長度的那一段。"""

    def __init__(self, f, length):
        self.f, self.left = f, length

    def read(self, n=-1):
        if self.left <= 0:
            return b''
        data = self.f.read(self.left if n < 0 else min(n, self.left))
        self.left -= len(data)
        return data

    def close(self):
        self.f.close()


class Handler(http.server.SimpleHTTPRequestHandler):
    def translate_path(self, path):
        # 以專案根目錄為基準，這樣網頁才讀得到 ../Assets/... 的背景與音樂
        path = urllib.parse.urlparse(path).path
        path = posixpath.normpath(urllib.parse.unquote(path))
        parts = [p for p in path.split('/') if p and p not in ('.', '..')]
        return os.path.join(ROOT, *parts)

    def send_head(self):
        """媒體檔要支援 Range（分段下載）。

        瀏覽器播音樂時不會整首抓完才播，它會先要前面一段，拖進度條再要中間那段。
        內建的 handler 不理 Range，一律回整包 200——十幾 MB 的 mp3 就會卡住，
        而且每動一次就重抓一次，開著音樂列表沒多久連線就塞滿了。
        """
        rng = self.headers.get('Range')
        if not rng:
            return super().send_head()

        path = self.translate_path(self.path)
        if not os.path.isfile(path):
            return super().send_head()

        m = re.match(r'bytes=(\d*)-(\d*)$', rng.strip())
        if not m:
            return super().send_head()

        size = os.path.getsize(path)
        start, end = m.group(1), m.group(2)
        if start == '':                      # bytes=-500 → 最後 500 個位元組
            start, end = max(0, size - int(end or 0)), size - 1
        else:
            start = int(start)
            end = int(end) if end else size - 1
        end = min(end, size - 1)
        if start > end:
            self.send_response(416)
            self.send_header('Content-Range', f'bytes */{size}')
            self.end_headers()
            return None

        f = open(path, 'rb')
        f.seek(start)
        self.send_response(206)
        self.send_header('Content-Type', self.guess_type(path))
        self.send_header('Accept-Ranges', 'bytes')
        self.send_header('Content-Range', f'bytes {start}-{end}/{size}')
        self.send_header('Content-Length', str(end - start + 1))
        self.end_headers()
        return _Slice(f, end - start + 1)

    def end_headers(self):
        self.send_header('Accept-Ranges', 'bytes')   # 先告訴瀏覽器我們支援分段
        super().end_headers()

    def do_GET(self):
        # /raw?file=chapter4.nani → 直接把硬碟上的內容給網頁，
        # 這樣過期的分頁可以只更新那一支，不用重烤整個檔案再重開。
        # /build → 現在硬碟上那份編輯器的版本。網頁拿它跟自己比，
        # 不一樣就表示「你開著的是舊程式」，會跳出重新整理的提示。
        if self.path == '/build':
            try:
                out = os.path.join(ROOT, 'tools', '劇本檢視器_已載入.html')
                head = open(out, encoding='utf-8', errors='ignore').read()
                m = re.search(r"const BUILD = '(\d*)'", head)
                return self._json({'ok': True, 'build': m.group(1) if m else ''})
            except Exception as e:
                return self._json({'ok': False, 'error': str(e)}, 404)

        if self.path.startswith('/raw?'):
            try:
                query = urllib.parse.parse_qs(urllib.parse.urlparse(self.path).query)
                name = os.path.basename(query.get('file', [''])[0])
                if not name.endswith('.nani'):
                    raise ValueError('只能讀 .nani')
                target = os.path.join(SCRIPTS, name)
                content = open(target, encoding='utf-8-sig', errors='ignore').read()
                return self._json({'ok': True, 'content': content})
            except Exception as e:
                return self._json({'ok': False, 'error': str(e)}, 404)
        return super().do_GET()

    def do_POST(self):
        if self.path == '/savemob':
            return self.save_mob()
        if self.path != '/save':
            return self.send_error(404)

        try:
            size = int(self.headers.get('Content-Length', 0))
            data = json.loads(self.rfile.read(size).decode('utf-8'))
            name = os.path.basename(data['file'])          # 只取檔名，擋掉 ../
            if not name.endswith('.nani'):
                raise ValueError('只能寫 .nani')

            target = os.path.join(SCRIPTS, name)
            if not os.path.abspath(target).startswith(os.path.abspath(SCRIPTS)):
                raise ValueError('超出 NaniScripts 資料夾')

            # 防呆：網頁載入的內容如果跟現在的檔案對不起來，代表這中間有人改過
            # （你在編輯器裡動過，或烤好的資料已經過期）。這時候寫下去會蓋掉別人的修改。
            if 'original' in data and os.path.exists(target):
                now = open(target, encoding='utf-8-sig', errors='ignore').read()
                crlf, lf = chr(13) + chr(10), chr(10)
                if now.replace(crlf, lf) != data['original'].replace(crlf, lf):
                    # 不要只丟一句錯誤就算了——把硬碟上的內容一起送回去，
                    # 網頁就能自己判斷要重新載入還是覆蓋過去。
                    self._json({'ok': False, 'stale': True, 'content': now,
                                'error': '這個檔案在你載入之後被別的地方改過了。'}, 409)
                    print(f'  {name} 有衝突（硬碟上的版本比較新）')
                    return

            with open(target, 'w', encoding='utf-8', newline='') as f:
                f.write(data['content'])

            print(f'  已寫回 {name}（{len(data["content"].splitlines())} 行）')
            self._json({'ok': True})
        except Exception as e:
            print(f'  寫檔失敗：{e}')
            self._json({'ok': False, 'error': str(e)}, 500)

    def save_mob(self):
        """把怪物編輯器改的欄位寫回 .asset。

        只動指定的那幾行，其餘原封不動——Unity 的 .asset 還有一堆
        m_ 開頭的欄位跟 fileID，整份重寫風險太高。
        """
        try:
            size = int(self.headers.get('Content-Length', 0))
            data = json.loads(self.rfile.read(size).decode('utf-8'))
            name = os.path.basename(data['name'])
            target = os.path.join(ROOT, 'Assets', 'Resources', 'MobData', name + '.asset')
            if not os.path.isfile(target):
                raise ValueError('找不到 ' + name + '.asset')

            text = open(target, encoding='utf-8', errors='ignore').read()

            def esc(v):
                """中文要寫成 Unity 慣用的 uXXXX 逃脫碼，不然它自己存檔時會改寫，diff 很難看。"""
                out = ''
                for ch in v:
                    out += (BS + 'u%04X') % ord(ch) if ord(ch) > 126 else ch   # Unity 寫大寫
                return '"' + out + '"'

            def num(v):
                f = float(v)
                return str(int(f)) if f == int(f) else str(f)

            # ── 單值欄位 ──
            for key in ('HP', 'EN', 'levelUpMaxBonusValue', 'maxSelectCardCount'):
                if key in data:
                    text = re.sub(r'(?m)^  ' + key + r': .*$',
                                  '  ' + key + ': ' + num(data[key]), text)
            if 'displayName' in data:
                # 用 lambda 當取代字串：esc() 產生的 uXXXX 逃脫碼裡有反斜線，
                # 直接丟給 re.sub 會被當成正規表示式的跳脫序列而炸掉
                want = '  displayName: ' + esc(data['displayName'])
                text = re.sub(r'(?m)^  displayName: .*$', lambda m: want, text)
            if 'spriteGuid' in data and data['spriteGuid']:
                text = re.sub(r'(?m)^  sprite: \{fileID: \d+, guid: \w+, type: \d+\}$',
                              '  sprite: {fileID: 21300000, guid: ' + data['spriteGuid'] + ', type: 3}',
                              text)
            # isBoss 只在「本來就有這一行」或「要設成 true」時才寫，
            # 不然每隻沒動過的怪都會多一行 isBoss: 0，diff 很吵
            if 'isBoss' in data:
                want = '  isBoss: ' + ('1' if data['isBoss'] else '0')
                if re.search(r'(?m)^  isBoss: ', text):
                    text = re.sub(r'(?m)^  isBoss: .*$', lambda m: want, text)
                elif data['isBoss']:
                    text = re.sub(r'(?m)^  maxSelectCardCount: (.*)$',
                                  lambda m: m.group(0) + NEWLINE + want, text, count=1)

            # ── 四個屬性 ──
            for el in data.get('elements', []):
                pat = (r'(  - element: ' + str(el['element']) + r'\s+attribute:\s+ATK: )(-?[\d.]+)'
                       r'(\s+DEF: )(-?[\d.]+)(\s+HEAL: )(-?[\d.]+)(\s+EN: )(-?[\d.]+)'
                       r'(\s+randomWeight: )(-?\d+)(\s+decisionWeight: )(-?\d+)')

                def sub(m, el=el):
                    return (m.group(1) + num(el['ATK']) + m.group(3) + num(el['DEF']) +
                            m.group(5) + num(el['HEAL']) + m.group(7) + num(el['EN']) +
                            m.group(9) + str(int(el['randomWeight'])) +
                            m.group(11) + str(int(el['decisionWeight'])))
                text = re.sub(pat, sub, text, count=1)

            # ── 台詞：整段重寫（原本沒有的話就補在最後）──
            if 'talk' in data:
                block = ''
                for group in ('battleStart', 'act', 'hurt', 'lowHp', 'defeated'):
                    lines = [x for x in data['talk'].get(group, []) if x.strip()]
                    if not lines:
                        continue
                    block += '    ' + group + ':' + NEWLINE
                    for line in lines:
                        block += '    - ' + esc(line) + NEWLINE
                if block:
                    block = '  talk:' + NEWLINE + block
                text = re.sub(r'(?m)^  talk:' + NEWLINE + r'(?:    .*' + NEWLINE + r'?)*', '', text)
                text = text.rstrip(NEWLINE) + NEWLINE + block

            with open(target, 'w', encoding='utf-8', newline='') as f:
                f.write(text)
            print('  已寫回 ' + name + '.asset')
            self._json({'ok': True})
        except Exception as e:
            print('  怪物存檔失敗：' + str(e))
            self._json({'ok': False, 'error': str(e)}, 500)

    def _json(self, obj, code=200):
        body = json.dumps(obj).encode('utf-8')
        self.send_response(code)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Content-Length', str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, fmt, *args):
        pass  # 太吵了，只留寫檔的紀錄


if __name__ == '__main__':
    os.chdir(ROOT)
    url = f'http://127.0.0.1:{PORT}/tools/劇本檢視器_已載入.html'
    print(f'劇本檢視器：{url}')
    print('可以拖名牌改站位，改完按存檔就會寫回 .nani。關掉這個視窗就結束。')
    threading.Timer(1.0, lambda: webbrowser.open(url)).start()

    # 一定要多執行緒：瀏覽器會同時開好幾條連線抓圖和音訊，
    # 單執行緒的話第一條沒結束就卡住，整個網頁會停在載入中。
    # Windows 的 SO_REUSEADDR 會讓第二台直接搶同一個埠（跟 Linux 不一樣），
    # 兩台同時在聽就會亂。關掉它，這樣重複啟動才會走下面那條友善的分支。
    http.server.ThreadingHTTPServer.allow_reuse_address = False
    try:
        httpd = http.server.ThreadingHTTPServer(('127.0.0.1', PORT), Handler)
    except OSError:
        # 已經有一台在跑了（多半是上一個視窗還開著）。不用再開一台，
        # 直接把網頁叫出來——但那台載的是它啟動當下的劇本。
        print(f'{PORT} 這個埠已經有東西在跑了，應該是另一個視窗還開著。')
        print('直接用那台就好，網頁已經幫你打開。')
        print('（劇本改過而畫面沒更新的話，把舊視窗關掉再跑一次。）')
        webbrowser.open(url)
        raise SystemExit(0)

    with httpd:
        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            print('\n結束。')

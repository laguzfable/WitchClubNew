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
import socketserver
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

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCRIPTS = os.path.join(ROOT, 'Assets', 'NaniScripts')
PORT = 8777


class Handler(http.server.SimpleHTTPRequestHandler):
    def translate_path(self, path):
        # 以專案根目錄為基準，這樣網頁才讀得到 ../Assets/... 的背景與音樂
        path = urllib.parse.urlparse(path).path
        path = posixpath.normpath(urllib.parse.unquote(path))
        parts = [p for p in path.split('/') if p and p not in ('.', '..')]
        return os.path.join(ROOT, *parts)

    def do_POST(self):
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
                    raise ValueError('這個檔案在你載入之後被改過了。'
                                     '請重跑 python tools/build_viewer.py 再改一次，'
                                     '不然會蓋掉編輯器裡的修改。')

            with open(target, 'w', encoding='utf-8', newline='') as f:
                f.write(data['content'])

            print(f'  已寫回 {name}（{len(data["content"].splitlines())} 行）')
            self._json({'ok': True})
        except Exception as e:
            print(f'  寫檔失敗：{e}')
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

    socketserver.TCPServer.allow_reuse_address = True
    with socketserver.TCPServer(('127.0.0.1', PORT), Handler) as httpd:
        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            print('\n結束。')

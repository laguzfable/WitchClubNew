@echo off
rem 雙擊這個檔就會開劇本檢視器。
rem 做兩件事：把最新的劇本烤進網頁，然後跑本機伺服器（這樣才存得回 .nani）。
rem 關掉這個黑色視窗就結束。

chcp 65001 >nul
cd /d "%~dp0"

set PY=python
where python >nul 2>nul || set PY=py

echo [1/2] 讀取最新的劇本...
%PY% tools\build_viewer.py
if errorlevel 1 (
  echo.
  echo 產生失敗。上面那行錯誤訊息是原因。
  pause
  exit /b 1
)

echo [2/2] 啟動...
%PY% tools\serve.py
pause

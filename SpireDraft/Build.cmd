@echo off
setlocal
cd /d "%~dp0"
set "SPIRE_BRANCH=stable"
set /p "SPIRE_BRANCH=Game branch (stable or beta) [stable]: "
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Build.ps1" -Branch "%SPIRE_BRANCH%"
pause

@echo off
setlocal
cd /d "%~dp0"
set "SPIRE_BRANCH=stable"
set "SPIRE_INPUT="
set /p "SPIRE_INPUT=Workspace branch (stable or beta) [stable]: "
if not "%SPIRE_INPUT%"=="" set "SPIRE_BRANCH=%SPIRE_INPUT%"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Upload.ps1" -Branch "%SPIRE_BRANCH%"
pause

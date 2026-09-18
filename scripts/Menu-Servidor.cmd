@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Menu-Servidor.ps1"
exit /b %ERRORLEVEL%

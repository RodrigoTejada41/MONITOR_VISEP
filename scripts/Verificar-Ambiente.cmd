@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Verificar-Ambiente.ps1"
if errorlevel 1 pause

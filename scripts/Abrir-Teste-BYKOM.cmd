@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Abrir-Teste-BYKOM.ps1"
if errorlevel 1 pause

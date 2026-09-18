@echo off
setlocal
if "%~1"=="" (
  echo Uso: Testar-SG3.cmd plain^|b32 [duracaoSegundos]
  exit /b 2
)
set "DURACAO=%~2"
if "%DURACAO%"=="" set "DURACAO=90"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Testar-SG3.ps1" -Framing "%~1" -DurationSeconds %DURACAO%
exit /b %ERRORLEVEL%

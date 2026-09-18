@echo off
setlocal
if "%~1"=="" (
  echo Uso: Analisar-SG3.cmd "C:\ProgramData\Visep\sg3-active-test\data.xml"
  exit /b 2
)
set "receiver=%~dp0..\Visep.Receiver.exe"
if exist "%~dp0..\build\Visep.Receiver.exe" set "receiver=%~dp0..\build\Visep.Receiver.exe"
"%receiver%" --sg3-analyze "%~1"
exit /b %ERRORLEVEL%

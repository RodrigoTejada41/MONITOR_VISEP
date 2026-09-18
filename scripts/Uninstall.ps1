param([string]$InstallDirectory = (Join-Path $env:ProgramFiles 'VISEP'))
$ErrorActionPreference = 'Stop'
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (!$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Execute em PowerShell elevado.' }
$install = [IO.Path]::GetFullPath($InstallDirectory).TrimEnd('\')
$root = [IO.Path]::GetPathRoot($install).TrimEnd('\')
if ($install -eq $root -or $install -eq ([IO.Path]::GetFullPath($env:ProgramFiles).TrimEnd('\')) -or $install -eq ([IO.Path]::GetFullPath($env:WINDIR).TrimEnd('\'))) {
    throw 'Diretorio de instalacao inseguro.'
}
$service = Get-Service -Name VisepReceiver -ErrorAction SilentlyContinue
if ($null -ne $service) {
    if ($service.Status -ne 'Stopped') { Stop-Service -Name VisepReceiver -Force; $service.WaitForStatus('Stopped',[TimeSpan]::FromSeconds(30)) }
    & sc.exe delete VisepReceiver | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao remover servico.' }
}
$shortcut = Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'VISEP.lnk'
if (Test-Path -LiteralPath $shortcut) { Remove-Item -LiteralPath $shortcut -Force }
foreach ($name in @('Visep.Core.dll','Visep.Desktop.exe','Visep.Desktop.exe.config','Visep.Receiver.exe','Visep.Receiver.exe.config','Visep.LegacyInspector.exe','Visep.LegacyInspector.exe.config')) {
    $path = Join-Path $install $name
    if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path -Force }
}
if ((Test-Path -LiteralPath $install -PathType Container) -and @(Get-ChildItem -LiteralPath $install -Force).Count -eq 0) { Remove-Item -LiteralPath $install -Force }
Write-Output 'Servico, atalho e binarios removidos. Dados em C:\ProgramData\Visep foram preservados.'

param(
    [string]$InstallDirectory = (Join-Path $env:ProgramFiles 'VISEP'),
    [string]$DataDirectory = (Join-Path $env:ProgramData 'Visep'),
    [string]$OperatorAccount = ([Security.Principal.WindowsIdentity]::GetCurrent().Name)
)
$ErrorActionPreference = 'Stop'
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (!$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Execute em PowerShell elevado.' }
$root = Split-Path (Split-Path $MyInvocation.MyCommand.Path -Parent) -Parent
$source = Join-Path $root 'build'
if (!(Test-Path (Join-Path $source 'Visep.Desktop.exe'))) { throw 'Execute scripts\Build.ps1 primeiro.' }
if (Get-Service -Name VisepReceiver -ErrorAction SilentlyContinue) { throw 'Servico existente. Pare e siga o procedimento de atualizacao documentado.' }
New-Item -ItemType Directory -Force -Path $InstallDirectory,$DataDirectory | Out-Null
# Validate identity before changing permissions.
$identity = New-Object Security.Principal.NTAccount($OperatorAccount)
$null = $identity.Translate([Security.Principal.SecurityIdentifier])
& icacls.exe $DataDirectory '/inheritance:r' '/grant:r' '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' '*S-1-5-19:(OI)(CI)M' ($OperatorAccount + ':(OI)(CI)M')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao aplicar ACL nos dados.' }
Get-ChildItem $source -File | Where-Object { $_.Name -notmatch 'Tests' } | Copy-Item -Destination $InstallDirectory
& icacls.exe $InstallDirectory '/grant' '*S-1-5-19:(OI)(CI)RX'
if ($LASTEXITCODE -ne 0) { throw 'Falha ao autorizar leitura do servico.' }
$exe = Join-Path $InstallDirectory 'Visep.Receiver.exe'
$data = Join-Path $DataDirectory 'data.xml'
$bin = '"' + $exe + '" "' + $data + '"'
& sc.exe create VisepReceiver binPath= $bin start= auto obj= 'NT AUTHORITY\LocalService' DisplayName= 'VISEP - Receptor SIMULACAO'
if ($LASTEXITCODE -ne 0) { throw 'Falha ao criar servico com identidade LocalService.' }
$shortcut = (New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'VISEP.lnk'))
$shortcut.TargetPath = Join-Path $InstallDirectory 'Visep.Desktop.exe'
$shortcut.Arguments = '"' + $data + '"'
$shortcut.Save()
Write-Output 'Instalado. Abra VISEP e crie o administrador; depois execute Start-Service VisepReceiver. Receptor somente SIMULACAO.'

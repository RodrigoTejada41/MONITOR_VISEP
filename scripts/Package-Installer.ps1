param([string]$Version = '20260918-r9')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'artifacts'
$stage = Join-Path $out ('installer-' + $Version)
$exe = Join-Path $out ('VISEP-Setup-' + $Version + '.exe')
if ((Test-Path $stage) -or (Test-Path $exe)) { throw 'Versao ja empacotada; use outra versao.' }
New-Item -ItemType Directory -Path $stage,(Join-Path $stage 'build'),(Join-Path $stage 'scripts'),(Join-Path $stage 'docs') | Out-Null
foreach ($name in @('Visep.Core.dll','Visep.Desktop.exe','Visep.Desktop.exe.config','Visep.Receiver.exe','Visep.Receiver.exe.config','Visep.LegacyInspector.exe','Visep.LegacyInspector.exe.config')) {
    Copy-Item -LiteralPath (Join-Path $root ('build\' + $name)) -Destination (Join-Path $stage 'build')
}
foreach ($name in @('Install.ps1','Installer.Common.ps1','Menu-Servidor.ps1','Menu-Servidor.cmd','Analisar-SG3.cmd','Verificar-Ambiente.ps1','Verificar-Ambiente.cmd','Testar-SG3.ps1','Uninstall.ps1','Preparar-Importacao-BYKOM.ps1')) {
    Copy-Item -LiteralPath (Join-Path $root ('scripts\' + $name)) -Destination (Join-Path $stage 'scripts')
}
foreach ($name in @('INSTALADOR.md','IMPORTACAO_BYKOM.md')) { Copy-Item -LiteralPath (Join-Path $root ('docs\' + $name)) -Destination (Join-Path $stage 'docs') }
$migration = Join-Path $stage 'migration'
New-Item -ItemType Directory -Path $migration | Out-Null
foreach ($name in @('import_legacy.py','sql_dump.py')) { Copy-Item -LiteralPath (Join-Path $root ('src\Migration\' + $name)) -Destination $migration }
$runtime = Join-Path $stage 'runtime\python'
New-Item -ItemType Directory -Force -Path $runtime | Out-Null
Expand-Archive -LiteralPath (Join-Path $root 'tools\vendor\python-3.8.10-embed-amd64.zip') -DestinationPath $runtime
Get-ChildItem $stage -Recurse -File | Sort-Object FullName | ForEach-Object {
    '{0} *{1}' -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash,$_.FullName.Substring($stage.Length + 1)
} | Set-Content (Join-Path $stage 'SHA256SUMS.txt') -Encoding ASCII
$payload = Join-Path $out ('payload-' + $Version + '.zip')
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $payload
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /target:exe /platform:anycpu /langversion:5 /warnaserror+ ('/out:' + $exe) ('/win32manifest:' + (Join-Path $root 'src\Installer\app.manifest')) ('/resource:' + $payload + ',Visep.Payload.zip') /reference:System.IO.Compression.dll (Join-Path $root 'src\Installer\InstallerBootstrap.cs')
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar instalador.' }
(Get-FileHash $exe -Algorithm SHA256).Hash | Set-Content ($exe + '.sha256') -Encoding ASCII
Write-Output $exe

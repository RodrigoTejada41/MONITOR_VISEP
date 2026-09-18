param(
    [string]$Version = (Get-Date -Format 'yyyyMMdd-HHmm'),
    [string]$OutputDirectory = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts')
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$name = 'VISEP_Server2008R2_Teste_' + $Version
$stage = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) $name
$zip = $stage + '.zip'
if (Test-Path -LiteralPath $stage) { throw 'Diretorio do pacote ja existe.' }
if (Test-Path -LiteralPath $zip) { throw 'ZIP do pacote ja existe.' }
New-Item -ItemType Directory -Path (Join-Path $stage 'build'),(Join-Path $stage 'scripts'),(Join-Path $stage 'docs') | Out-Null
foreach ($file in @('Visep.Core.dll','Visep.Desktop.exe','Visep.Desktop.exe.config','Visep.Receiver.exe','Visep.Receiver.exe.config','Visep.LegacyInspector.exe','Visep.LegacyInspector.exe.config')) {
    Copy-Item -LiteralPath (Join-Path $root ('build\' + $file)) -Destination (Join-Path $stage 'build')
}
foreach ($file in @('Menu-Servidor.cmd','Menu-Servidor.ps1','Analisar-SG3.cmd','Verificar-Ambiente.cmd','Verificar-Ambiente.ps1','Abrir-Teste-Servidor.cmd','Abrir-Teste-Servidor.ps1','Testar-SG3.cmd','Testar-SG3.ps1','Install.ps1','Uninstall.ps1','Backup.ps1','Restore.ps1')) {
    Copy-Item -LiteralPath (Join-Path $root ('scripts\' + $file)) -Destination (Join-Path $stage 'scripts')
}
foreach ($file in @('CLASSIFICACAO_SG3.md','PACOTE_TESTE_SERVIDOR.md','TESTE_REAL_SG3.md','OPERACAO.md','COMPATIBILIDADE_WINDOWS.md','JOURNAL_REPLAY.md','BACKUP_RESTAURACAO.md')) {
    Copy-Item -LiteralPath (Join-Path $root ('docs\' + $file)) -Destination (Join-Path $stage 'docs')
}
Copy-Item -LiteralPath (Join-Path $root 'docs\PACOTE_TESTE_SERVIDOR.md') -Destination (Join-Path $stage 'LEIA-ME.md')
$hashes = Get-ChildItem -LiteralPath $stage -Recurse -File | Sort-Object FullName | ForEach-Object {
    $relative = $_.FullName.Substring($stage.Length + 1)
    '{0} *{1}' -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash,$relative
}
$hashes | Set-Content -LiteralPath (Join-Path $stage 'SHA256SUMS.txt') -Encoding ASCII
Compress-Archive -LiteralPath $stage -DestinationPath $zip -CompressionLevel Optimal
$zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
($zipHash + ' *' + (Split-Path $zip -Leaf)) | Set-Content -LiteralPath ($zip + '.sha256') -Encoding ASCII
Write-Output $zip

param(
    [Parameter(Mandatory=$true)][string]$DataDirectory,
    [Parameter(Mandatory=$true)][string]$Destination
)
$ErrorActionPreference = 'Stop'
if (Get-Process -Name 'Visep.Receiver','Visep.Desktop' -ErrorAction SilentlyContinue) {
    throw 'Feche o desktop e pare o receptor antes do backup consistente.'
}
$source = [IO.Path]::GetFullPath($DataDirectory)
$target = [IO.Path]::GetFullPath($Destination)
if ($target.StartsWith($source.TrimEnd('\') + '\',[StringComparison]::OrdinalIgnoreCase) -or $target -eq $source) {
    throw 'Destino deve ficar fora do diretorio de dados.'
}
if (Test-Path $target) { throw 'Use um destino novo para preservar backups anteriores.' }
if (!(Test-Path (Join-Path $source 'data.xml'))) { throw 'data.xml nao encontrado.' }
$gate = [IO.File]::Open((Join-Path $source 'data.xml.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try {
    New-Item -ItemType Directory -Path $target | Out-Null
    $account = [Security.Principal.WindowsIdentity]::GetCurrent().Name
    & icacls.exe $target '/inheritance:r' '/grant:r' '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' ($account + ':(OI)(CI)F') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Falha ACL backup.' }
    Copy-Item -LiteralPath (Join-Path $source 'data.xml') -Destination $target
    $inbox = Join-Path $source 'inbox'
    if (Test-Path $inbox) { Copy-Item -LiteralPath $inbox -Destination $target -Recurse }
    $files = @(Get-ChildItem $target -Recurse -File | ForEach-Object {
        [pscustomobject]@{ Path=$_.FullName.Substring($target.Length + 1); Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
    })
    $files | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $target 'manifest.json') -Encoding UTF8
    Write-Output ('Backup concluido: ' + $target)
} finally { $gate.Dispose() }

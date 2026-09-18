param(
    [Parameter(Mandatory=$true)][string]$BackupDirectory,
    [Parameter(Mandatory=$true)][string]$Destination
)
$ErrorActionPreference = 'Stop'
if (Get-Process -Name 'Visep.Receiver','Visep.Desktop' -ErrorAction SilentlyContinue) {
    throw 'Feche o desktop e pare o receptor antes da restauracao.'
}
$source = [IO.Path]::GetFullPath($BackupDirectory).TrimEnd('\')
$target = [IO.Path]::GetFullPath($Destination)
if (Test-Path $target) { throw 'Restaure em um diretorio novo; os dados existentes serao preservados.' }
$manifest = @(Get-Content -LiteralPath (Join-Path $source 'manifest.json') -Raw | ConvertFrom-Json | ForEach-Object { $_ })
if (!$manifest.Count -or !($manifest | Where-Object { $_.Path -eq 'data.xml' })) { throw 'Manifesto incompleto.' }
foreach ($file in $manifest) {
    $path = [IO.Path]::GetFullPath((Join-Path $source $file.Path))
    if (!$path.StartsWith($source + '\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Caminho invalido no manifesto.' }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.Sha256) { throw 'Backup corrompido: hash divergente.' }
}
$settings = New-Object System.Xml.XmlReaderSettings
$settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
$settings.XmlResolver = $null
$settings.MaxCharactersInDocument = 64 * 1024 * 1024
$reader = [System.Xml.XmlReader]::Create((Join-Path $source 'data.xml'),$settings)
try {
    $document = New-Object System.Xml.XmlDocument
    $document.XmlResolver = $null
    $document.Load($reader)
} finally { $reader.Dispose() }
$state = $document.DocumentElement
if ($state.Name -ne 'Visep' -or $state.NamespaceURI -ne '' -or $state.GetAttribute('SchemaVersion') -ne '1') {
    throw 'Versao ou raiz do estado invalida.'
}
foreach ($section in @('Users','Clients','Incidents','Audit')) {
    if ($state.SelectNodes($section).Count -ne 1) { throw ('Secao ausente ou duplicada: ' + $section) }
}
New-Item -ItemType Directory -Path $target | Out-Null
$account = [Security.Principal.WindowsIdentity]::GetCurrent().Name
& icacls.exe $target '/inheritance:r' '/grant:r' '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' ($account + ':(OI)(CI)F') | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Falha ACL restauracao.' }
foreach ($file in $manifest) {
    $destinationFile = Join-Path $target $file.Path
    New-Item -ItemType Directory -Force -Path (Split-Path $destinationFile -Parent) | Out-Null
    Copy-Item -LiteralPath (Join-Path $source $file.Path) -Destination $destinationFile
}
Write-Output ('Restaurado: ' + $target + '. Reaplique ACL do servico e valide o login antes de operar.')

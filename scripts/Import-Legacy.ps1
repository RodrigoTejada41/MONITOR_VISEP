param(
    [string]$SqlFile = (Join-Path $PSScriptRoot '..\Inventario\bykom.sql'),
    [string]$Destination = (Join-Path $PSScriptRoot '..\data\bykom-teste'),
    [ValidateSet('latin-1','utf-8-sig','cp1252')][string]$Encoding = 'latin-1',
    [ValidateRange(0,1000)][int]$HistoryLimit = 1000,
    [switch]$AllowControlSeparator
)
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $Destination) { throw 'Destino existente. Use outro diretorio para preservar o teste anterior.' }
$extra = @()
if ($AllowControlSeparator) { $extra += '--allow-control-separator' }
& python (Join-Path $PSScriptRoot '..\src\Migration\import_legacy.py') --source $SqlFile --destination $Destination --encoding $Encoding --history-limit $HistoryLimit @extra
if ($LASTEXITCODE -ne 0) { throw 'Importacao nao concluida. Nenhum SQL foi executado.' }

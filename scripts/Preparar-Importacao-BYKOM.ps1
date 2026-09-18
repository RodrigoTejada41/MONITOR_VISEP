param(
    [Parameter(Mandatory=$true)][string]$SqlFile,
    [string]$DestinationRoot = (Join-Path $env:ProgramData 'Visep-Imports'),
    [ValidateSet('latin-1','utf-8-sig','cp1252')][string]$Encoding = 'latin-1',
    [ValidateRange(0,10000)][int]$HistoryLimit = 1000,
    [switch]$AllowControlSeparator
)
$ErrorActionPreference = 'Stop'
trap { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }
$scriptDirectory = Split-Path $MyInvocation.MyCommand.Path -Parent
$pythonScript = Join-Path (Split-Path $scriptDirectory -Parent) 'migration\import_legacy.py'
if (!(Test-Path -LiteralPath $SqlFile -PathType Leaf)) { throw 'Backup SQL nao encontrado.' }
if (!(Test-Path -LiteralPath $pythonScript -PathType Leaf)) { throw 'Importador BYKOM ausente no pacote.' }
$python = Get-Command python.exe -ErrorAction SilentlyContinue
if ($null -eq $python) { throw 'Python 3 nao encontrado. Instale Python 3 no servidor antes da previa; o SQL nao sera executado.' }
$destination = Join-Path $DestinationRoot ('bykom-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force -Path $DestinationRoot | Out-Null
$arguments = @($pythonScript,'--source',$SqlFile,'--destination',$destination,'--encoding',$Encoding,'--history-limit',$HistoryLimit)
if ($AllowControlSeparator) { $arguments += '--allow-control-separator' }
& $python.Source @arguments
if ($LASTEXITCODE -ne 0) { throw 'Previa BYKOM falhou. Base ativa VISEP nao foi alterada.' }
Write-Output ('Previa concluida: ' + $destination)
Write-Output ('Relatorio: ' + (Join-Path $destination 'import-report.json'))
Write-Output 'Esta previa nao altera C:\ProgramData\Visep\data.xml, usuarios, ocorrencias ou journal.'

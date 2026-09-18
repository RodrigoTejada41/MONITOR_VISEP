param(
    [Parameter(Mandatory=$true)][string]$SqlFile,
    [string]$DestinationRoot = (Join-Path $env:ProgramData 'Visep-Imports'),
    [ValidateSet('latin-1','utf-8-sig','cp1252')][string]$Encoding = 'latin-1',
    [switch]$AllowControlSeparator
)
$ErrorActionPreference = 'Stop'
trap { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }
$scriptDirectory = Split-Path $MyInvocation.MyCommand.Path -Parent
$pythonScript = Join-Path (Split-Path $scriptDirectory -Parent) 'migration\import_legacy.py'
if (!(Test-Path -LiteralPath $SqlFile -PathType Leaf)) { throw 'Backup SQL nao encontrado.' }
if (!(Test-Path -LiteralPath $pythonScript -PathType Leaf)) { throw 'Importador BYKOM ausente no pacote.' }
$runtimePython = Join-Path (Split-Path $scriptDirectory -Parent) 'runtime\python\python.exe'
$python = if (Test-Path -LiteralPath $runtimePython -PathType Leaf) { $runtimePython } else { $null }
if ($null -eq $python) {
    $command = Get-Command python.exe -ErrorAction SilentlyContinue
    if ($null -ne $command) { $python = $command.Source }
}
if ($null -eq $python) { throw 'Runtime Python ausente no VISEP. Execute o instalador r15 ou posterior; o SQL nao sera executado.' }
$destination = Join-Path $DestinationRoot ('bykom-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force -Path $DestinationRoot | Out-Null
$arguments = @($pythonScript,'--source',$SqlFile,'--destination',$destination,'--encoding',$Encoding,'--history-limit','0','--clients-only')
if ($AllowControlSeparator) { $arguments += '--allow-control-separator' }
& $python @arguments
if ($LASTEXITCODE -ne 0) { throw 'Previa BYKOM falhou. Base ativa VISEP nao foi alterada.' }
Write-Output ('Previa concluida: ' + $destination)
Write-Output ('Relatorio: ' + (Join-Path $destination 'import-report.json'))
Write-Output 'Esta previa nao altera C:\ProgramData\Visep\data.xml, usuarios, ocorrencias ou journal.'

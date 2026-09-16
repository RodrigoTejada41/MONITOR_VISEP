param([Parameter(Mandatory=$true)][string]$SqlFile)
$ErrorActionPreference = 'Stop'
$inspector = Join-Path $PSScriptRoot '..\build\Visep.LegacyInspector.exe'
if (-not (Test-Path -LiteralPath $inspector -PathType Leaf)) { throw 'Compile o projeto antes de executar a inspecao.' }
if (-not (Test-Path -LiteralPath $SqlFile -PathType Leaf)) { throw 'Arquivo SQL nao encontrado.' }
& $inspector (Resolve-Path -LiteralPath $SqlFile).Path
if ($LASTEXITCODE -ne 0) { throw 'Inspecao falhou.' }

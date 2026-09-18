param(
    [Parameter(Mandatory=$true)][ValidateSet('plain','b32')][string]$Framing,
    [ValidateRange(10,600)][int]$DurationSeconds = 90,
    [string]$Address = '192.168.1.249',
    [ValidateRange(1,65535)][int]$Port = 1025,
    [string]$DataFile
)
$ErrorActionPreference = 'Stop'
$scriptDirectory = Split-Path $MyInvocation.MyCommand.Path -Parent
$root = Split-Path $scriptDirectory -Parent
if ([String]::IsNullOrEmpty($DataFile)) { $DataFile = Join-Path $root 'data\sg3-capture\data.xml' }
$receiver = Join-Path $root 'build\Visep.Receiver.exe'
if (!(Test-Path -LiteralPath $receiver)) { $receiver = Join-Path $root 'Visep.Receiver.exe' }
if (!(Test-Path -LiteralPath $receiver -PathType Leaf)) { throw 'Visep.Receiver.exe nao encontrado no pacote.' }

$service = Get-Service -Name 'VisepReceiver' -ErrorAction SilentlyContinue
if ($null -ne $service -and $service.Status -ne 'Stopped') { throw 'Pare o servico VisepReceiver antes da captura SG3.' }

$escapedEndpoint = [Regex]::Escape($Address + ':' + $Port)
$existing = @(& netstat.exe -ano -p tcp | Where-Object { $_ -match ('^\s*TCP\s+\S+\s+' + $escapedEndpoint + '\s+ESTABLISHED\s+\d+\s*$') })
if ($existing.Count -gt 0) {
    Write-Host 'Conexao ativa encontrada; captura recusada para nao criar segundo consumidor:' -ForegroundColor Red
    $existing | ForEach-Object { Write-Host $_ }
    exit 3
}

$data = [IO.Path]::GetFullPath($DataFile)
$dataDirectory = Split-Path $data -Parent
$resultDirectory = Join-Path $root 'resultados-sg3'
New-Item -ItemType Directory -Force -Path $dataDirectory,$resultDirectory | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$resultFile = Join-Path $resultDirectory ('sg3-' + $stamp + '.txt')

@(
    'utc=' + [DateTime]::UtcNow.ToString('o')
    'endpoint=' + $Address + ':' + $Port
    'framing=' + $Framing
    'durationSeconds=' + $DurationSeconds
    'computer=' + $env:COMPUTERNAME
) | Set-Content -LiteralPath $resultFile -Encoding ASCII

Write-Host ('Conectando em ' + $Address + ':' + $Port + ' por ate ' + $DurationSeconds + ' segundos; framing=' + $Framing + '.')
& $receiver --sg3-capture $data $Address $Port $Framing $DurationSeconds --confirm-live --send-ack 2>&1 | ForEach-Object {
    Write-Host ([string]$_)
    Add-Content -LiteralPath $resultFile -Value ([string]$_) -Encoding ASCII
}
$exitCode = $LASTEXITCODE
('exitCode=' + $exitCode) | Add-Content -LiteralPath $resultFile -Encoding ASCII
Write-Host ('Resultado: ' + $resultFile)
exit $exitCode

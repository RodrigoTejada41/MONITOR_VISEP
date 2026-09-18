param([string]$DataFile, [switch]$Once)
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $MyInvocation.MyCommand.Path -Parent) -Parent
$desktopData = Join-Path $env:ProgramData 'Visep\data.xml'
$mode = 'simulation'
$address = '192.168.1.249'
$port = 1025
$configPath = Join-Path $root 'receiver-config.xml'
if (Test-Path -LiteralPath $configPath) {
    $config = New-Object System.Xml.XmlDocument
    $config.XmlResolver = $null
    $config.Load($configPath)
    $desktopData = [string]$config.DocumentElement.GetAttribute('DataFile')
    $mode = [string]$config.DocumentElement.GetAttribute('Mode')
    $address = [string]$config.DocumentElement.GetAttribute('Address')
    $port = [int]$config.DocumentElement.GetAttribute('Port')
    if ([String]::IsNullOrEmpty($desktopData)) { throw 'Configuracao instalada sem caminho de dados.' }
}
if ([String]::IsNullOrEmpty($DataFile)) { $DataFile = Join-Path $env:ProgramData 'Visep\sg3-active-test\data.xml' }
if ($mode -eq 'sg3' -and !$PSBoundParameters.ContainsKey('DataFile')) { $DataFile = $desktopData }
$receiver = Join-Path $root 'build\Visep.Receiver.exe'
if (!(Test-Path -LiteralPath $receiver)) { $receiver = Join-Path $root 'Visep.Receiver.exe' }
$desktopProcess = $null
function Show-Status([string]$path) {
    $inbox = Join-Path (Split-Path $path -Parent) 'inbox'
    $captures = @()
    $captureDir = Join-Path $inbox 'captures'
    if (Test-Path -LiteralPath $captureDir) {
        $captures = @(Get-ChildItem -LiteralPath $captureDir -Filter '*.xml' | Where-Object { !$_.PSIsContainer })
    }
    $endpoint = [Regex]::Escape($address + ':' + $port)
    $connections = @(& netstat.exe -ano -p tcp | Where-Object { $_ -match ('^\s*TCP\s+\S+\s+' + $endpoint + '\s+ESTABLISHED\s+\d+\s*$') })
    Write-Host '===== VISEP - PAINEL SG3 =====' -ForegroundColor Cyan
    Write-Host ('Dados: ' + $path)
    Write-Host ('TCP ' + $address + ':' + $port + ': ' + $(if ($connections.Count) { 'CONECTADO (consumidor pode ser BYKOM ou VISEP)' } else { 'SEM CONEXAO ESTABELECIDA' }))
    foreach ($connection in $connections) { Write-Host $connection.Trim() }
    $service = Get-Service VisepReceiver -ErrorAction SilentlyContinue
    Write-Host ('Servico VISEP (' + $mode + '): ' + $(if ($null -eq $service) { 'NAO INSTALADO' } else { [string]$service.Status }))
    Write-Host ('Base da interface: ' + $desktopData)
    Write-Host ('Capturas gravadas: ' + $captures.Count)
    if ($captures.Count) {
        $last = $captures | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
        Write-Host ('Ultima gravacao local UTC: ' + $last.LastWriteTimeUtc.ToString('u'))
    }
    Write-Host 'Conexao TCP nao comprova recebimento. Aumento das capturas indica gravacao.'
    Write-Host 'Validacao dos arquivos: use ANALISAR. Contagem inclui repeticoes.'
}
function Run-Analysis([string]$path) {
    & $receiver --sg3-analyze $path
    $code = $LASTEXITCODE
    Write-Host ('Codigo de saida: ' + $code + ' (0=estrutura valida; 1=falha). Unknown exige revisao.')
}
if ($Once) { Show-Status $DataFile; exit 0 }
while ($true) {
    try {
        Clear-Host
        Show-Status $DataFile
        Write-Host "`n1 - Atualizar painel`n2 - Analisar captura offline`n3 - Escolher data.xml`n4 - Captura REAL acompanhada (90 segundos)`n5 - Verificar ambiente`n6 - Reiniciar servico VISEP (administrador)`n7 - Abrir interface VISEP`n8 - Reiniciar interface aberta por este menu`n0 - Sair"
        $choice = Read-Host 'Opcao'
        switch ($choice) {
            '0' { exit 0 }
            '1' { continue }
            '2' { Run-Analysis $DataFile }
            '3' {
                $newPath = Read-Host 'Caminho completo do data.xml'
                if (![String]::IsNullOrEmpty($newPath)) { $DataFile = [IO.Path]::GetFullPath($newPath.Trim('"')) }
            }
            '4' {
                if ($mode -eq 'sg3') { throw 'Captura continua configurada. Use opcao 1 para acompanhar e 2 para analisar; nao abra segunda captura.' }
                Write-Host 'Exige janela operacional, consumidor anterior e VisepReceiver parados.' -ForegroundColor Yellow
                Write-Host 'Envia ACK ao SG3. Nao cria ocorrencias. Nao altera servicos automaticamente.'
                $framing = Read-Host 'B32 Headers OFF = plain; ON = b32'
                if ($framing -ne 'plain' -and $framing -ne 'b32') { throw 'Framing invalido.' }
                if ((Read-Host 'Digite CAPTURAR para iniciar') -ne 'CAPTURAR') { continue }
                $sessionDir = Join-Path $root ('data\sg3-menu-' + [Guid]::NewGuid().ToString('N'))
                New-Item -ItemType Directory -Path $sessionDir | Out-Null
                $DataFile = Join-Path $sessionDir 'data.xml'
                $script = Join-Path $root 'scripts\Testar-SG3.ps1'
                $stdout = Join-Path $sessionDir 'console.txt'
                $stderr = Join-Path $sessionDir 'erro.txt'
                $arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + $script + '" -Framing ' + $framing + ' -DurationSeconds 90 -DataFile "' + $DataFile + '"'
                $process = Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
                try {
                    while (!$process.HasExited) {
                        Clear-Host
                        Show-Status $DataFile
                        Write-Host 'CAPTURA EM EXECUCAO - atualizacao a cada 2 segundos; aguarde encerramento.'
                        Start-Sleep -Seconds 2
                        $process.Refresh()
                    }
                    $process.WaitForExit()
                    Write-Host 'Captura encerrada. Confira resultado abaixo:'
                    Get-Content -LiteralPath $stdout
                    Get-Content -LiteralPath $stderr
                    Run-Analysis $DataFile
                } finally {
                    if (!$process.HasExited) {
                        Write-Host 'Aguardando captura terminar antes de liberar o menu...'
                        $process.WaitForExit()
                    }
                    $process.Dispose()
                }
            }
            '5' { & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'scripts\Verificar-Ambiente.ps1') }
            '6' {
                $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
                if (!$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Abra Menu-Servidor.cmd como administrador.' }
                $svc = Get-Service VisepReceiver -ErrorAction Stop
                Write-Host ('Reinicia somente VisepReceiver, modo ' + $mode + '. BYKOM e Printer nao sao alterados.')
                if ((Read-Host 'Digite REINICIAR para confirmar a interrupcao') -ne 'REINICIAR') { continue }
                if ($svc.Status -ne 'Stopped') {
                    Stop-Service -Name VisepReceiver -ErrorAction Stop
                    $svc.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
                }
                Start-Service -Name VisepReceiver -ErrorAction Stop
                $svc.WaitForStatus('Running', [TimeSpan]::FromSeconds(30))
                Write-Host 'VisepReceiver: RUNNING. Isso nao comprova conexao SG3.' -ForegroundColor Green
            }
            { $_ -eq '7' -or $_ -eq '8' } {
                if ($choice -eq '8') {
                    if ($null -eq $desktopProcess -or $desktopProcess.HasExited) { throw 'Nenhuma interface aberta por este menu. Use opcao 7.' }
                    if ((Read-Host 'Salve o trabalho. Digite REINICIAR para fechar e reabrir a interface') -ne 'REINICIAR') { continue }
                    $null = $desktopProcess.CloseMainWindow()
                    if (!$desktopProcess.WaitForExit(10000)) { throw 'Interface ainda aberta. Feche os dialogos; reinicio cancelado sem encerramento forcado.' }
                    $desktopProcess.Dispose()
                    $desktopProcess = $null
                }
                if (@(Get-Process -Name 'Visep.Desktop' -ErrorAction SilentlyContinue).Count -gt 0) { throw 'Interface VISEP ja aberta. Use a janela existente ou feche-a antes.' }
                $desktop = Join-Path $root 'build\Visep.Desktop.exe'
                if (!(Test-Path -LiteralPath $desktop)) { $desktop = Join-Path $root 'Visep.Desktop.exe' }
                Write-Host ('Interface: ' + $desktopData)
                $desktopProcess = Start-Process -FilePath $desktop -ArgumentList ('"' + $desktopData + '"') -WindowStyle Normal -PassThru
            }

            default { Write-Host 'Opcao invalida.' }
        }
    } catch { Write-Host ('Falha: ' + $_.Exception.Message) -ForegroundColor Red }
    $null = Read-Host 'Enter para voltar'
}

param(
    [string]$InstallDirectory = (Join-Path $env:ProgramFiles 'VISEP'),
    [string]$DataDirectory = (Join-Path $env:ProgramData 'Visep'),
    [string]$OperatorAccount = ([Security.Principal.WindowsIdentity]::GetCurrent().Name),
    [string]$SourceDataFile,
    [ValidateSet('simulation','sg3')][string]$ReceiverMode,
    [string]$Sg3Address = '192.168.1.249',
    [ValidateRange(1,65535)][int]$Sg3Port = 1025,
    [ValidateSet('plain','b32')][string]$Sg3Framing = 'plain',
    [switch]$ConfirmLive,
    [switch]$Interactive
)
$ErrorActionPreference = 'Stop'
trap { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }
$root = Split-Path (Split-Path $MyInvocation.MyCommand.Path -Parent) -Parent
. (Join-Path $root 'scripts\Installer.Common.ps1')
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (!$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Execute em PowerShell elevado.' }
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'scripts\Verificar-Ambiente.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Ambiente incompativel.' }
$source = Join-Path $root 'build'
foreach ($name in @('Visep.Desktop.exe','Visep.Receiver.exe')) { if (!(Test-Path (Join-Path $source $name))) { throw ('Pacote incompleto: ' + $name) } }
$service = Get-WmiObject Win32_Service -Filter "Name='VisepReceiver'"
$previousCommand = $null
$wasRunning = $false
$data = Join-Path $DataDirectory 'data.xml'
if ($service) {
    $previousCommand = $service.PathName
    $previous = Get-VisepServicePaths $previousCommand
    $data = $previous.DataFile
    $DataDirectory = Split-Path $data -Parent
    $InstallDirectory = Split-Path $previous.Executable -Parent
    $wasRunning = $service.State -eq 'Running'
}
$InstallDirectory = [IO.Path]::GetFullPath($InstallDirectory).TrimEnd('\')
$DataDirectory = [IO.Path]::GetFullPath($DataDirectory).TrimEnd('\')
foreach ($target in @($InstallDirectory,$DataDirectory)) {
    if ($target -eq [IO.Path]::GetPathRoot($target).TrimEnd('\') -or $target -eq $env:ProgramFiles -or $target -eq $env:ProgramData -or $target -eq $env:windir) { throw 'Diretorio amplo demais; instalacao cancelada.' }
    if ((Test-Path -LiteralPath $target) -and ((Get-Item -LiteralPath $target).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Diretorio de instalacao/dados nao pode ser link.' }
}
if ($InstallDirectory -eq $DataDirectory -or $InstallDirectory.StartsWith($DataDirectory + '\',[StringComparison]::OrdinalIgnoreCase) -or $DataDirectory.StartsWith($InstallDirectory + '\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Pastas de binarios e dados devem ser separadas.' }
foreach ($process in @(Get-WmiObject Win32_Process -Filter "Name='Visep.Desktop.exe' OR Name='Visep.Receiver.exe'")) {
    if (!$service -or $process.ProcessId -ne $service.ProcessId) { throw ('Feche a interface/receptor console VISEP (PID ' + $process.ProcessId + ') e execute novamente.') }
}
$identity = New-Object Security.Principal.NTAccount($OperatorAccount)
$null = $identity.Translate([Security.Principal.SecurityIdentifier])
$candidates = @(Find-VisepBases @('C:\CVISEP-Teste','C:\CVISEP-r8') $data)
for ($i = 0; $i -lt $candidates.Count; $i++) { $c = $candidates[$i]; Write-Host ('[{0}] {1} | Usuarios={2}; Clientes={3}; Ocorrencias={4}' -f ($i+1),$c.Path,$c.Users,$c.Clients,$c.Incidents) }
if (!$SourceDataFile) {
    $meaningful = @($candidates | Where-Object { $_.Clients -gt 0 -or $_.Incidents -gt 0 })
    $external = @($meaningful | Where-Object { $_.Path -ne $data })
    if ($external.Count -gt 0) {
        $answer = Read-Host 'Selecione explicitamente o numero da base para usar (0 = manter base atual). Nao ha mesclagem'
        $choice = 0
        if (![int]::TryParse($answer, [ref]$choice) -or $choice -lt 0 -or $choice -gt $candidates.Count) { throw 'Selecao invalida.' }
        if ($choice -gt 0) { $SourceDataFile = $candidates[$choice-1].Path }
    }
}
if ($SourceDataFile) { $null = Get-VisepDataSummary $SourceDataFile; $SourceDataFile = [IO.Path]::GetFullPath($SourceDataFile) }
if (!$ReceiverMode) {
    $mode = Read-Host 'Modo: 1 = simulacao; 2 = captura continua SG3 real'
    if ($mode -eq '1') { $ReceiverMode = 'simulation' } elseif ($mode -eq '2') { $ReceiverMode = 'sg3' } else { throw 'Modo invalido.' }
}
if ($ReceiverMode -eq 'sg3') {
    if ($Interactive) {
        $answer = Read-Host ('IP SG3 [' + $Sg3Address + ']')
        if (![String]::IsNullOrEmpty($answer)) { $Sg3Address = $answer.Trim() }
        $answer = Read-Host ('Porta SG3 [' + $Sg3Port + ']')
        if (![String]::IsNullOrEmpty($answer)) { $Sg3Port = [int]$answer }
        $answer = Read-Host ('Framing SG3 plain|b32 [' + $Sg3Framing + ']')
        if (![String]::IsNullOrEmpty($answer)) { $Sg3Framing = $answer.Trim().ToLowerInvariant() }
        if ($Sg3Framing -ne 'plain' -and $Sg3Framing -ne 'b32') { throw 'Framing SG3 invalido.' }
    }
    $parsedAddress = $null
    if (![Net.IPAddress]::TryParse($Sg3Address, [ref]$parsedAddress)) { throw 'Endereco IP SG3 invalido.' }
    foreach ($connection in @(& netstat.exe -ano -p tcp)) {
        $parts = @($connection.Trim() -split '\s+')
        if ($parts.Count -ge 5 -and $parts[0] -eq 'TCP' -and $parts[2] -eq ($Sg3Address + ':' + $Sg3Port) -and (!$service -or $parts[4] -ne [string]$service.ProcessId)) { throw ('Conexao SG3 ja existe no PID ' + $parts[4] + '. Encerre o consumidor anterior antes de instalar.') }
    }
    if (!$ConfirmLive) {
        Write-Host ('SG3 {0}:{1}; framing={2}. Captura persistida sem criar ocorrencias.' -f $Sg3Address,$Sg3Port,$Sg3Framing)
        if ((Read-Host 'Confirme que consumidor anterior esta parado e framing correto. Digite CAPTURAR') -cne 'CAPTURAR') { throw 'Captura real nao autorizada.' }
    }
}
$backupRoot = Join-Path (Join-Path $env:ProgramData 'Visep-Installer-Backups') ((Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N'))
$installBackup = Join-Path $backupRoot 'binaries'
$dataBackup = Join-Path $backupRoot 'data'
$installExisted = Test-Path -LiteralPath $InstallDirectory
$dataExisted = Test-Path -LiteralPath $DataDirectory
$changed = $false
$createdService = $false
$startAttempted = $false
try {
    if ($service -and $wasRunning) { Stop-Service VisepReceiver; (Get-Service VisepReceiver).WaitForStatus('Stopped', (New-TimeSpan -Seconds 30)) }
    New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
    & icacls.exe $backupRoot '/inheritance:r' '/grant:r' '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao proteger backup.' }
    if ($installExisted) { Copy-VisepVerifiedTree $InstallDirectory $installBackup }
    if ($dataExisted) { Copy-VisepVerifiedTree $DataDirectory $dataBackup }
    if ($previousCommand) { Set-Content (Join-Path $backupRoot 'service-command.txt') $previousCommand }
    $migration = $SourceDataFile -and $SourceDataFile -ne $data
    if ($migration) { Copy-VisepVerifiedTree (Split-Path $SourceDataFile -Parent) (Join-Path $backupRoot 'migration-source') }
    $changed = $true
    New-Item -ItemType Directory -Force -Path $InstallDirectory,$DataDirectory | Out-Null
    if ($migration) {
        # Replace the complete data directory; never merge inbox or journal from different bases.
        foreach ($entry in @(Get-ChildItem -LiteralPath $DataDirectory -Force)) { Remove-Item -LiteralPath $entry.FullName -Recurse -Force }
        Copy-VisepVerifiedTree (Join-Path $backupRoot 'migration-source') $DataDirectory
        if ([IO.Path]::GetFileName($SourceDataFile) -ne [IO.Path]::GetFileName($data)) { Copy-Item -LiteralPath (Join-Path $DataDirectory ([IO.Path]::GetFileName($SourceDataFile))) -Destination $data -Force }
        $activeCapture = Join-Path $dataBackup 'sg3-active-test'
        if (Test-Path -LiteralPath $activeCapture) {
            Copy-VisepVerifiedTree $activeCapture (Join-Path $DataDirectory 'sg3-active-test')
            Write-Host 'Captura SG3 preservada: sg3-active-test.'
        }
    }
    Get-ChildItem $source | Where-Object { !$_.PSIsContainer -and $_.Name -notmatch 'Tests' -and $_.Extension -match '^\.(exe|dll|config|pdb)$' } | Copy-Item -Destination $InstallDirectory -Force
    foreach ($directory in @('migration','runtime')) {
        $resource = Join-Path $root $directory
        if (Test-Path -LiteralPath $resource -PathType Container) { Copy-VisepVerifiedTree $resource (Join-Path $InstallDirectory $directory) }
    }
    & icacls.exe $DataDirectory '/grant' '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' '*S-1-5-19:(OI)(CI)M' ($OperatorAccount + ':(OI)(CI)M') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Falha ACL dados.' }
    & icacls.exe $InstallDirectory '/grant' '*S-1-5-19:(OI)(CI)RX' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Falha ACL binarios.' }
    $exe = Join-Path $InstallDirectory 'Visep.Receiver.exe'
    $bin = '"' + $exe + '" "' + $data + '"'
    if ($ReceiverMode -eq 'sg3') { $bin = '"' + $exe + '" --service-sg3 "' + $data + '" ' + $Sg3Address + ' ' + $Sg3Port + ' ' + $Sg3Framing }
    if ($service) { $result = $service.Change($null,$bin) } else {
        $serviceClass = [wmiclass]'Win32_Service'
        $result = $serviceClass.Create('VisepReceiver','VISEP - Receptor',$bin,16,1,'Automatic',$false,'NT AUTHORITY\LocalService',$null,$null,$null,$null)
        if ($result -and $result.ReturnValue -eq 0) { $createdService = $true }
    }
    if (!$result -or $result.ReturnValue -ne 0) { throw 'Falha WMI ao configurar servico.' }
    Copy-VisepVerifiedTree (Join-Path $root 'scripts') (Join-Path $InstallDirectory 'scripts')
    $config = New-Object Xml.XmlDocument
    $node = $config.CreateElement('Receiver')
    foreach ($pair in @(@('DataFile',$data),@('Mode',$ReceiverMode),@('Address',$Sg3Address),@('Port',[string]$Sg3Port),@('Framing',$Sg3Framing))) { $node.SetAttribute($pair[0],$pair[1]) }
    $null = $config.AppendChild($node)
    $config.Save((Join-Path $InstallDirectory 'receiver-config.xml'))
    $shortcut = (New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'VISEP.lnk'))
    $shortcut.TargetPath = Join-Path $InstallDirectory 'Visep.Desktop.exe'
    $shortcut.Arguments = '"' + $data + '"'
    $shortcut.Save()
    $startAttempted = $true
    Start-Service VisepReceiver
    (Get-Service VisepReceiver).WaitForStatus('Running', (New-TimeSpan -Seconds 30))
    Write-Host ('Instalacao/atualizacao concluida. Dados: ' + $data)
    Write-Host ('Backup verificado: ' + $backupRoot)
    Write-Host ('Servico iniciado. Modo: ' + $ReceiverMode + '. Abra o atalho VISEP.')
} catch {
    $failure = $_
    try {
        $current = Get-Service VisepReceiver -ErrorAction SilentlyContinue
        if ($current -and $current.Status -ne 'Stopped') { Stop-Service VisepReceiver; $current.WaitForStatus('Stopped',(New-TimeSpan -Seconds 30)) }
        if ($startAttempted -and (Test-Path -LiteralPath $DataDirectory)) { Copy-VisepVerifiedTree $DataDirectory (Join-Path $backupRoot 'post-start-data') }
        if ($changed) {
            foreach ($target in @($InstallDirectory,$DataDirectory)) {
                $resolved = [IO.Path]::GetFullPath($target).TrimEnd('\')
                if ($resolved -eq [IO.Path]::GetPathRoot($resolved).TrimEnd('\')) { throw 'Rollback recusou raiz de disco.' }
                foreach ($entry in @(Get-ChildItem -LiteralPath $resolved -Force)) { Remove-Item -LiteralPath $entry.FullName -Recurse -Force }
            }
            & icacls.exe $backupRoot '/inheritance:r' '/grant:r' '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao proteger backup.' }
    if ($installExisted) { Copy-VisepVerifiedTree $installBackup $InstallDirectory }
            if ($dataExisted) { Copy-VisepVerifiedTree $dataBackup $DataDirectory }
        }
        if ($service) { $restored = (Get-WmiObject Win32_Service -Filter "Name='VisepReceiver'").Change($null,$previousCommand); if ($restored.ReturnValue -ne 0) { throw 'Falha ao restaurar comando do servico.' } }
        if ($createdService) { $deleted = (Get-WmiObject Win32_Service -Filter "Name='VisepReceiver'").Delete(); if ($deleted.ReturnValue -ne 0) { throw 'Falha ao remover servico novo.' } }
        if ($wasRunning) { Start-Service VisepReceiver }
    } catch { Write-Warning ('Rollback incompleto: ' + $_.Exception.Message + '. Backup: ' + $backupRoot) }
    throw $failure
}

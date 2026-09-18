param([int]$TimeoutSeconds = 20)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$receiver = Join-Path $root 'build\Visep.Receiver.exe'
$inspector = Join-Path $root 'build\Visep.LegacyInspector.exe'
foreach ($executable in @($receiver, $inspector)) {
    if (!(Test-Path -LiteralPath $executable -PathType Leaf)) { throw 'Execute scripts/Build.ps1 antes dos testes.' }
}
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
$sandbox = Join-Path $temporaryRoot ('visep-integration-' + [Guid]::NewGuid().ToString('N'))
$dataDirectory = Join-Path $sandbox 'data'
$dataFile = Join-Path $dataDirectory 'data.xml'
$inbox = Join-Path $dataDirectory 'inbox'
$script:assertions = 0
$process = $null
function Assert-True([bool]$Condition, [string]$Message) {
    if (!$Condition) { throw $Message }
    $script:assertions++
}
function Wait-ForFile([string]$Path) {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while (!(Test-Path -LiteralPath $Path)) {
        if ($process.HasExited) { throw ('Receptor encerrou: ' + $process.ExitCode) }
        if ([DateTime]::UtcNow -ge $deadline) { throw ('Timeout aguardando ' + $Path) }
        Start-Sleep -Milliseconds 100
    }
}
function Write-Message([string]$Name, [string]$Content) {
    $staging = Join-Path $inbox ($Name + '.tmp')
    [IO.File]::WriteAllText($staging, $Content)
    Move-Item -LiteralPath $staging -Destination (Join-Path $inbox ($Name + '.xml'))
}
function Assert-Rejected([scriptblock]$Action, [string]$Message) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    Assert-True $rejected $Message
}
try {
    New-Item -ItemType Directory -Path $inbox | Out-Null
    $process = Start-Process -FilePath $receiver -ArgumentList @('--console', ('"' + $dataFile + '"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $sandbox 'receiver.stdout') -RedirectStandardError (Join-Path $sandbox 'receiver.stderr')
    $valid = '<simulation id="synthetic-1" account="TEST001" code="130" zone="001" partition="01" originUtc="2026-01-01T00:00:00Z" />'
    Write-Message 'valid' $valid
    Wait-ForFile (Join-Path $inbox 'processed\valid.xml')
    [xml]$state = Get-Content -LiteralPath $dataFile -Raw
    Assert-True (@($state.Visep.Incidents.Incident).Count -eq 1) 'Evento nao persistido.'
    Assert-True ($state.Visep.Incidents.Incident.MessageId -eq 'synthetic-1') 'MessageId incorreto.'
    Assert-True ($state.Visep.Incidents.Incident.Raw -like 'SIMULATION|*') 'Identificacao de simulacao ausente.'
    $incidentId = $state.Visep.Incidents.Incident.Id
    Write-Message 'duplicate' $valid
    Wait-ForFile (Join-Path $inbox 'processed\duplicate.xml')
    [xml]$state = Get-Content -LiteralPath $dataFile -Raw
    Assert-True (@($state.Visep.Incidents.Incident).Count -eq 1) 'Mensagem duplicada criou evento.'
    Assert-True ($state.Visep.Incidents.Incident.Id -eq $incidentId) 'Duplicata alterou identidade.'
    Write-Message 'invalid' '<simulation id="invalid" account="TEST001" />'
    Write-Message 'dtd' '<!DOCTYPE simulation [<!ENTITY probe "synthetic">]><simulation id="dtd" account="&probe;" code="130" zone="001" partition="01" />'
    foreach ($name in @('invalid', 'dtd')) {
        Wait-ForFile (Join-Path $inbox ('quarantine\' + $name + '.xml.error.txt'))
        Assert-True (Test-Path -LiteralPath (Join-Path $inbox ('quarantine\' + $name + '.xml'))) 'Quarentena ausente.'
    }
    $process.Kill()
    $process.WaitForExit()
    $process.Dispose()
    $process = $null
    [xml]$state = Get-Content -LiteralPath $dataFile -Raw
    Assert-True (@($state.Visep.Incidents.Incident).Count -eq 1) 'Mensagem invalida persistida.'

    $captureDirectory = Join-Path $inbox 'captures'
    $captures = @(Get-ChildItem -LiteralPath $captureDirectory -Filter '*.xml' | ForEach-Object {
        ([xml](Get-Content -LiteralPath $_.FullName -Raw)).capture
    })
    Assert-True ($captures.Count -eq 4) 'Capturas nao preservaram cada entrega, inclusive duplicata.'
    Assert-True (@($captures | Where-Object { $_.state -eq 'delivered' }).Count -eq 2) 'Estado de entrega incorreto.'
    Assert-True (@($captures | Where-Object { $_.state -eq 'invalid' }).Count -eq 2) 'Estado de entrada invalida incorreto.'
    $originalCapture = $captures | Where-Object { $_.source -eq 'valid.xml' }
    $duplicateCapture = $captures | Where-Object { $_.source -eq 'duplicate.xml' }
    Assert-True ($originalCapture.id -ne $duplicateCapture.id) 'Retransmissao perdeu identidade propria.'
    Assert-True ($originalCapture.payloadSha256 -eq $duplicateCapture.payloadSha256) 'Payload identico perdeu correlacao.'
    Assert-True ($originalCapture.capturedUtc -match 'Z$') 'Instante da captura deve ser UTC.'

    $journalDirectory = Join-Path $inbox 'journal'
    Assert-True (Test-Path -LiteralPath $journalDirectory) 'Journal duravel ausente.'
    $journalBefore = @(Get-ChildItem -LiteralPath $journalDirectory -File -Recurse)
    Assert-True ($journalBefore.Count -gt 0) 'Journal vazio apos recepcao.'
    & $receiver --replay $dataFile | Out-Null
    Assert-True ($LASTEXITCODE -eq 1) 'Replay nao reportou entradas invalidas preservadas.'
    [xml]$state = Get-Content -LiteralPath $dataFile -Raw
    Assert-True (@($state.Visep.Incidents.Incident).Count -eq 1) 'Replay duplicou ocorrencia.'
    Assert-True ($state.Visep.Incidents.Incident.Id -eq $incidentId) 'Replay alterou identidade.'
    Assert-True (@(Get-ChildItem -LiteralPath $captureDirectory -Filter '*.xml').Count -eq 4) 'Replay inventou novas capturas.'

    $sql = Join-Path $sandbox 'synthetic.sql'
    @'
-- Server version 5.7.44
CREATE TABLE `sample` (
  `id` INT NOT NULL,
  `label` VARCHAR(100) DEFAULT 'SYNTHETIC_PRIVATE_DEFAULT',
  PRIMARY KEY (`id`)
) DEFAULT CHARSET=utf8mb4;
INSERT INTO sample VALUES (1, 'SYNTHETIC_PRIVATE_ROW');
DROP TABLE obsolete;
CREATE VIEW sample_view AS SELECT id FROM sample;
'@ | Set-Content -LiteralPath $sql -Encoding UTF8
    $report = (& $inspector $sql | Out-String)
    Assert-True ($LASTEXITCODE -eq 0) 'Inspector falhou.'
    Assert-True ($report -match 'tabelas: 1; colunas: 2; indices: 1') 'Contagem estrutural incorreta.'
    Assert-True ($report -match 'INSERT/REPLACE: 1; objetos programaveis/views: 1; comandos potencialmente destrutivos: 1') 'Contagem SQL incorreta.'
    Assert-True ($report -match '5\.7\.44' -and $report -match 'utf8mb4') 'Metadados SQL ausentes.'
    Assert-True ($report -notmatch 'SYNTHETIC_PRIVATE') 'Inspector vazou valores SQL.'

    $backup = Join-Path $sandbox 'backup'
    $restored = Join-Path $sandbox 'restored'
    & (Join-Path $root 'scripts\Backup.ps1') -DataDirectory $dataDirectory -Destination $backup | Out-Null
    $retentionPreview = (& $receiver --retention $dataFile $backup '2099-01-01T00:00:00Z' | Out-String)
    Assert-True ($LASTEXITCODE -eq 0) 'Previa de retencao com backup valido falhou.'
    Assert-True ($retentionPreview -match 'candidateCaptures=4; candidatePayloads=3') 'Plano de retencao divergente.'
    & $receiver --retention $dataFile $backup '2099-01-01T00:00:00Z' --apply | Out-Null
    Assert-True ($LASTEXITCODE -eq 0) 'Aplicacao da retencao falhou.'
    Assert-True (@(Get-ChildItem -LiteralPath $journalDirectory -Filter '*.raw').Count -eq 0 -and @(Get-ChildItem -LiteralPath $captureDirectory -Filter '*.xml').Count -eq 0) 'Retencao nao removeu somente o conjunto planejado.'
    & (Join-Path $root 'scripts\Restore.ps1') -BackupDirectory $backup -Destination $restored | Out-Null
    $manifest = Get-Content -LiteralPath (Join-Path $backup 'manifest.json') -Raw | ConvertFrom-Json
    foreach ($entry in $manifest) {
        Assert-True ((Get-FileHash -LiteralPath (Join-Path $restored $entry.Path)).Hash -eq $entry.Sha256) ('Restore divergente: ' + $entry.Path)
    }
    Assert-True (@($manifest | Where-Object { $_.Path -like 'inbox\journal\*' }).Count -gt 0) 'Backup omitiu journal.'
    Assert-True (@($manifest | Where-Object { $_.Path -like 'inbox\captures\*' }).Count -eq 4) 'Backup omitiu envelopes de captura.'
    & $receiver --replay (Join-Path $restored 'data.xml') | Out-Null
    Assert-True ($LASTEXITCODE -eq 1) 'Replay restaurado nao reportou entradas invalidas.'
    [xml]$restoredState = Get-Content -LiteralPath (Join-Path $restored 'data.xml') -Raw
    Assert-True (@($restoredState.Visep.Incidents.Incident).Count -eq 1) 'Replay restaurado duplicou ocorrencia.'
    Assert-Rejected { & (Join-Path $root 'scripts\Restore.ps1') -BackupDirectory $backup -Destination $restored } 'Restore sobrescreveu destino existente.'
    Add-Content -LiteralPath (Join-Path $backup 'data.xml') -Value 'tampered'
    $rejectedTarget = Join-Path $sandbox 'rejected'
    Assert-Rejected { & (Join-Path $root 'scripts\Restore.ps1') -BackupDirectory $backup -Destination $rejectedTarget } 'Restore aceitou hash adulterado.'
    Assert-True (!(Test-Path -LiteralPath $rejectedTarget)) 'Restore corrompido criou destino.'
    $singleSource = Join-Path $sandbox 'single-source'
    $singleBackup = Join-Path $sandbox 'single-backup'
    $singleRestore = Join-Path $sandbox 'single-restore'
    New-Item -ItemType Directory -Path $singleSource | Out-Null
    Copy-Item -LiteralPath $dataFile -Destination (Join-Path $singleSource 'data.xml')
    & (Join-Path $root 'scripts\Backup.ps1') -DataDirectory $singleSource -Destination $singleBackup | Out-Null
    & (Join-Path $root 'scripts\Restore.ps1') -BackupDirectory $singleBackup -Destination $singleRestore | Out-Null
    Assert-True ((Get-FileHash -LiteralPath (Join-Path $singleRestore 'data.xml')).Hash -eq (Get-FileHash -LiteralPath $dataFile).Hash) 'Restore de manifesto com um item divergente.'
    $invalidStates = @{
        schema = '<Visep SchemaVersion="99"><Users/><Clients/><Incidents/><Audit/></Visep>'
        missing = '<Visep SchemaVersion="1"><Users/><Clients/><Incidents/></Visep>'
        duplicate = '<Visep SchemaVersion="1"><Users/><Clients/><Incidents/><Audit/><Audit/></Visep>'
        root = '<Invalid SchemaVersion="1"><Users/><Clients/><Incidents/><Audit/></Invalid>'
    }
    foreach ($name in $invalidStates.Keys) {
        $invalidFile = Join-Path $singleBackup 'data.xml'
        [IO.File]::WriteAllText($invalidFile, $invalidStates[$name])
        [pscustomobject]@{ Path = 'data.xml'; Sha256 = (Get-FileHash -LiteralPath $invalidFile).Hash } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $singleBackup 'manifest.json') -Encoding UTF8
        $invalidTarget = Join-Path $sandbox ('invalid-' + $name)
        Assert-Rejected { & (Join-Path $root 'scripts\Restore.ps1') -BackupDirectory $singleBackup -Destination $invalidTarget } ('Restore aceitou estado invalido: ' + $name)
        Assert-True (!(Test-Path -LiteralPath $invalidTarget)) ('Restore invalido criou destino: ' + $name)
    }
    Write-Output ('PASS integration: ' + $script:assertions + ' assertions (receiver, dedup, quarantine, inspector, backup/restore).')
} finally {
    if ($null -ne $process) {
        if (!$process.HasExited) { $process.Kill(); $process.WaitForExit() }
        $process.Dispose()
    }
    $resolved = [IO.Path]::GetFullPath($sandbox)
    if (!$resolved.StartsWith($temporaryRoot + '\', [StringComparison]::OrdinalIgnoreCase) -or !(Split-Path $resolved -Leaf).StartsWith('visep-integration-')) { throw 'Diretorio temporario fora do escopo.' }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}

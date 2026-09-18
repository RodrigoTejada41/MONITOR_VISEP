$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $MyInvocation.MyCommand.Path -Parent) -Parent
. (Join-Path $root 'scripts\Installer.Common.ps1')
$temp = Join-Path ([IO.Path]::GetTempPath()) ('visep-installer-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $temp | Out-Null
try {
    $xml = Join-Path $temp 'data.xml'
    Set-Content $xml '<Visep SchemaVersion="1"><Users><User /></Users><Clients><Client /></Clients><Incidents><Incident /></Incidents><Audit /></Visep>'
    $summary = Get-VisepDataSummary $xml
    if ($summary.Users -ne 1 -or $summary.Incidents -ne 1) { throw 'Incorrect XML summary' }
    $parsed = Get-VisepServicePaths '"C:\Program Files\VISEP\Visep.Receiver.exe" "C:\ProgramData\Visep\data.xml"'
    if ($parsed.DataFile -ne 'C:\ProgramData\Visep\data.xml') { throw 'Legacy command parsing failed' }
    $parsed = Get-VisepServicePaths '"C:\Program Files\VISEP\Visep.Receiver.exe" --service-sg3 "C:\ProgramData\Visep\data.xml" 192.168.1.249 1025 plain'
    if ($parsed.DataFile -ne 'C:\ProgramData\Visep\data.xml') { throw 'SG3 command parsing failed' }
    $backup = Join-Path $temp 'backup'
    New-Item -ItemType Directory (Join-Path $temp 'original') | Out-Null
    Copy-Item $xml (Join-Path $temp 'original\data.xml')
    Copy-VisepVerifiedTree (Join-Path $temp 'original') $backup
    if ((Get-VisepHash $xml) -ne (Get-VisepHash (Join-Path $backup 'data.xml'))) { throw 'Backup mismatch' }
    Set-Content $xml '<Unexpected />'
    $rejected = $false
    try { Get-VisepDataSummary $xml } catch { $rejected = $true }
    if (!$rejected) { throw 'Invalid XML accepted' }
    Write-Output 'Installer pure-function tests passed.'
} finally {
    if ([IO.Path]::GetFullPath($temp).StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase)) { Remove-Item -LiteralPath $temp -Recurse -Force }
}

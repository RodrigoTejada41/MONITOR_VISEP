param([string]$DataFile = (Join-Path $PSScriptRoot '..\data\bykom-teste\data.xml'))
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$data = (Resolve-Path -LiteralPath $DataFile).Path
$desktop = Join-Path $root 'build\Visep.Desktop.exe'
$receiver = Join-Path $root 'build\Visep.Receiver.exe'
if (!(Test-Path -LiteralPath $desktop) -or !(Test-Path -LiteralPath $receiver)) { throw 'Execute scripts\Build.ps1 primeiro.' }
$gate = $null
$process = $null
try {
    $gate = [IO.File]::Open(($data + '.launcher.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $process = Start-Process -FilePath $receiver -ArgumentList @('--console', ('"' + $data + '"')) -WindowStyle Hidden -PassThru
    Start-Process -FilePath $desktop -ArgumentList ('"' + $data + '"') -Wait
} finally {
    if ($null -ne $process) {
        if (!$process.HasExited) { $process.Kill(); $process.WaitForExit() }
        $process.Dispose()
    }
    if ($null -ne $gate) { $gate.Dispose() }
}

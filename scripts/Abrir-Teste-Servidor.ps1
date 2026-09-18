param([string]$DataFile)
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $MyInvocation.MyCommand.Path -Parent) -Parent
if ([String]::IsNullOrEmpty($DataFile)) { $DataFile = Join-Path $root 'data\teste-servidor\data.xml' }
$data = [IO.Path]::GetFullPath($DataFile)
$dataDirectory = Split-Path $data -Parent
$desktop = Join-Path $root 'build\Visep.Desktop.exe'
$receiver = Join-Path $root 'build\Visep.Receiver.exe'
if (!(Test-Path -LiteralPath $desktop -PathType Leaf) -or !(Test-Path -LiteralPath $receiver -PathType Leaf)) { throw 'Pacote incompleto.' }
New-Item -ItemType Directory -Force -Path $dataDirectory | Out-Null
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

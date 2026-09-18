param([switch]$Test)
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $MyInvocation.MyCommand.Path -Parent) -Parent
$out = Join-Path $root 'build'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$refs = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2'
if (!(Test-Path $csc) -or !(Test-Path $refs)) { throw 'Instale .NET Framework 4.7.2 Developer Pack no ambiente de desenvolvimento.' }
New-Item -ItemType Directory -Force -Path $out | Out-Null
$common = @('/nologo','/noconfig','/nostdlib+','/langversion:5','/warn:4','/warnaserror+','/optimize+','/platform:anycpu')
foreach ($name in @('mscorlib','System','System.Core','System.Xml','System.Xml.Linq','System.Runtime.Serialization')) {
    $common += '/reference:' + (Join-Path $refs ($name + '.dll'))
}
function Compile($target, $output, $sources, $extra) {
    & $csc @common ('/target:' + $target) ('/out:' + (Join-Path $out $output)) @extra @sources
    if ($LASTEXITCODE -ne 0) { throw ('Compilacao falhou: ' + $output) }
}
$core = @(Get-ChildItem (Join-Path $root 'src\Core') -Filter '*.cs' | ForEach-Object { $_.FullName })
Compile 'library' 'Visep.Core.dll' $core @()
$coreRef = '/reference:' + (Join-Path $out 'Visep.Core.dll')
Compile 'winexe' 'Visep.Desktop.exe' @(Get-ChildItem (Join-Path $root 'src\Desktop') -Filter '*.cs' | ForEach-Object { $_.FullName }) @($coreRef,('/reference:' + (Join-Path $refs 'System.Windows.Forms.dll')),('/reference:' + (Join-Path $refs 'System.Drawing.dll')))
Compile 'exe' 'Visep.Receiver.exe' @(Get-ChildItem (Join-Path $root 'src\Receiver') -Filter '*.cs' | ForEach-Object { $_.FullName }) @($coreRef,('/reference:' + (Join-Path $refs 'System.ServiceProcess.dll')))
Compile 'exe' 'Visep.LegacyInspector.exe' @(Get-ChildItem (Join-Path $root 'src\Migration') -Filter '*.cs' | ForEach-Object { $_.FullName }) @()
$config = '<?xml version="1.0"?><configuration><startup><supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.7.2"/></startup></configuration>'
foreach ($exe in @('Visep.Desktop.exe','Visep.Receiver.exe','Visep.LegacyInspector.exe')) {
    [IO.File]::WriteAllText((Join-Path $out ($exe + '.config')), $config)
}
if ($Test) {
    Compile 'exe' 'Visep.BootstrapTests.exe' @((Join-Path $root 'tests\InstallerBootstrapTests.cs'),(Join-Path $root 'src\Installer\InstallerBootstrap.cs')) @(('/reference:' + (Join-Path $refs 'System.IO.Compression.dll')), '/main:InstallerBootstrapTests')
    & (Join-Path $out 'Visep.BootstrapTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Testes do bootstrap falharam.' }
    Compile 'exe' 'Visep.Sg3ContinuousTests.exe' (@((Join-Path $root 'tests\Sg3ContinuousTests.cs')) + @(Get-ChildItem (Join-Path $root 'src\Receiver') -Filter '*.cs' | ForEach-Object { $_.FullName })) @($coreRef,('/reference:' + (Join-Path $refs 'System.ServiceProcess.dll')), '/main:Sg3ContinuousTests')
    & (Join-Path $out 'Visep.Sg3ContinuousTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Testes SG3 continuo falharam.' }
    Compile 'exe' 'Visep.Sg3ParserTests.exe' (@((Join-Path $root 'tests\Sg3ParserTests.cs')) + @(Get-ChildItem (Join-Path $root 'src\Receiver') -Filter '*.cs' | ForEach-Object { $_.FullName })) @($coreRef,('/reference:' + (Join-Path $refs 'System.ServiceProcess.dll')), '/main:Sg3ParserTests')
    & (Join-Path $out 'Visep.Sg3ParserTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Testes do parser SG3 falharam.' }
    Compile 'exe' 'Visep.Sg3TransportTests.exe' (@((Join-Path $root 'tests\Sg3TransportTests.cs')) + @(Get-ChildItem (Join-Path $root 'src\Receiver') -Filter '*.cs' | ForEach-Object { $_.FullName })) @($coreRef,('/reference:' + (Join-Path $refs 'System.ServiceProcess.dll')), '/main:Sg3TransportTests')
    & (Join-Path $out 'Visep.Sg3TransportTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Testes do transporte SG3 falharam.' }
    Compile 'exe' 'Visep.RetentionTests.exe' (@((Join-Path $root 'tests\RetentionTests.cs')) + @(Get-ChildItem (Join-Path $root 'src\Receiver') -Filter '*.cs' | ForEach-Object { $_.FullName })) @($coreRef,('/reference:' + (Join-Path $refs 'System.ServiceProcess.dll')), '/main:RetentionTests')
    & (Join-Path $out 'Visep.RetentionTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Testes de retencao falharam.' }
    Compile 'exe' 'Visep.MonitoringTests.exe' (@((Join-Path $root 'tests\MonitoringTests.cs')) + @(Get-ChildItem (Join-Path $root 'src\Receiver') -Filter '*.cs' | ForEach-Object { $_.FullName })) @($coreRef,('/reference:' + (Join-Path $refs 'System.ServiceProcess.dll')), '/main:MonitoringTests')
    & (Join-Path $out 'Visep.MonitoringTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Testes de supervisao falharam.' }
    Compile 'exe' 'Visep.CaptureTests.exe' (@((Join-Path $root 'tests\CaptureTests.cs')) + @(Get-ChildItem (Join-Path $root 'src\Receiver') -Filter '*.cs' | ForEach-Object { $_.FullName })) @($coreRef,('/reference:' + (Join-Path $refs 'System.ServiceProcess.dll')), '/main:CaptureTests')
    & (Join-Path $out 'Visep.CaptureTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Testes de captura falharam.' }
    Compile 'exe' 'Visep.JournalTests.exe' (@((Join-Path $root 'tests\JournalTests.cs')) + @(Get-ChildItem (Join-Path $root 'src\Receiver') -Filter '*.cs' | ForEach-Object { $_.FullName })) @($coreRef,('/reference:' + (Join-Path $refs 'System.ServiceProcess.dll')), '/main:JournalTests')
    & (Join-Path $out 'Visep.JournalTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Testes do journal falharam.' }
    Compile 'exe' 'Visep.Tests.exe' @((Join-Path $root 'tests\CoreTests.cs')) @($coreRef)
    & (Join-Path $out 'Visep.Tests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Testes do nucleo falharam.' }
    Compile 'exe' 'Visep.LegacyHistoryTests.exe' @((Join-Path $root 'tests\LegacyHistoryTests.cs')) @($coreRef)
    & (Join-Path $out 'Visep.LegacyHistoryTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Testes do historico legado falharam.' }
}
Write-Output ('Build concluido: ' + $out)

$ErrorActionPreference = 'Stop'
$os = Get-WmiObject Win32_OperatingSystem
$release = 0
foreach ($path in @('HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full','HKLM:\SOFTWARE\Wow6432Node\Microsoft\NET Framework Setup\NDP\v4\Full')) {
    if (Test-Path $path) {
        $value = (Get-ItemProperty $path -ErrorAction SilentlyContinue).Release
        if ($null -ne $value -and [int]$value -gt $release) { $release = [int]$value }
    }
}
$powerShellMajor = $PSVersionTable.PSVersion.Major
$osVersion = New-Object Version $os.Version
$minimumVersion = New-Object Version '6.1'
$osFamily = New-Object Version ($osVersion.Major.ToString() + "." + $osVersion.Minor.ToString())
$osOk = ($osFamily -gt $minimumVersion) -or ($osFamily -eq $minimumVersion -and $os.ServicePackMajorVersion -ge 1)
$checks = @(
    (New-Object PSObject -Property @{ Item='Windows 6.1 SP1 ou posterior'; Ok=$osOk; Value=($os.Caption + ' SP' + $os.ServicePackMajorVersion) }),
    (New-Object PSObject -Property @{ Item='Sistema x64'; Ok=($os.OSArchitecture -match '64'); Value=$os.OSArchitecture }),
    (New-Object PSObject -Property @{ Item='PowerShell 2+'; Ok=($powerShellMajor -ge 2); Value=$PSVersionTable.PSVersion.ToString() }),
    (New-Object PSObject -Property @{ Item='.NET Framework 4.7.2+'; Ok=($release -ge 461808); Value=('Release=' + $release) })
)
$checks | Format-Table -AutoSize
if (@($checks | Where-Object { !$_.Ok }).Count -gt 0) {
    Write-Error 'Ambiente nao atende aos pre-requisitos. Nao instale o servico.'
    exit 1
}
Write-Output 'Ambiente compativel documentalmente. Continue com o teste sem instalar.'

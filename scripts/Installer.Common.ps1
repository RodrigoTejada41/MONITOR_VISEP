function Get-VisepHash([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($stream)) } finally { $stream.Dispose(); $sha.Dispose() }
}
function Get-VisepDataSummary([string]$Path) {
    $doc = New-Object System.Xml.XmlDocument
    $settings = New-Object System.Xml.XmlReaderSettings
    $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $reader = [System.Xml.XmlReader]::Create($Path,$settings)
    try { $doc.XmlResolver = $null; $doc.Load($reader) } finally { $reader.Close() }
    if ($doc.DocumentElement.Name -ne 'Visep' -or $doc.DocumentElement.GetAttribute('SchemaVersion') -ne '1') { throw ('Base XML invalida: ' + $Path) }
    foreach ($section in @('Users','Clients','Incidents','Audit')) {
        if ($doc.SelectNodes('/Visep/' + $section).Count -ne 1) { throw ('Secao ausente ou duplicada: ' + $section) }
    }
    New-Object PSObject -Property @{ Path = [IO.Path]::GetFullPath($Path); Users = $doc.SelectNodes('/Visep/Users/*').Count; Clients = $doc.SelectNodes('/Visep/Clients/*').Count; Incidents = $doc.SelectNodes('/Visep/Incidents/*').Count; Hash = (Get-VisepHash $Path) }
}
function Get-VisepServicePaths([string]$CommandLine) {
    $tokens = @([regex]::Matches($CommandLine, '"([^"]*)"|(\S+)') | ForEach-Object { if ($_.Groups[1].Success) { $_.Groups[1].Value } else { $_.Groups[2].Value } })
    if ($tokens.Count -lt 2 -or [IO.Path]::GetFileName($tokens[0]) -ne 'Visep.Receiver.exe') { throw 'Linha de comando do servico desconhecida; atualizacao cancelada.' }
    $index = 1
    if ($tokens[1] -eq '--service-sg3') { $index = 2 }
    if ($tokens.Count -le $index -or ![IO.Path]::IsPathRooted($tokens[$index])) { throw 'Caminho de dados do servico invalido.' }
    New-Object PSObject -Property @{ Executable = $tokens[0]; DataFile = $tokens[$index] }
}
function Copy-VisepVerifiedTree([string]$Source, [string]$Destination) {
    $sourceFull = [IO.Path]::GetFullPath($Source).TrimEnd('\')
    $destinationFull = [IO.Path]::GetFullPath($Destination).TrimEnd('\')
    if ($destinationFull.StartsWith($sourceFull + '\', [StringComparison]::OrdinalIgnoreCase) -or $sourceFull -eq $destinationFull) { throw 'Backup nao pode ficar dentro da origem.' }
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    foreach ($item in @(Get-ChildItem -LiteralPath $Source -Recurse -Force)) {
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw ('Link nao permitido no backup: ' + $item.FullName) }
        $relative = $item.FullName.Substring($sourceFull.Length).TrimStart('\')
        $target = Join-Path $Destination $relative
        if ($item.PSIsContainer) { New-Item -ItemType Directory -Force -Path $target | Out-Null } else {
            Copy-Item -LiteralPath $item.FullName -Destination $target -Force
            if ((Get-VisepHash $item.FullName) -ne (Get-VisepHash $target)) { throw ('Backup divergente: ' + $relative) }
        }
    }
}
function Find-VisepBases([string[]]$Roots, [string]$ExistingData) {
    $paths = @($ExistingData)
    foreach ($searchRoot in $Roots) {
        if (Test-Path -LiteralPath $searchRoot) { $paths += @(Get-ChildItem -LiteralPath $searchRoot -Filter data.xml -Recurse -ErrorAction Stop | Where-Object { !$_.PSIsContainer } | ForEach-Object { $_.FullName }) }
    }
    foreach ($path in @($paths | Select-Object -Unique)) { if ($path -and (Test-Path -LiteralPath $path)) { Get-VisepDataSummary $path } }
}

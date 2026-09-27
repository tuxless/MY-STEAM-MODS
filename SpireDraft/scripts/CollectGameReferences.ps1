param([string]$GameDataDir, [ValidateSet('stable','beta')][string]$Branch = 'stable')
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Split-Path -Parent $PSScriptRoot
$GameDataDir = Resolve-GameData $GameDataDir
$temp = Join-Path $root ('reference-export-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    $files = @('sts2.dll','0Harmony.dll','GodotSharp.dll','SmartFormat.dll')
    foreach ($name in $files) {
        $source = Join-Path $GameDataDir $name
        if (-not (Test-Path -LiteralPath $source)) { throw "Missing $name. Select the game's data folder." }
        Copy-Item -LiteralPath $source -Destination (Join-Path $temp $name)
    }
    $release = Join-Path (Split-Path -Parent $GameDataDir) 'release_info.json'
    if (Test-Path -LiteralPath $release) { Copy-Item -LiteralPath $release -Destination (Join-Path $temp 'release_info.json') }
    $metadata = [ordered]@{ branch=$Branch; sha256=(Get-FileHash -LiteralPath (Join-Path $temp 'sts2.dll') -Algorithm SHA256).Hash }
    Write-Utf8 (Join-Path $temp 'reference-info.json') ($metadata | ConvertTo-Json)
    $zip = Join-Path $root ("GameReferences-$Branch-" + [DateTime]::Now.ToString('yyyyMMdd-HHmmss') + '.zip')
    Compress-Archive -Path (Join-Path $temp '*') -DestinationPath $zip
    Write-Host "Created: $zip"
    Write-Host 'Attach this ZIP in the conversation for compilation. Do not put it on Steam Workshop.'
    Write-Host 'This contains only the listed game build dependencies and release metadata, not your saves or Steam account data.'
} finally { Remove-Item -LiteralPath $temp -Recurse -Force }

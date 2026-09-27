$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
function Resolve-GameData([string]$GameDataDir) {
    if ($GameDataDir -and (Test-Path -LiteralPath (Join-Path $GameDataDir 'sts2.dll'))) {
        return (Resolve-Path -LiteralPath $GameDataDir).Path
    }
    $roots = @()
    try { $roots += (Get-ItemProperty 'HKCU:\Software\Valve\Steam').SteamPath } catch {}
    if (${env:ProgramFiles(x86)}) { $roots += (Join-Path ${env:ProgramFiles(x86)} 'Steam') }
    $libraries = @($roots | Where-Object { $_ } | Select-Object -Unique)
    foreach ($steam in $roots) {
        $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
        if (Test-Path -LiteralPath $vdf) {
            $text = Get-Content -Raw -LiteralPath $vdf
            foreach ($match in [regex]::Matches($text, '"path"\s+"([^"]+)"')) {
                $libraries += $match.Groups[1].Value.Replace('\\','\')
            }
        }
    }
    $candidates = @()
    foreach ($lib in ($libraries | Select-Object -Unique)) {
        $game = Join-Path $lib 'steamapps\common\Slay the Spire 2'
        if (Test-Path -LiteralPath $game) {
            $candidates += @(Get-ChildItem -LiteralPath $game -Filter sts2.dll -File -Recurse | ForEach-Object { $_.DirectoryName })
        }
    }
    # Recent launchers can keep the downloaded game under the Godot user-data directory.
    foreach ($game in @((Join-Path $env:APPDATA 'SlayTheSpire2\game'), (Join-Path $env:APPDATA 'Godot\app_userdata\Slay the Spire 2\game'))) {
        if (Test-Path -LiteralPath $game) {
            $candidates += @(Get-ChildItem -LiteralPath $game -Filter sts2.dll -File -Recurse | ForEach-Object { $_.DirectoryName })
        }
    }
    $candidates = @($candidates | Select-Object -Unique)
    if ($candidates.Count -eq 1) { return $candidates[0] }
    if ($candidates.Count -gt 1) {
        for ($i=0; $i -lt $candidates.Count; $i++) { Write-Host "[$i] $($candidates[$i])" }
        $selection = Read-Host 'Choose the active game folder number'
        $index = 0
        if ([int]::TryParse($selection,[ref]$index) -and $index -ge 0 -and $index -lt $candidates.Count) { return $candidates[$index] }
    }
    $typed = (Read-Host 'Paste the folder containing sts2.dll (without quotes)').Trim().Trim('"')
    if (-not (Test-Path -LiteralPath (Join-Path $typed 'sts2.dll'))) { throw 'sts2.dll was not found in that folder.' }
    return (Resolve-Path -LiteralPath $typed).Path
}
function Write-Utf8([string]$Path,[string]$Text) {
    [System.IO.File]::WriteAllText($Path,$Text,(New-Object System.Text.UTF8Encoding($false)))
}

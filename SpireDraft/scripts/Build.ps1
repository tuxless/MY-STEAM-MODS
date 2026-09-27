param([string]$GameDataDir, [ValidateSet('stable','beta')][string]$Branch = 'stable')
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Split-Path -Parent $PSScriptRoot
$GameDataDir = Resolve-GameData $GameDataDir
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) { throw 'Install .NET 9 SDK (not only Runtime) from https://dotnet.microsoft.com/download/dotnet/9.0 and run Build.cmd again.' }
if (-not (Test-Path -LiteralPath (Join-Path $GameDataDir '0Harmony.dll'))) { throw '0Harmony.dll is missing. Select the complete game data folder.' }
Write-Host 'Running decision and persistence tests...'
& dotnet run --project (Join-Path $root 'tests\SpireDraft.Tests.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed. No upload package was produced.' }
$build = Join-Path $root "build\$Branch"
& dotnet build (Join-Path $root 'src\SpireDraft.csproj') -c Release "-p:GameDataDir=$GameDataDir" -o $build
if ($LASTEXITCODE -ne 0) { throw 'Mod compilation failed. No upload package was produced. Keep the error output for diagnosis.' }
$workspace = Join-Path $root "dist\SpireDraft-$Branch"
$content = Join-Path $workspace 'content'
New-Item -ItemType Directory -Force -Path $content | Out-Null
# Copy an allowlist only: never publish game DLLs, framework DLLs, or personal settings.
Copy-Item -LiteralPath (Join-Path $build 'SpireDraft.dll') -Destination (Join-Path $content 'SpireDraft.dll') -Force
Copy-Item -LiteralPath (Join-Path $root 'workshop\content\SpireDraft.json') -Destination (Join-Path $content 'SpireDraft.json') -Force
if (-not (Test-Path -LiteralPath (Join-Path $workspace 'workshop.json'))) {
    Copy-Item -LiteralPath (Join-Path $root 'workshop\workshop.json') -Destination (Join-Path $workspace 'workshop.json')
}
Copy-Item -LiteralPath (Join-Path $root 'workshop\image.png') -Destination (Join-Path $workspace 'image.png') -Force
$report = [ordered]@{
    createdUtc = [DateTime]::UtcNow.ToString('o'); branch = $Branch; coreTests = 'passed';
    gameSha256 = (Get-FileHash -LiteralPath (Join-Path $GameDataDir 'sts2.dll') -Algorithm SHA256).Hash;
    dllSha256 = (Get-FileHash -LiteralPath (Join-Path $content 'SpireDraft.dll') -Algorithm SHA256).Hash;
    ritsuLib = '0.6.2'; gameplayTest = 'not yet performed';
    note = 'Compilation does not prove in-game compatibility. Test this workspace with the selected game branch.'
}
Write-Utf8 (Join-Path $workspace 'build-report.json') ($report | ConvertTo-Json -Depth 5)
& (Join-Path $PSScriptRoot 'ValidateWorkshop.ps1') -Workspace $workspace
Write-Host "Created upload workspace: $workspace"
Write-Host "For local testing copy ONLY the content folder contents into the active game's mods\SpireDraft folder."
Write-Host 'Subscribe to RitsuLib separately. Do not install duplicate copies of SpireDraft.'
Write-Host 'After testing, run Upload.cmd and select this workspace.'

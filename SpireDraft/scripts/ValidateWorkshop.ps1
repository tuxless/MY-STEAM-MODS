param([Parameter(Mandatory=$true)][string]$Workspace, [switch]$Template)
. (Join-Path $PSScriptRoot 'Common.ps1')

if (-not (Test-Path -LiteralPath $Workspace -PathType Container)) { throw "Workspace directory not found: $Workspace" }
$imagePath = Join-Path $Workspace 'image.png'
$configPath = Join-Path $Workspace 'workshop.json'
$contentPath = Join-Path $Workspace 'content'
$manifestPath = Join-Path $contentPath 'SpireDraft.json'
$dllPath = Join-Path $contentPath 'SpireDraft.dll'
foreach ($path in @($imagePath, $configPath, $manifestPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Required official uploader file missing: $path" }
}
if (-not (Test-Path -LiteralPath $contentPath -PathType Container)) { throw "Missing content directory: $contentPath" }
$image = Get-Item -LiteralPath $imagePath
if ($image.Length -ge 1048576) { throw 'image.png must be smaller than 1 MiB.' }
$imageBytes = [System.IO.File]::ReadAllBytes($imagePath)
$pngHeader = [byte[]]@(137,80,78,71,13,10,26,10)
if ($imageBytes.Length -lt 8) { throw 'image.png is empty or invalid.' }
for ($i=0; $i -lt 8; $i++) {
    if ($imageBytes[$i] -ne $pngHeader[$i]) { throw 'image.png is not a PNG file.' }
}

$config = Get-Content -Raw -Encoding UTF8 -LiteralPath $configPath | ConvertFrom-Json
$allowedKeys = @('title','description','visibility','changeNote','tags','dependencies','contentDescriptors','minBranch','maxBranch')
$unknownKeys = @($config.PSObject.Properties.Name | Where-Object { $_ -notin $allowedKeys })
if ($unknownKeys.Count -gt 0) { throw "Unknown official workshop.json field(s): $($unknownKeys -join ', ')" }
foreach ($key in @('title','description','visibility','changeNote','tags','dependencies','contentDescriptors')) {
    if ($key -notin $config.PSObject.Properties.Name) { throw "workshop.json needs field: $key" }
}
if ([string]::IsNullOrWhiteSpace($config.title) -or [string]::IsNullOrWhiteSpace($config.description)) {
    throw 'workshop.json title and description must not be empty.'
}
if ($config.visibility -notin @('private','public','unlisted','friends_only')) { throw 'Invalid workshop visibility.' }
if ($null -eq $config.tags -or $null -eq $config.contentDescriptors) { throw 'tags and contentDescriptors must be arrays.' }
if (@($config.dependencies).Count -ne 1 -or [string]$config.dependencies[0] -ne '3747602295') {
    throw 'workshop.json must declare RitsuLib Workshop item 3747602295 as its dependency.'
}
if ($config.dependencies[0] -is [string]) { throw 'Workshop dependencies must be numeric IDs, not strings.' }

$manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
if ($manifest.id -ne 'SpireDraft' -or $manifest.has_dll -ne $true -or $manifest.has_pck -ne $false) {
    throw 'content/SpireDraft.json must describe the SpireDraft DLL mod without a PCK.'
}
if ([string]::IsNullOrWhiteSpace($manifest.author)) { throw 'Set the mod author in content/SpireDraft.json.' }
if (@($manifest.dependencies | Where-Object { $_.id -eq 'STS2-RitsuLib' }).Count -ne 1) {
    throw 'content/SpireDraft.json must declare its in-game RitsuLib dependency.'
}
$allowedContent = @('SpireDraft.json')
if (-not $Template) {
    if (-not (Test-Path -LiteralPath $dllPath -PathType Leaf)) { throw 'No compiled SpireDraft.dll. Build.cmd must finish before upload.' }
    $dll = [System.IO.File]::ReadAllBytes($dllPath)
    if ($dll.Length -lt 2 -or $dll[0] -ne 77 -or $dll[1] -ne 90) { throw 'SpireDraft.dll does not have a valid Windows DLL header.' }
    $allowedContent += 'SpireDraft.dll'
}
$entries = @(Get-ChildItem -LiteralPath $contentPath -Force)
$unexpected = @($entries | Where-Object { $_.Name -notin $allowedContent -or $_.PSIsContainer })
if ($unexpected.Count -gt 0) { throw "Unexpected files in content/: $($unexpected.Name -join ', ')" }
$modIdPath = Join-Path $Workspace 'mod_id.txt'
if (Test-Path -LiteralPath $modIdPath) {
    $modId = [uint64]0
    $value = (Get-Content -Raw -LiteralPath $modIdPath).Trim()
    if (-not [uint64]::TryParse($value, [ref]$modId) -or $modId -eq 0) { throw 'mod_id.txt must contain a Steam Workshop numeric item ID.' }
}
Write-Host "Official uploader workspace validated: $Workspace"

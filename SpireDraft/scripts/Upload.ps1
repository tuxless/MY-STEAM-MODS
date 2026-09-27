param([string]$Uploader, [ValidateSet('stable','beta')][string]$Branch='stable')
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Split-Path -Parent $PSScriptRoot
$workspace = Join-Path $root "dist\SpireDraft-$Branch"
& (Join-Path $PSScriptRoot 'ValidateWorkshop.ps1') -Workspace $workspace
if (-not $Uploader) { $Uploader = (Read-Host 'Paste the official ModUploader.exe path').Trim().Trim('"') }
if (-not (Test-Path -LiteralPath $Uploader)) { throw 'Download the official uploader from https://github.com/megacrit/sts2-mod-uploader first.' }
$manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $workspace 'workshop.json') | ConvertFrom-Json
Write-Host "Uploading $workspace with visibility: $($manifest.visibility)"
Push-Location (Split-Path -Parent $Uploader)
try {
    & $Uploader upload -w $workspace
    if ($LASTEXITCODE -ne 0) { throw 'Upload failed. See mod-uploader.log next to ModUploader.exe.' }
} finally { Pop-Location }

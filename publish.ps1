# ==============================================================================
# mepOS — Release & Distribution Publisher
# Builds Release binaries, updates version.json, and stages for GitHub deployment
# ==============================================================================
param(
    [string]$Version = "1.0.1",
    [string]$Changelog = "Added Bloom [BL] stub generator and Connect To [CT] 3D auto-alignment"
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
if (-not (Test-Path $msbuild)) {
    $cmd = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
    if ($cmd) { $msbuild = $cmd.Source }
}

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "  mepOS // RELEASING VERSION $Version" -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan

# 1. Compile Release Configuration
$coreProj = Join-Path $scriptDir "src\mepOS.Core\mepOS.Core.csproj"
$loaderProj = Join-Path $scriptDir "src\mepOS.Loader\mepOS.Loader.csproj"

Write-Host "`n[1/3] Compiling mepOS.Core (Release)..." -ForegroundColor Cyan
& "$msbuild" "$coreProj" /p:Configuration=Release /p:Platform="AnyCPU" /v:m
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to compile mepOS.Core in Release mode."
    exit 1
}

Write-Host "`n[2/3] Compiling mepOS.Loader (Release)..." -ForegroundColor Cyan
& "$msbuild" "$loaderProj" /p:Configuration=Release /p:Platform="AnyCPU" /v:m
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to compile mepOS.Loader in Release mode."
    exit 1
}

# 2. Stage files in bin\Release
$releaseDir = Join-Path $scriptDir "bin\Release"
if (-not (Test-Path $releaseDir)) {
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
}

$resSource = Join-Path $scriptDir "src\mepOS.Loader\Resources"
$resDest = Join-Path $releaseDir "Resources"
if (Test-Path $resSource) {
    Copy-Item -Path $resSource -Destination $releaseDir -Recurse -Force
}

# 3. Update version.json
$manifestObj = @{
    version           = $Version
    releaseDate       = (Get-Date).ToString("yyyy-MM-dd")
    coreDownloadUrl   = "https://raw.githubusercontent.com/nguyen-hoang-hai/mepOS/main/bin/Release/mepOS.Core.dll"
    loaderDownloadUrl = "https://raw.githubusercontent.com/nguyen-hoang-hai/mepOS/main/bin/Release/mepOS.Loader.dll"
    changelog         = "mepOS v$($Version): $Changelog"
}

$jsonText = $manifestObj | ConvertTo-Json -Depth 4
Set-Content -Path (Join-Path $scriptDir "version.json") -Value $jsonText -Encoding UTF8
Set-Content -Path (Join-Path $releaseDir "version.json") -Value $jsonText -Encoding UTF8

Write-Host "`n[3/3] Release package ready in $releaseDir :" -ForegroundColor Green
Get-ChildItem -Path $releaseDir | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize

Write-Host "`n[OK] version.json updated to v$Version!" -ForegroundColor Green
Write-Host "[OK] When pushed to GitHub, all client machines will silently auto-update on next Revit launch." -ForegroundColor Yellow

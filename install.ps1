# ==============================================================================
# mepOS — Universal One-Click Client Installer
# Sets up mepOS in %APPDATA%\mepOS and registers addin manifest in all Revit versions
# Usage:
#   powershell -ExecutionPolicy Bypass -File install.ps1
#   Or via web:
#   irm https://raw.githubusercontent.com/nguyen-hoang-hai/mepOS/main/install.ps1 | iex
# ==============================================================================
param(
    [switch]$Local = $false
)

$ErrorActionPreference = "Stop"
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor [System.Net.SecurityProtocolType]::Tls12

$targetDir = Join-Path $env:APPDATA "mepOS"
$targetRes = Join-Path $targetDir "Resources"
if (-not (Test-Path $targetRes)) {
    New-Item -ItemType Directory -Path $targetRes -Force | Out-Null
}

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  mepOS // CÀI ĐẶT ADD-IN REVIT (1-CLICK CLIENT INSTALL)" -ForegroundColor Yellow
Write-Host "  Đích cài đặt: $targetDir" -ForegroundColor Gray
Write-Host "========================================================" -ForegroundColor Cyan

$scriptDir = $null
try {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
} catch {}

$isLocalAvailable = ($scriptDir -and (Test-Path (Join-Path $scriptDir "bin\Release\mepOS.Core.dll")))

if ($Local -or $isLocalAvailable) {
    Write-Host "`n[1/3] Cài đặt từ thư mục Release cục bộ..." -ForegroundColor Cyan
    $srcBin = Join-Path $scriptDir "bin\Release"
    Copy-Item -Path (Join-Path $srcBin "mepOS.Loader.dll") -Destination (Join-Path $targetDir "mepOS.Loader.dll") -Force
    Copy-Item -Path (Join-Path $srcBin "mepOS.Core.dll")   -Destination (Join-Path $targetDir "mepOS.Core.dll") -Force
    if (Test-Path (Join-Path $srcBin "version.json")) {
        Copy-Item -Path (Join-Path $srcBin "version.json") -Destination (Join-Path $targetDir "version.json") -Force
    }
    $resSrc = Join-Path $srcBin "Resources"
    if (Test-Path $resSrc) {
        Copy-Item -Path (Join-Path $resSrc "*") -Destination $targetRes -Force
    }
} else {
    Write-Host "`n[1/3] Tải phiên bản mới nhất từ GitHub..." -ForegroundColor Cyan
    $baseUrl = "https://raw.githubusercontent.com/nguyen-hoang-hai/mepOS/main"
    $wc = New-Object System.Net.WebClient
    $wc.Headers.Add("User-Agent", "mepOS-Installer")

    Write-Host "  • Tải version.json..." -ForegroundColor Gray
    $wc.DownloadFile("$baseUrl/version.json", (Join-Path $targetDir "version.json"))

    Write-Host "  • Tải mepOS.Loader.dll..." -ForegroundColor Gray
    $wc.DownloadFile("$baseUrl/bin/Release/mepOS.Loader.dll", (Join-Path $targetDir "mepOS.Loader.dll"))

    Write-Host "  • Tải mepOS.Core.dll..." -ForegroundColor Gray
    $wc.DownloadFile("$baseUrl/bin/Release/mepOS.Core.dll", (Join-Path $targetDir "mepOS.Core.dll"))

    # Download Icons
    $icons = @("CT_32.png", "CT_16.png", "BL_32.png", "BL_16.png")
    foreach ($ico in $icons) {
        try {
            $wc.DownloadFile("$baseUrl/bin/Release/Resources/$ico", (Join-Path $targetRes $ico))
        } catch {}
    }
}

# 2. Generate Client-Specific mepOS.addin
Write-Host "`n[2/3] Khởi tạo tệp manifest addin..." -ForegroundColor Cyan
$loaderDllPath = Join-Path $targetDir "mepOS.Loader.dll"

$addinXml = @"
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>mepOS</Name>
    <Assembly><![CDATA[$loaderDllPath]]></Assembly>
    <AddInId>B7E84920-5621-4A59-8671-5085BC361902</AddInId>
    <FullClassName>mepOS.Loader.RibbonApp</FullClassName>
    <VendorId>MEPOS</VendorId>
    <VendorDescription>mepOS - Swiss Clean Architectural Revit Suite</VendorDescription>
  </AddIn>
</RevitAddIns>
"@

$addinLocalPath = Join-Path $targetDir "mepOS.addin"
Set-Content -Path $addinLocalPath -Value $addinXml -Encoding UTF8

# 3. Deploy manifest to all Revit versions on client machine
Write-Host "`n[3/3] Đăng ký mepOS vào các phiên bản Revit có trên máy..." -ForegroundColor Cyan
$addinsBase = Join-Path $env:APPDATA "Autodesk\Revit\Addins"
$revitYears = @("2019", "2020", "2021", "2022", "2023", "2024", "2025", "2026")

$installedCount = 0
foreach ($yr in $revitYears) {
    $yrDir = Join-Path $addinsBase $yr
    if (-not (Test-Path $yrDir)) {
        # Create folder if Revit is installed or might be installed
        New-Item -ItemType Directory -Path $yrDir -Force | Out-Null
    }
    $targetFile = Join-Path $yrDir "mepOS.addin"
    Copy-Item -Path $addinLocalPath -Destination $targetFile -Force
    Write-Host "  [OK] Đã đăng ký -> Revit $yr" -ForegroundColor Green
    $installedCount++
}

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host "  [SUCCESS] CÀI ĐẶT HOÀN TẤT THÀNH CÔNG!" -ForegroundColor Green
Write-Host "  Từ bây giờ, mọi cập nhật mới của mepOS sẽ tự động đồng bộ" -ForegroundColor Yellow
Write-Host "  ngầm mà không cần cài đặt lại lần nào nữa!" -ForegroundColor Yellow
Write-Host "========================================================`n" -ForegroundColor Green

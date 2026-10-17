# ==============================================================================
# mepOS — Solution Build Script
# Compiles mepOS.Core and mepOS.Loader using native .NET Framework MSBuild
# ==============================================================================

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$slnPath   = Join-Path $scriptDir "mepOS.sln"

# 1. Locate MSBuild.exe
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
if (-not (Test-Path $msbuild)) {
    $msbuildCmd = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
    if ($msbuildCmd) { $msbuild = $msbuildCmd.Source }
}

if (-not (Test-Path $msbuild)) {
    Write-Error "MSBuild.exe was not found."
    exit 1
}

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Building mepOS Solution (Debug x64)..." -ForegroundColor Cyan
Write-Host "Compiler: $msbuild" -ForegroundColor Gray
Write-Host "==========================================" -ForegroundColor Cyan

# 2. Check if Revit process is locking mepOS.Loader.dll
$revitProcs = Get-Process -Name "*revit*" -ErrorAction SilentlyContinue

if ($revitProcs) {
    Write-Host "`n[LIVE RELOAD MODE] Revit is currently running." -ForegroundColor Yellow
    Write-Host "Compiling mepOS.Core (Dynamic In-Memory DLL)..." -ForegroundColor Cyan
    $coreProj = Join-Path $scriptDir "src\mepOS.Core\mepOS.Core.csproj"
    $cmd = "& `"$msbuild`" `"$coreProj`" /p:Configuration=Debug /p:Platform=`"AnyCPU`" /v:m"
    Invoke-Expression $cmd
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "`n[FAIL] Compilation of mepOS.Core encountered errors."
        exit 1
    }

    # Hot-swap compilation of mepOS.Loader
    $loaderDll = Join-Path $scriptDir "bin\Debug\mepOS.Loader.dll"
    $oldDll = Join-Path $scriptDir "bin\Debug\mepOS.Loader.dll.old"
    if (Test-Path $loaderDll) {
        if (Test-Path $oldDll) { Remove-Item $oldDll -Force -ErrorAction SilentlyContinue }
        Move-Item -Path $loaderDll -Destination $oldDll -Force -ErrorAction SilentlyContinue
    }

    Write-Host "Compiling mepOS.Loader..." -ForegroundColor Cyan
    $loaderProj = Join-Path $scriptDir "src\mepOS.Loader\mepOS.Loader.csproj"
    $cmd = "& `"$msbuild`" `"$loaderProj`" /p:Configuration=Debug /p:Platform=`"AnyCPU`" /v:m"
    Invoke-Expression $cmd

    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n[SUCCESS] mepOS.Core & mepOS.Loader compiled successfully!" -ForegroundColor Green
        Write-Host "Dynamic Core reloaded into RAM! Ribbon buttons and proxies updated." -ForegroundColor Yellow
        exit 0
    } else {
        Write-Error "`n[FAIL] Compilation of mepOS.Loader encountered errors."
        exit 1
    }
}

# 3. Full Solution Build (When Revit is closed)
$cmd = "& `"$msbuild`" `"$slnPath`" /p:Configuration=Debug /p:Platform=`"AnyCPU`" /v:m"
Invoke-Expression $cmd

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[SUCCESS] mepOS Solution compiled successfully!" -ForegroundColor Green
    
    $binDir = Join-Path $scriptDir "bin\Debug"
    Write-Host "Output files in $binDir :" -ForegroundColor Yellow
    Get-ChildItem -Path $binDir -Filter "*.dll" | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
    
    # 3. Auto-Deploy mepOS.addin to all Revit Addins directories
    $addinSource = Join-Path $binDir "mepOS.addin"
    if (-not (Test-Path $addinSource)) {
        $addinSource = Join-Path $scriptDir "src\mepOS.Loader\mepOS.addin"
    }

    $addinsBase = Join-Path $env:APPDATA "Autodesk\Revit\Addins"
    if (Test-Path $addinsBase) {
        Write-Host "`nDeploying mepOS.addin manifest to Revit Addins folders..." -ForegroundColor Cyan
        $targetYears = @("2022", "2024", "2023", "2021", "2020", "2019")
        foreach ($year in $targetYears) {
            $yearDir = Join-Path $addinsBase $year
            if (Test-Path $yearDir) {
                $targetFile = Join-Path $yearDir "mepOS.addin"
                Copy-Item -Path $addinSource -Destination $targetFile -Force
                Write-Host " [OK] Installed -> $targetFile" -ForegroundColor Green
            }
        }
    }
} else {
    Write-Error "`n[FAIL] Compilation encountered errors."
    exit 1
}

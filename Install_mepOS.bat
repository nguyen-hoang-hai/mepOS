@echo off
setlocal
chcp 65001 >nul
title mepOS Installer (Non-Admin)
color 0E

echo ==============================================================================
echo   mepOS // ONE-CLICK REVIT ADD-IN INSTALLER (NON-ADMIN)
echo ==============================================================================
echo.
echo [INFO] Dang kiem tra moi truong va cai dat mepOS...
echo.

if exist "%~dp0bin\Release\mepOS.Core.dll" (
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" -Local
) else (
    echo [INFO] Dang ket noi GitHub de tai ban cai dat moi nhat...
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor [System.Net.SecurityProtocolType]::Tls12; $s=(New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/nguyen-hoang-hai/mepOS/main/install.ps1'); Invoke-Expression $s"
)

echo.
echo ==============================================================================
echo [XONG] Hoan tat! Nhan phim bat ky de thoat...
echo ==============================================================================
pause >nul

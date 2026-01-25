# Auto Vibrance Uninstaller
# Run as Administrator

$AppName = "Auto Vibrance"
$InstallDir = "$env:ProgramFiles\$AppName"

# Check if running as admin
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
    Write-Host "Please run this script as Administrator!" -ForegroundColor Red
    pause
    exit 1
}

Write-Host "================================" -ForegroundColor Cyan
Write-Host "  $AppName Uninstaller" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Stop process
$process = Get-Process -Name "AutoVibrance" -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "Stopping Auto Vibrance..." -ForegroundColor Yellow
    Stop-Process -Name "AutoVibrance" -Force
    Start-Sleep -Seconds 2
}

# Remove install directory
if (Test-Path $InstallDir) {
    Write-Host "Removing files..." -ForegroundColor Yellow
    Remove-Item -Path $InstallDir -Recurse -Force
    Write-Host "Removed installation folder" -ForegroundColor Green
}

# Remove shortcuts
$DesktopPath = [Environment]::GetFolderPath("Desktop")
$StartMenuPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs"
$StartupPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup"

if (Test-Path "$DesktopPath\$AppName.lnk") {
    Remove-Item "$DesktopPath\$AppName.lnk" -Force
    Write-Host "Removed Desktop shortcut" -ForegroundColor Green
}

if (Test-Path "$StartMenuPath\$AppName.lnk") {
    Remove-Item "$StartMenuPath\$AppName.lnk" -Force
    Write-Host "Removed Start Menu shortcut" -ForegroundColor Green
}

if (Test-Path "$StartupPath\$AppName.lnk") {
    Remove-Item "$StartupPath\$AppName.lnk" -Force
    Write-Host "Removed Startup shortcut" -ForegroundColor Green
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "  Uninstall Complete!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Auto Vibrance Installer
# Run as Administrator: Right-click -> Run with PowerShell

$AppName = "Auto Vibrance"
$AppExe = "AutoVibrance.exe"
$InstallDir = "$env:ProgramFiles\$AppName"
$SourceDir = $PSScriptRoot
$PublishDir = Join-Path $SourceDir "publish"

# Check if running as admin
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
    Write-Host "Please run this script as Administrator!" -ForegroundColor Red
    Write-Host "Right-click the script and select 'Run with PowerShell' as Administrator"
    pause
    exit 1
}

Write-Host "================================" -ForegroundColor Cyan
Write-Host "  $AppName Installer" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Check if publish folder exists
if (-not (Test-Path $PublishDir)) {
    Write-Host "Error: publish folder not found!" -ForegroundColor Red
    Write-Host "Please build the project first with: dotnet publish -c Release -r win-x64 --self-contained true -o ./publish"
    pause
    exit 1
}

# Stop existing process
$process = Get-Process -Name "AutoVibrance" -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "Stopping existing Auto Vibrance process..." -ForegroundColor Yellow
    Stop-Process -Name "AutoVibrance" -Force
    Start-Sleep -Seconds 2
}

# Create install directory
Write-Host "Installing to: $InstallDir" -ForegroundColor Green
if (Test-Path $InstallDir) {
    Remove-Item -Path $InstallDir -Recurse -Force
}
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null

# Copy files
Write-Host "Copying files..." -ForegroundColor Green
Copy-Item -Path "$PublishDir\*" -Destination $InstallDir -Recurse -Force

# Create Desktop shortcut
$WshShell = New-Object -ComObject WScript.Shell
$DesktopPath = [Environment]::GetFolderPath("Desktop")
$Shortcut = $WshShell.CreateShortcut("$DesktopPath\$AppName.lnk")
$Shortcut.TargetPath = "$InstallDir\$AppExe"
$Shortcut.WorkingDirectory = $InstallDir
$Shortcut.Description = "Auto Vibrance - Arc Raiders display settings"
$Shortcut.Save()
Write-Host "Created Desktop shortcut" -ForegroundColor Green

# Create Start Menu shortcut
$StartMenuPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs"
$Shortcut = $WshShell.CreateShortcut("$StartMenuPath\$AppName.lnk")
$Shortcut.TargetPath = "$InstallDir\$AppExe"
$Shortcut.WorkingDirectory = $InstallDir
$Shortcut.Description = "Auto Vibrance - Arc Raiders display settings"
$Shortcut.Save()
Write-Host "Created Start Menu shortcut" -ForegroundColor Green

# Ask about startup
Write-Host ""
$addStartup = Read-Host "Start Auto Vibrance with Windows? (Y/N)"
if ($addStartup -eq "Y" -or $addStartup -eq "y") {
    $StartupPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup"
    $Shortcut = $WshShell.CreateShortcut("$StartupPath\$AppName.lnk")
    $Shortcut.TargetPath = "$InstallDir\$AppExe"
    $Shortcut.WorkingDirectory = $InstallDir
    $Shortcut.Save()
    Write-Host "Added to Windows startup" -ForegroundColor Green
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "  Installation Complete!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Installed to: $InstallDir"
Write-Host ""

# Ask to launch
$launch = Read-Host "Launch Auto Vibrance now? (Y/N)"
if ($launch -eq "Y" -or $launch -eq "y") {
    Start-Process "$InstallDir\$AppExe"
    Write-Host "Auto Vibrance started!" -ForegroundColor Green
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

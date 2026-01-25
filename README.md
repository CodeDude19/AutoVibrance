# Auto Vibrance

A lightweight Windows system tray application that automatically adjusts NVIDIA display settings (gamma and digital vibrance) when playing **Arc Raiders**.

Built specifically for Arc Raiders (`PioneerGame.exe`) to enhance visibility with boosted gamma and vibrance.

## Features

- **Automatic Detection** - Monitors for Arc Raiders (`PioneerGame.exe`) process every 5 seconds
- **Gamma Control** - Adjusts display gamma using Windows GDI
- **Digital Vibrance** - Controls NVIDIA digital vibrance via NVAPI
- **System Tray** - Runs silently in the background with a tray icon
- **Toggle Controls** - Checkboxes to individually enable/disable gamma and vibrance boosts while gaming
- **Auto-Restore** - Automatically restores default settings when game closes or app exits

## Settings

| State | Gamma | Digital Vibrance |
|-------|-------|------------------|
| Game Running | 1.70 | 60% |
| Idle | 1.00 | 50% |

## Requirements

- Windows 10/11
- NVIDIA GPU with latest drivers
- .NET 8.0 Runtime (or use the self-contained build)

## Installation

### Option 1: Self-Contained (Recommended)
Download the latest release from the `publish` folder - no .NET runtime required.

### Option 2: Build from Source
```bash
git clone https://github.com/YourUsername/AutoVibrance.git
cd AutoVibrance
dotnet build -c Release
```

## Usage

1. Run `AutoVibrance.exe`
2. The app appears in your system tray
3. Launch your game - settings adjust automatically
4. Right-click the tray icon to:
   - View current status
   - Toggle Gamma Boost on/off (when game is running)
   - Toggle Vibrance Boost on/off (when game is running)
   - Exit the application

## Configuration

To modify the target process (for other games) or display values, edit `TrayApplication.cs`:

```csharp
private const string TargetProcess = "PioneerGame";  // Arc Raiders process name
private const float GameGamma = 1.70f;               // Gamma when gaming
private const int GameVibrance = 60;                 // Vibrance % when gaming
private const float DefaultGamma = 1.00f;            // Default gamma
private const int DefaultVibrance = 50;              // Default vibrance %
```

## Building

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Self-contained single-directory publish
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

## Project Structure

```
Auto Vibrance/
├── AutoVibrance.csproj   # Project configuration
├── Program.cs            # Entry point, single-instance check
├── TrayApplication.cs    # System tray, timer, menu, state management
├── ProcessMonitor.cs     # Process detection
├── DisplaySettings.cs    # NVAPI + GDI display control
└── README.md
```

## Dependencies

- [NvAPIWrapper.Net](https://github.com/falahati/NvAPIWrapper) - C# wrapper for NVIDIA NVAPI

## License

MIT License

## Author

**Yasser Arafat**

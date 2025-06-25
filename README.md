# ClippedImgToWSLPath

A Windows system tray application that automatically saves clipboard images and converts their paths to WSL2 format

## Overview

This tool monitors the Windows clipboard for images and automatically:

1. **Auto-saves images**: Saves clipboard images to a specified folder in PNG format
2. **WSL2 path conversion**: Converts Windows paths (e.g., `C:\ClipboardImages\image.png`) to WSL2 paths (e.g., `/mnt/c/ClipboardImages/image.png`)
3. **Updates clipboard**: Automatically copies the converted WSL2 path to the clipboard

This makes it easy to reference Windows screenshots and images in WSL2 environments.

## Key Features

- Runs in the system tray as a background service
- **Windows+Shift+S screenshot support**: Full compatibility with Windows native screenshot tool
- **Timer-based monitoring**: Checks clipboard every second to reliably detect new images
- Duplicate image detection (using SHA256 hash)
- Customizable save location
- Desktop notifications on image save
- **Debug logging**: Optional logging feature for troubleshooting
- **Automatic icon generation**: Generates icon if not present

## Requirements

- Windows 10/11
- **.NET SDK 8.0** or later

## Installing .NET SDK 8.0

This application requires .NET 8.0. Download and install the .NET SDK 8.0 from:

https://dotnet.microsoft.com/download/dotnet/8.0

## Building

```bash
git clone https://github.com/yourusername/ClippedImgToWSLPath.git
cd ClippedImgToWSLPath
dotnet build
```

## Running

```bash
dotnet run
```

Or run the built executable directly:

```bash
.\bin\Debug\net8.0-windows\ClippedImgToWSLPath.exe
```

## Usage

1. Launch the application - it will minimize to the system tray
2. Copy an image to clipboard using:
   - **Windows+Shift+S**: Windows native screenshot tool
   - **PrintScreen**: Full screen screenshot
   - **Alt+PrintScreen**: Active window screenshot
   - **Snipping Tool**: Windows snipping tool
   - Any other image copy operation
3. The image is automatically saved and the WSL2 path is copied to clipboard
4. Paste the path in WSL2 using `Ctrl+V`

## Configuration

- **Default save location**: `ClipboardImages` folder in the application directory
- Double-click the tray icon or right-click → "Settings" to change the save location
- **Debug logging**: Right-click tray icon → "Enable Logging" to enable debug output
  - Log file: `clipboard_log.txt` (in application directory)

## File Naming Format

Saved images use the following naming convention:
```
clipboard_yyyyMMdd_HHmmss.png
```
Example: `clipboard_20240125_143052.png`

## Troubleshooting

- **Application won't start**: Verify .NET SDK 8.0 is installed
- **Images not saving**:
  - Check write permissions for the save folder
  - Enable debug logging to identify issues
- **Windows+Shift+S not working**:
  - Ensure the application is running
  - Check debug logs to verify clipboard monitoring is active
- **System tray icon not visible**: Check Windows notification area settings

## Technical Details

- **Clipboard monitoring**: Uses both Windows API and timer-based monitoring
- **Image format**: Saves as PNG
- **Duplicate detection**: SHA256 hash-based image comparison
- **Icon generation**: Dynamic icon creation using System.Drawing API
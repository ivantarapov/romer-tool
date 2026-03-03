# Romer Overlay (Visual-Only v1)

A Windows WPF overlay tool for practicing map navigation with USGS PDFs in any external PDF reader.

## What v1 does

- Always-on-top transparent overlay window.
- Romer-like graphic templates:
  - `UTM/MGRS 1:24k`
  - `UTM/MGRS 1:50k`
- Drag, uniform resize, and rotation controls.
- Global hotkeys:
  - `Ctrl+Alt+R`: show/hide overlay
  - `Ctrl+Alt+T`: toggle click-through
  - `Ctrl+Alt+L`: lock/unlock manipulation
  - `Ctrl+Alt+S`: switch template
- System tray menu for quick actions and hotkey remapping.
- No coordinate calculations, no PDF parsing, no persistence.

## Solution layout

- `src/Romer.App`: WPF app bootstrap, tray integration, runtime orchestration
- `src/Romer.Core`: shared models/state/controllers/interfaces
- `src/Romer.UI`: overlay UI, controls, hotkeys, monitor service, logging
- `tests/Romer.Core.Tests`: unit tests for core logic and template availability

## Build and run (Windows)

```powershell
cd C:\src\romer
 dotnet restore
 dotnet build Romer.sln -c Debug
 dotnet run --project .\src\Romer.App\Romer.App.csproj
```

## Notes

- Requires Windows 10/11 and .NET 8 SDK.
- Logs are written to `%TEMP%\\romer\\romer.log`.
- If a hotkey cannot be registered due to conflict, use tray menu `Hotkey Settings` to remap for the current session.

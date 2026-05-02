# Za-World

<p align="center">
  <a href="https://dotnet.microsoft.com/languages/csharp"><img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#" /></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/8.0"><img src="https://img.shields.io/badge/.NET%208-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8" /></a>
  <a href="https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/"><img src="https://img.shields.io/badge/WPF-5C2D91?style=for-the-badge&logo=windows&logoColor=white" alt="WPF" /></a>
  <a href="https://github.com/shimat/opencvsharp"><img src="https://img.shields.io/badge/OpenCvSharp-5C3EE8?style=for-the-badge&logo=opencv&logoColor=white" alt="OpenCvSharp" /></a>
  <a href="https://sixlabors.com/products/imagesharp/"><img src="https://img.shields.io/badge/ImageSharp-673AB7?style=for-the-badge" alt="ImageSharp" /></a>
  <a href="https://xunit.net/"><img src="https://img.shields.io/badge/xUnit-0D5CAB?style=for-the-badge&logo=xunit&logoColor=white" alt="xUnit" /></a>
</p>

<p align="center">
  <img src="src/assets/theworld-logo.ico" alt="Za-World logo" width="120" height="120" />
</p>

Windows 11 desktop utility that **locks keyboard and mouse input** (with movement/hover still allowed, clicks blocked), **captures webcam snapshots** when someone moves the mouse or presses keys while locked, and unlocks with a **configurable hotkey**.

## Repository layout

```text
Za-World.sln                 # Visual Studio / dotnet solution
README.md
plan.md                      # Design notes (optional read)

src/
  assets/
    theworld-logo.ico        # App + Explorer icon; copied next to exe on build
  ZaWorld.App/               # WPF UI, hooks, camera, settings
    App.xaml(.cs)
    MainWindow.xaml(.cs)
    Media/                   # Optional WebP branding helper
    Services/                # InputLockService, WebcamSnapshotService, AppSettingsStore
  ZaWorld.Core/              # Config, hotkey parsing, path resolution
    Configuration/
    Hotkeys/

tests/
  ZaWorld.Core.Tests/        # xUnit tests for core logic
```

## Prerequisites

- **Windows 10/11** (x64), **.NET 8 SDK**  
  Install: [Download .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) (SDK includes the CLI used below).
- **Webcam** allowed in **Settings → Privacy & security → Camera** (for intrusion snapshots).
- Building the WPF app requires the **Windows Desktop** workload (included with the .NET SDK for Windows).

## Git hooks (optional, recommended for contributors)

This repo includes a hook that **removes** `Co-authored-by: Cursor …` lines from commit messages so they are not pushed to GitHub.

After cloning, point Git at the shared hooks folder once:

```powershell
git config core.hooksPath .githooks
```

## Quick start (developer)

Clone the repo, then from the repository root:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/ZaWorld.App/ZaWorld.App.csproj
```

Settings are stored under `%AppData%\Za-World\config.json`. Default capture folder is under **Pictures** → `TheWorldLock\Captures` (or as configured in the UI).

## Publish a self-contained executable

From the repo root (output folder is gitignored; create it locally):

```powershell
dotnet publish src/ZaWorld.App/ZaWorld.App.csproj `
  -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true `
  -o ./publish-out
```

Run **`publish-out\Za-World.exe`**. Keep **all files** in that folder together (native OpenCV DLLs and `theworld-logo.ico` are required alongside the exe for this project).

If publish fails because the exe is in use, close Za-World or use a different `-o` path.

## Dependencies (NuGet)

Declared in `src/ZaWorld.App/ZaWorld.App.csproj`:

- **OpenCvSharp4** + **OpenCvSharp4.runtime.win** — webcam capture
- **SixLabors.ImageSharp** — optional WebP decoding if you add `theworld-logo.webp` next to the exe again

Core library has **no** NuGet packages beyond the .NET runtime.

## Tests

```powershell
dotnet test
```

## What not to commit

Build locally with `dotnet build` / `dotnet publish` and keep outputs on your machine or release them via **Releases**, not inside the git tree.

## Security & privacy

This tool is intended for **your own PC** and **consenting use**. It blocks input and may store **photos** locally. Review **Windows privacy**, **camera**, and **data-retention** policies before deploying. **Ctrl+Alt+Del** may still reach the secure screen for recovery if you misconfigure the unlock hotkey.

## Created by:
  **Mohamed Ali (Frax)**


if you have any suggestions for improvements or features contact `https://github.com/Muhammad-FRAX`

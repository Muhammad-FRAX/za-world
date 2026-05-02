# Windows Protective Lock App - Implementation Plan

## 1) Product Goal
Build a Windows 11 desktop app (`.exe`) that:
- Temporarily blocks keyboard and mouse input while the PC remains powered on and active.
- Unlocks only through a user-defined global key combination.
- Captures a webcam photo when mouse movement or mouse button input or keyboard click is detected during lock mode.

This is a protective screen-lock utility (similar intent to KeyFreeze), not a full OS login replacement.

## 2) Recommended Tech Stack (Best Fit)
- **Language/Runtime:** C# with `.NET 8`
- **UI Framework:** `WPF`
- **Packaging:** Publish self-contained Windows x64 `.exe` (unsigned for now, as requested)
- **Config:** JSON file stored in `%AppData%\<AppName>\config.json`
- **Logging:** Rolling local logs in `%LocalAppData%\<AppName>\logs\`
- **Camera Access:** `Windows.Media.Capture` interop (or OpenCvSharp fallback if webcam API issues appear)

Why this stack is best:
- WPF + .NET has strong compatibility on Windows 11.
- Easy integration with global keyboard/mouse hooks.
- Easier long-term maintenance than lower-level native Win32 for this feature set.

## 3) Core Functional Requirements (From Your Choices)
- Unlock method: **global customizable hotkey only**
- Camera trigger: **any mouse movement/click/button event while locked**
- Runtime mode: **manual launch**
- Trust model: **developer/local use (unsigned executable)**
- Default snapshot folder: **`%Pictures%\TheWorldLock\Captures`**

## 4) High-Level Architecture

### A. `UI Layer` (WPF)
Responsibilities:
- Settings window (set unlock hotkey, camera options, save path).
- Lock/unlock controls.
- Status indicators (locked, camera ready, last snapshot timestamp).

### B. `InputGuard Service`
Responsibilities:
- Install global low-level keyboard and mouse hooks.
- Swallow/block all input while locked.
- Allow only the configured unlock chord to pass through internal detector.
- Fail-safe auto-unlock when service exits unexpectedly.

### C. `UnlockCombination Engine`
Responsibilities:
- Validate and normalize key combinations.
- Detect exact chord press timing/state from keyboard events.
- Debounce repeated unlock attempts.

### D. `IntrusionCapture Service`
Responsibilities:
- React to any mouse event while locked.
- Capture webcam image and store with timestamp.
- Save snapshots to the configured folder, defaulting to `%Pictures%\TheWorldLock\Captures`.
- Optionally rate-limit snapshots (recommended to avoid disk flood).

### E. `Config & Persistence`
Responsibilities:
- Read/write hotkey, camera settings, storage path.
- Validate config on startup and fallback to safe defaults.
- If custom path is invalid/unwritable, fallback to `%Pictures%\TheWorldLock\Captures`.

### F. `Audit Logger`
Responsibilities:
- Log lock start/end times, unlock attempts, capture events, errors.
- Keep local-only forensic timeline.

## 5) Windows 11 Compatibility and "Allowed to Work Fine"

To minimize Windows blocking and maximize reliability:
- Build target: `win-x64` self-contained `.exe`.
- Include app manifest with requested execution level (`asInvoker` first; elevate only if required by hooks).
- Avoid anti-cheat-like behavior (no stealth actions, no hidden persistence).
- Ask and verify explicit camera permission on first use.
- Use visible tray icon + clear user control to stop/uninstall.

Important note:
- Unsigned executables can still run, but SmartScreen warnings are common.  
- For production/no-warning experience, add code signing certificate and installer in a later phase.

## 6) Safety and Abuse Prevention (Must-Have)
- Add **emergency failsafe unlock** (time-based or hidden fallback key) in case hook logic fails.
- Auto-disable lock if webcam initialization crashes repeatedly.
- Store only local snapshots unless user explicitly enables export.
- Show clear privacy notice in app UI: mouse activity during lock triggers camera capture.

## 7) Proposed Project Structure

```text
TheWorldLock/
  src/
    TheWorldLock.App/                 # WPF app entry/UI
      App.xaml
      MainWindow.xaml
      Views/
      ViewModels/
    TheWorldLock.Core/                # Business rules
      LockState/
      Hotkeys/
      Configuration/
      Logging/
    TheWorldLock.Input/               # Keyboard/mouse hook implementation
      KeyboardHookService.cs
      MouseHookService.cs
      InputBlocker.cs
    TheWorldLock.Camera/              # Webcam capture service
      CameraService.cs
      SnapshotWriter.cs
    TheWorldLock.Platform.Windows/    # Win11-specific interop/helpers
      WinApi/
      Manifest/
  tests/
    TheWorldLock.Core.Tests/
    TheWorldLock.Input.Tests/
  docs/
    threat-model.md
    user-guide.md
  packaging/
    publish.ps1
```

## 8) Implementation Phases

### Phase 1 - Foundation
- Create WPF app shell and settings model.
- Implement config persistence and logging.
- Build lock state machine (Unlocked -> Locking -> Locked -> Unlocking).

### Phase 2 - Input Locking
- Implement low-level keyboard/mouse hooks.
- Block all mouse/keyboard actions while locked.
- Build customizable unlock-chord detector.

### Phase 3 - Camera Trigger
- Initialize webcam pipeline.
- On any mouse event during lock, capture and save image.
- Add simple rate limiting to avoid excessive snapshots.

### Phase 4 - Hardening
- Add crash recovery and emergency unlock fallback.
- Improve error handling and telemetry logs.
- Test on Windows 11 with common USB/Bluetooth keyboards/mice.

### Phase 5 - Packaging
- Publish self-contained x64 `.exe`.
- Add startup checks (camera availability, permissions, config validity).
- Write usage instructions.

## 9) Testing Strategy
- Unit tests:
  - hotkey parsing/validation
  - lock state transitions
  - camera trigger event pipeline
- Integration tests:
  - lock mode blocks keyboard/mouse
  - configured unlock combo consistently unlocks
  - mouse event causes image capture while locked
- Manual Windows tests:
  - built-in camera and external USB camera
  - multiple keyboard layouts
  - wake/sleep behavior while app is open

## 10) Build and Publish Baseline Commands

```powershell
dotnet new sln -n TheWorldLock
dotnet new wpf -n TheWorldLock.App -f net8.0-windows
dotnet publish .\src\TheWorldLock.App\TheWorldLock.App.csproj `
  -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true
```

## 11) Risk Register and Mitigation
- **Hook instability:** add watchdog + auto-unlock on fatal errors.
- **Camera unavailable/blocked:** degrade gracefully, keep lock functional, notify user.
- **False positive captures:** add optional cooldown (for now keep immediate capture as requested).
- **SmartScreen warning (unsigned):** expected in dev mode; resolve later with signing.

## 12) Future Enhancements (Optional)
- Unlock by hotkey + PIN (stronger against accidental unlock).
- Silent alarm sound on intrusion.
- Encrypted snapshot storage.
- Installer + signed binaries for trusted distribution.

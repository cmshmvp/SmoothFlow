<img src="assets/SmoothFlow-icon.png" alt="SmoothFlow mouse wheel icon" width="72" height="72">

# SmoothFlow

English · [Simplified Chinese](README.zh-CN.md)

SmoothFlow is an open-source smooth scrolling utility for physical mouse wheels on Windows 10 and 11 (x64). It turns each wheel notch into a series of smaller scroll events so a page eases into motion and slows to a stop. You can tune the distance, duration, and acceleration for each application.

**[Download SmoothFlow 0.5.0 for Windows x64](https://github.com/cmshmvp/SmoothFlow/releases/download/v0.5.0/SmoothFlow-0.5.0-win-x64.zip)** · [Release notes and checksums](https://github.com/cmshmvp/SmoothFlow/releases)

## Interface language

Use **Language** in the left sidebar to choose a language by its native name. Changes apply immediately and are saved for the next launch. **Use system language** is the default. The settings window, tray menu, help, and notifications are available in English, Simplified Chinese, Traditional Chinese, Japanese, Korean, Spanish, French, German, Portuguese, Italian, and Russian. Other display languages use English. After changing the Windows display language, quit SmoothFlow from the tray and reopen it.

## Getting started

1. Extract the entire ZIP to a writable folder where you want to keep the app. Run `SmoothFlow.exe`.
2. With **Enabled** checked, move the pointer over a scrollable page and turn the wheel one notch. **Reverse wheel direction** is on by default. Clear it if you prefer your usual wheel direction.
3. Close the settings window to send SmoothFlow to the system tray. Double-click its tray icon to reopen settings. Press **Ctrl+Alt+S** to pause or resume.

**Start on login** is on by default. Clear it to disable startup. SmoothFlow saves settings in `settings.xml` next to the executable. To uninstall, disable login startup, choose **Quit** from the tray menu, then delete the extracted folder.

## Tuning the wheel

| Setting | Default | What it controls |
| --- | --- | --- |
| Scroll distance | 1.00× | Total travel per wheel notch |
| Animation time | 380 ms | Duration of the scrolling motion. Increase it for a longer glide. |
| Maximum acceleration | 1.3× | Maximum boost when you turn the wheel repeatedly |
| Animation easing | On | Gradual acceleration at the start and deceleration near the end |
| Fine steps (tray menu) | On | The timing of small wheel increments. This reduces the pause before the final movement. |

Use **Add** beside the application profile to add a profile by process name. For example, to give WeChat chat history a longer glide, add `Weixin.exe` and increase **Animation time** for that profile. The final scrolling behavior depends on how the receiving application handles small wheel increments.

## Compatibility

SmoothFlow is written in C# using Windows Forms and .NET Framework 4.x. Small high-resolution wheel increments, scrolling over elevated windows, and wheel input with Ctrl, Shift, Alt, or the Windows key pass through to the application. SmoothFlow pauses if it detects SmoothScroll running, so the two utilities do not handle the same wheel input.

## Building from source

Run this command from the repository directory on 64-bit Windows.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

The script uses the system .NET Framework C# compiler and builds `SmoothFlow.exe`. It runs regression tests for individual notches, rapid scrolling, direction changes, switching windows, and horizontal and vertical scrolling. The tests also compare 2,080 pairs of frame outputs with the classic scrolling behavior. Results are written to `self-test-results.txt`.

To generate the portable ZIP and its SHA-256 checksum in `dist/`, run the packaging script.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\package.ps1
```

SmoothFlow is released under the [MIT License](LICENSE).

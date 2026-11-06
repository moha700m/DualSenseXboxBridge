# Project status — 2026-11-06

## User configuration
- Connection: USB
- Compatibility target: Xbox App, PC Game Pass, Microsoft Store and Win32 XInput titles

## Implemented in source alpha
- WPF application shell.
- DualSense USB HID discovery (Sony VID 054C; PIDs 0CE6 and 0DF2).
- USB report ID 01 parser for 64-byte reports.
- Mapping for face buttons, D-pad, bumpers, analog triggers, sticks, Start/Back and Guide.
- HID reader loop.
- Optional legacy ViGEm Xbox 360 output adapter.
- Explicit test-only warning; no HidHide automation or automatic driver installation.
- Windows GitHub Actions build/publish workflow.

## Not yet verified
- No Windows build was run in the authoring environment (dotnet SDK unavailable).
- No physical DualSense was connected to this environment.
- No XInput output test or Xbox App/Game Pass game test has been run.
- No HidHide integration; physical and virtual pads may both be visible.
- Bluetooth report parsing is not implemented.

## Blocking release work
1. Run CI on Windows and fix compiler/package errors.
2. Validate USB report bytes on the user's DualSense hardware.
3. Replace retired ViGEm output backend with a maintained, signed virtual-pad backend.
4. Implement per-device HidHide config with backup and guaranteed restore.
5. Test `joy.cpl`, XInput tester, then actual Xbox App and Game Pass games.

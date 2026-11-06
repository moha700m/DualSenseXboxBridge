# Implementation plan

## Phase 1 — verified discovery
- Validate Sony VID/PID on the target PC.
- Capture USB report descriptors and input reports.
- Separately capture Bluetooth report format.
- Add parser unit tests from anonymized report fixtures.

## Phase 2 — mapping
- Parse buttons, hat switch, sticks and analog triggers.
- Apply configurable deadzones and Y-axis convention.
- Submit a complete Xbox 360 report at a stable polling cadence.
- Validate with Windows `joy.cpl` and an XInput tester.

## Phase 3 — HidHide
- Integrate the official HidHide configuration client/API only after output validation.
- Add only the selected DualSense device instance.
- Allow-list this app.
- Snapshot existing configuration and restore it on Stop/uninstall.
- Never hide all HID devices.

## Phase 4 — modern backend
- Evaluate a maintained backend (including VIIPER/usbip-win2 integration) and its exact release packaging.
- Keep ViGEm as an optional legacy adapter only.
- Do not redistribute drivers without verifying licensing, signatures, and release provenance.

## Acceptance criteria
- One virtual Xbox controller visible to XInput.
- No duplicate physical DualSense visible to the target game after opt-in HidHide setup.
- Every button/axis/trigger maps correctly over USB and Bluetooth.
- Disconnect/reconnect cannot leave stuck buttons.
- Stop/crash recovery restores physical device visibility.

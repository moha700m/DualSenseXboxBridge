# DualSense Xbox Bridge

Windows WPF alpha for reading a Sony DualSense controller over USB and mapping input to a virtual Xbox 360/XInput controller.

> **Alpha / test-only:** This source is not a verified game-ready release. The current virtual output adapter uses the retired ViGEm stack, HidHide is not integrated, and double input may occur. Do not use in online games or enable virtual output for gameplay until the driver backend, device hiding, and hardware tests are complete.

## Build

GitHub Actions builds a self-contained Windows x64 artifact on pushes to `main`.

See [PROJECT_STATUS.md](PROJECT_STATUS.md) for limitations and [docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md) for next steps.

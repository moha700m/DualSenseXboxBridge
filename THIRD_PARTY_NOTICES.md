# Third-party notices

DualSense Xbox Bridge ships two unmodified third-party binaries inside the Windows installer.
Neither binary is built from this repository, and neither is covered by this project's code.

## VIIPER v0.9.0

- File: `thirdparty/viiper/viiper.exe`
- Copyright: the VIIPER authors
- License: GNU General Public License v3.0 (see `thirdparty/licenses/LICENSE-VIIPER.txt`)
- Source: https://github.com/Alia5/VIIPER
- Usage: separate process; this application talks to it over TCP on 127.0.0.1:3242.
- The binary is redistributed unmodified. The complete corresponding source is available at
  the URL above for the same version tag.

## usbip-win2 v0.9.8.1

- File: `thirdparty/usbip-win2/USBip-0.9.8.1-x64.exe`
- Copyright (c) 2021-2026, Vadym Hrynchyshyn <vadimgrn@gmail.com>
- License: BSD 2-Clause (see `thirdparty/licenses/LICENSE-usbip-win2.txt`)
- Source: https://github.com/vadimgrn/usbip-win2
- Usage: the official signed driver installer, copied unmodified and executed only after the
  user explicitly asks for it. This project does not alter, repackage or re-sign it.

## NuGet packages

- HidSharp 2.6.4 (Apache-2.0 / MIT-style dual license as published by the package author)
- Nefarius.ViGEm.Client 1.21.256 (MIT)

## No warranty

The third-party components are provided "as is", without warranty of any kind, as stated in
their respective licenses. Driver installation is performed by the upstream installer and
requires administrator approval.

# DualSense USB report mapping (alpha)

Source basis: public DualSense USB HID report documentation for Sony VID `054C`, PID `0CE6`, input report ID `01`, 64-byte report.

| Report byte | Meaning |
|---:|---|
| 0 | Report ID `0x01` |
| 1–4 | Left X/Y, Right X/Y (0–255, center about 128) |
| 5–6 | L2/R2 analog values |
| 8 low nibble | D-pad hat (0–7, 8 neutral) |
| 8 high nibble | Square, Cross, Circle, Triangle |
| 9 | L1, R1, digital triggers, Create, Options, L3, R3 |
| 10 bit 0 | PS/Guide button |

Bluetooth is deliberately rejected by this parser; its packet framing and CRC differ.

## Verification required on real hardware
- Confirm HIDSharp returns the report ID at offset 0 and exactly the expected 64-byte USB payload on the target controller firmware.
- Verify all button bits and axes with live captures.
- Check stick center/calibration and trigger ranges.
- Confirm XInput output in `joy.cpl` and a trusted XInput tester.
- Only then implement HidHide configuration and gameplay mode.

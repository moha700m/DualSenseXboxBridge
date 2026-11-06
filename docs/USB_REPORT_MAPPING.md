# تنسيق تقرير USB وخريطة التحويل

## التقرير

- النوع: HID Input Report
- Report ID: `0x01` (USB فقط)
- الطول: 64 بايت
- Bluetooth يستخدم ID `0x31` بتنسيق مختلف — **مرفوض صراحة**.

### الإزاحات المستخدمة

| الإزاحة | المعنى |
| --- | --- |
| 0 | Report ID = 0x01 |
| 1 | Left Stick X (0..255، 128 مركز) |
| 2 | Left Stick Y (0 = أعلى) |
| 3 | Right Stick X |
| 4 | Right Stick Y |
| 5 | L2 (0..255) |
| 6 | R2 (0..255) |
| 7 | Sequence |
| 8 | D-Pad (bits 0-3) + Square(4) + Cross(5) + Circle(6) + Triangle(7) |
| 9 | L1(0) R1(1) L2(2) R2(3) Create(4) Options(5) L3(6) R3(7) |
| 10 | PS(0) Touchpad(1) Mute(2) |
| 53 | مستوى البطارية (0-10) + إشارة الشحن |

### D-Pad

`0=Up, 1=UpRight, 2=Right, 3=DownRight, 4=Down, 5=DownLeft, 6=Left, 7=UpLeft, 8=Neutral`

## خريطة الأزرار

| DualSense | Xbox 360 |
| --- | --- |
| Cross | A |
| Circle | B |
| Square | X |
| Triangle | Y |
| L1 / R1 | LB / RB |
| L2 / R2 | LT / RT (تناظري) |
| L3 / R3 | Left/Right Thumb |
| D-Pad | D-Pad |
| Options | Start |
| Create | Back |
| PS | Guide |
| Touchpad / Mute | غير mapped افتراضيًا (قابل للتغيير) |

## تحويل المحاور

- X: `(raw - center) / span × 32767`.
- Y: نفس التحويل ثم **عكس** لأن DualSense يستخدم 0 = أعلى بينما XInput يستخدم +32767 = أعلى.
- المنطقة الميتة دائرية radial، ثم يُعاد قياس المدى إلى كامل 0..32767.
- الزنادات: 0..255 مع منطقة ميتة تُصفّر القيم الصغيرة وتعيد قياس الباقي إلى 255.

## بروتوكول VIIPER

حزمة الحالة 20 بايت little-endian:

| الإزاحة | الحجم | المعنى |
| --- | --- | --- |
| 0 | 4 | Buttons (uint32) |
| 4 | 1 | LT |
| 5 | 1 | RT |
| 6 | 2 | LX (int16) |
| 8 | 2 | LY |
| 10 | 2 | RX |
| 12 | 2 | RY |
| 14 | 6 | Reserved |

الأعلام مطابقة لـ XInput (`A=0x1000`، `Start=0x0010`، `Guide=0x0400` …).

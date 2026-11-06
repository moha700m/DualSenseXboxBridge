# إعدادات متقدمة (ملف settings.json)

المسار:

```
%LOCALAPPDATA%\DualSenseXboxBridge\settings.json
```

يمكن تعديله والتطبيق مغلق، ثم تشغيل التطبيق. القيم غير الصالحة تُصحَّح تلقائيًا عند التحميل.

## إعادة تعيين الأزرار

```json
{
  "Mapping": {
    "ButtonOverrides": {
      "Cross": "B",
      "Circle": "A",
      "Ps": "None",
      "Touchpad": "Back"
    }
  }
}
```

- المفتاح: اسم زر DualSense (`Cross`, `Circle`, `Square`, `Triangle`, `L1`, `R1`, `L2`, `R2`, `Create`, `Options`, `L3`, `R3`, `Ps`, `Touchpad`, `Mute`).
- القيمة: اسم زر Xbox (`A`, `B`, `X`, `Y`, `LeftShoulder`, `RightShoulder`, `Back`, `Start`, `LeftThumb`, `RightThumb`, `Guide`, `DpadUp`, `DpadDown`, `DpadLeft`, `DpadRight`, `None`).
- `None` يعطّل الزر.
- أي قيمة غير معروفة تُتجاهَل ويُستخدم التعيين الافتراضي.

## جهاز USB المفضل

عند وجود أكثر من يد متصلة، يمكن تثبيت الجهاز المطلوب:

```json
{
  "PreferredDevicePath": "\\\\?\\hid#vid_054c&pid_0ce6&mi_03#7&1a2b3c&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}"
}
```

انسخ القيمة من حقل «معرّف الجهاز» في الشاشة الرئيسية.

## اختيار الـ Backend

```json
{ "Backend": "Auto" }
```

القيم: `Auto`, `Viiper`, `ViGEm`, `Null`.

## VIIPER

```json
{
  "ViiperHost": "127.0.0.1",
  "ViiperPort": 3242,
  "ViiperServerPath": "C:\\Tools\\VIIPER\\viiper.exe",
  "ViiperAutoLaunch": true
}
```

## ملاحظة

لا تُخزَّن أي بيانات حساسة في هذا الملف. لا يوجد اعتماد على السجل (Registry) إلا لخيار بدء التشغيل مع Windows
(مفتاح HKCU الخاص بالمستخدم فقط).

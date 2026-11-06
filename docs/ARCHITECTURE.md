# البنية

## المشاريع

| المشروع | الهدف | منصة |
| --- | --- | --- |
| `DualSenseXboxBridge.Core` | parser، mapping، فلترة HID، بروتوكول VIIPER، الإعدادات، HidHide helpers | `net8.0` (قابل للاختبار على Linux) |
| `DualSenseXboxBridge` | WPF، HID، محرك الربط، Backends، HidHide، الواجهة | `net8.0-windows` |
| `DualSenseXboxBridge.Core.Tests` | xUnit | `net8.0` |

الفصل يضمن أن منطق التحويل قابل للاختبار دون Windows أو عتاد.

## مسار البيانات

```
DualSense (USB HID, report 0x01, 64 bytes)
        │  HidSharp Read (خيط خلفي، مهلة 250ms)
        ▼
DualSenseReportParser ──► DualSenseInputState (أزرار، D-Pad، محاور، زنادات)
        ▼
DualSenseToXboxMapper ──► XboxPadState (XInput flags, -32768..32767, 0..255)
        ▼
IVirtualPadBackend
   ├── ViiperBackend  → TCP 127.0.0.1:3242 → 20-byte packet → USB/IP → XInput
   ├── ViGEmBackend   → ViGEmBus driver (legacy)
   └── NullPadBackend → وضع الاختبار فقط
```

## دورة الحياة

- `BridgeService.StartAsync` يفتح الجهاز ويشغّل `Pump` على `Task.Run` (خيط طويل العمر).
- القراءة متزامنة مع `ReadTimeout = 250ms`، لذا يمكن فحص `CancellationToken` بانتظام دون تعليق الواجهة.
- عند `TimeoutException` تُعاد المحاولة، ويُفحص وجود الجهاز كل 1.5 ثانية.
- عند أي خطأ: إرسال حالة محايدة، تحرير الـ stream، ثم إعادة الاكتشاف كل ثانية.
- `StopAsync` يلغي، ينتظر الخيط حتى 3 ثوانٍ، يرسل حالة محايدة، ثم يفصل الجهاز الافتراضي ويتخلص منه.
- إغلاق النافذة يمر عبر `ShutdownAsync`: إيقاف الربط ثم استعادة HidHide إن كان مفعّلًا.

## اختيار الـ Backend

`BackendFactory`:

1. تفضيل المستخدم إن حُدِّد.
2. Auto: VIIPER إن وُجد خادم/منفذ مفتوح، ثم ViGEm إن كان المشغّل مثبتًا، وإلا VIIPER (لإظهار رسالة إعداد واضحة).

## HidHide

- الموقع: `HidHideCLI.exe` في Program Files أو من مفتاح التسجيل.
- القراءة والكتابة عبر CLI فقط (لا تعديل مباشر على التسجيل أو تعريفات النظام).
- العمليات المرتفعة تُنفَّذ عبر سكربت مؤقت cmd لتجميع الخرج ورمز الخروج بأمان.
- لا تُكتب نسخة احتياطية إلا بعد قراءة ناجحة للحالة الحالية.
- الاستعادة تحسب الفروق (إضافة/إزالة) لكل من الأجهزة والتطبيقات، ولا تلمس أجهزة أخرى.

## الخيوط والواجهة

- الواجهة لا تلمس HID ولا الشبكة.
- `DispatcherTimer` بمعدل 30Hz يقرأ `BridgeService.CurrentState` لتحديث شاشة الاختبار.
- أحداث الحالة من الخلفية تُمرَّر إلى خيط الواجهة عبر `Dispatcher.BeginInvoke`.

# DualSense Xbox Bridge

![Build](https://github.com/moha700m/DualSenseXboxBridge/actions/workflows/build.yml/badge.svg)

تحويل يد **Sony DualSense** الموصولة عبر **USB** إلى **جهاز Xbox 360 افتراضي** متوافق مع **XInput**،
حتى تتعرف عليها ألعاب Windows وتطبيقات Xbox App و PC Game Pass التي لا تدعم DualSense مباشرة.

> **الحالة بصراحة:** كل ما في هذا المستودع مبني ومُختبر آليًا (بناء + 92 اختبار وحدة + حزمة تثبيت)،
> لكن **لم يُختبر على يد DualSense حقيقية** في بيئة إعداد هذا الإصدار لعدم توفر الجهاز.
> راجع [PROJECT_STATUS.md](PROJECT_STATUS.md) و[docs/TESTING.md](docs/TESTING.md) قبل الاعتماد عليه في اللعب اليومي.


## التنزيل

أحدث إصدار: https://github.com/moha700m/DualSenseXboxBridge/releases/latest

- `DualSenseXboxBridge-Setup-x64.exe` — مثبّت per-user (موصى به).
- `DualSenseXboxBridge-...-portable.zip` — نسخة محمولة.

---

## ما يفعله التطبيق

- يكتشف واجهة HID الصحيحة ليد DualSense (VID `0x054C`، PID `0x0CE6` أو `0x0DF2`) عبر USB.
- يقرأ تقرير USB القياسي (64 بايت، Report ID `0x01`) ويفكّك كل الأزرار والعصي والزنادات و D-Pad.
- يحوّل الإدخال إلى حالة Xbox 360 مع منطقة ميتة، حساسية، عكس محاور، وتبديل عصي.
- يرسل الحالة إلى جهاز افتراضي عبر أحد الـ Backends:
  - **VIIPER** (المسار الحديث والموصى به) — USB/IP مع مشغّل `usbip-win2` الموقّع.
  - **ViGEmBus** (بديل قديم، يُستخدم فقط إن كان مثبتًا مسبقًا).
  - **وضع الاختبار** — قراءة اليد دون إنشاء جهاز افتراضي.
- يعرض شاشة اختبار مباشرة لكل زر وعصا وزناد.
- يدمج **HidHide** بشكل آمن واختياري لمنع الإدخال المزدوج، مع نسخة احتياطية واستعادة.
- يتعامل مع الفصل وإعادة التوصيل وإرسال حالة محايدة عند الانقطاع.

**غير مدعوم في هذا الإصدار:** Bluetooth (تنسيق تقارير مختلف)، والإنترنت، وأي تعديل تلقائي على التعريفات.

---

## المتطلبات

| المتطلب | التفاصيل |
| --- | --- |
| نظام التشغيل | Windows 10 أو Windows 11، x64 |
| اليد | DualSense أو DualSense Edge عبر **USB** |
| .NET | غير مطلوب — النسخة المنشورة self-contained |
| جهاز افتراضي | **VIIPER** + مشغّل **usbip-win2** الموقّع، أو **ViGEmBus** إن كان مثبتًا |
| HidHide | اختياري، لمنع الإدخال المزدوج |

### تجهيز VIIPER (موصى به)

1. ثبّت مشغّل **usbip-win2** الموقّع من: `https://github.com/vadimgrn/usbip-win2`
2. نزّل `viiper.exe` من: `https://github.com/Alia5/VIIPER/releases`
3. ضع `viiper.exe` بجانب `DualSenseXboxBridge.exe`، أو في `PATH`، أو حدّد مساره في الإعدادات.
4. فعّل خيار «تشغيل خادم VIIPER تلقائيًا» أو شغّل `viiper.exe` يدويًا قبل استخدام التطبيق.

التطبيق يتصل بخادم VIIPER عبر TCP على `127.0.0.1:3242` (قابل للتغيير)، وينشئ جهاز `xbox360` افتراضيًا،
ثم يرسل حزم حالة بحجم 20 بايت.

### تجهيز HidHide (اختياري)

ثبّت HidHide من `https://github.com/nefarius/HidHide`. عند الضغط على «تفعيل HidHide»:

- يُحفظ نسخة احتياطية من الإعدادات الحالية في `%LOCALAPPDATA%\DualSenseXboxBridge\hidhide-backup.json`.
- يُضاف **مسار التطبيق فقط** إلى قائمة التطبيقات المسموح لها.
- يُخفى **الجهاز المحدد فقط**.
- يُفعّل الـ cloak، ثم يُتحقق من الحالة فعليًا.
- عند إيقاف الميزة أو إغلاق التطبيق تُستعاد الإعدادات السابقة.

قد تظهر نافذة UAC لأن HidHideCLI يحتاج صلاحيات مسؤول. التطبيق نفسه يعمل بصلاحيات المستخدم العادية.

---

## التثبيت

### المثبّت

نزّل `DualSenseXboxBridge-Setup-x64.exe` من صفحة Releases وشغّله.
التثبيت per-user ولا يحتاج صلاحيات مسؤول، ولا يثبّت أي تعريف، ولا يحذف إعداداتك عند إزالة التطبيق.

### النسخة المحمولة

فك ضغط `DualSenseXboxBridge-...-portable.zip` وشغّل `DualSenseXboxBridge.exe`.

---

## الاستخدام

1. وصّل اليد بكابل USB.
2. شغّل التطبيق واضغط **تحديث الأجهزة** — يجب أن تظهر DualSense.
3. اضغط **اختبار اليد** وتحقق من كل زر وعصا وزناد.
4. اضغط **تفعيل وضع Xbox** — سيُنشأ جهاز Xbox 360 افتراضي.
5. افتح `joy.cpl` أو أي أداة XInput للتأكد من ظهور الجهاز.
6. عند الحاجة، فعّل **HidHide** لمنع رؤية اليد الفعلية والجهاز الافتراضي في الوقت نفسه.
7. للإيقاف: **إيقاف**. عند الفصل تُرسل حالة محايدة تلقائيًا.

---

## البناء من المصدر

```powershell
dotnet build DualSenseXboxBridge.sln -c Release
dotnet test tests/DualSenseXboxBridge.Core.Tests/DualSenseXboxBridge.Core.Tests.csproj -c Release
dotnet publish src/DualSenseXboxBridge/DualSenseXboxBridge.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/publish
```

يتطلب .NET 8 SDK. مشروع الواجهة يستخدم `EnableWindowsTargeting` لذا يمكن التحقق من الترجمة على أي منصة،
لكن التشغيل الفعلي يتطلب Windows.

### بناء المثبّت محليًا

```powershell
choco install innosetup -y
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" `
  /DAppVersion=1.0.0 `
  /DPublishDir="$PWD\artifacts\publish" `
  /DOutputDir="$PWD\artifacts" `
  installer\DualSenseXboxBridge.iss
```

---

## البنية

```
src/DualSenseXboxBridge.Core     نواة خالصة قابلة للاختبار: parser، mapping، فلترة HID، بروتوكول VIIPER
src/DualSenseXboxBridge          تطبيق WPF: HID، محرك الربط، Backends، HidHide، الواجهة
tests/…Core.Tests                اختبارات xUnit (تعمل على Windows وLinux)
installer/                       سكربت Inno Setup
.github/workflows/build.yml      بناء + اختبارات + نشر + مثبّت + Release
```

التفاصيل في [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). الإعدادات المتقدمة (إعادة تعيين الأزرار، اختيار جهاز USB، Backend) في [docs/ADVANCED_SETTINGS.md](docs/ADVANCED_SETTINGS.md).

---

## الاختبارات

- **92 اختبار وحدة** تغطي: تحليل تقارير USB، رفض التقارير القصيرة وBluetooth، خريطة الأزرار، D-Pad،
  تحويل المحاور والزنادات، المنطقة الميتة، إعادة التعيين، فلترة الأجهزة، بروتوكول VIIPER، مسارات HidHide،
  وحفظ الإعدادات.
- **اختبارات يدوية مطلوبة** على جهاز حقيقي — القائمة الكاملة في [docs/TESTING.md](docs/TESTING.md).

---

## حدود معروفة

- Bluetooth غير مدعوم.
- بعض الألعاب تمنع الأجهزة الافتراضية أو تستخدم أنظمة إدخال مختلفة (Raw Input/Steam Input)؛ لا يمكن ضمان
  التوافق معها.
- ViGEmBus مشروع متقاعد؛ يبقى مدعومًا كخيار توافق فقط ولا يُثبّته التطبيق.
- قيم البطارية تقريبية وتُقرأ من تقرير USB دون تحقق على عتاد حقيقي.
- HidHide لا يعمل إلا إذا كان مثبتًا، وتتطلب عملياته موافقة UAC.

---

## الترخيص

لا يوجد ملف ترخيص في هذا المستودع بعد؛ أضف الترخيص الذي تريده قبل إعادة التوزيع.
VIIPER برنامج GPL-3.0 يُوزَّع منفصلًا ولا يُضمَّن داخل هذا التطبيق، ويُستخدم عبر واجهة TCP فقط.

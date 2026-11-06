# Third-party binaries

هذا المجلد يحتوي **التراخيص فقط** داخل Git. الملفات التنفيذية تُنزّل وقت البناء عبر:

```powershell
pwsh scripts/fetch-thirdparty.ps1
```

| المكوّن | الإصدار | الترخيص | المصدر |
| --- | --- | --- | --- |
| VIIPER (`viiper.exe`) | v0.9.0 | GPL-3.0 | https://github.com/Alia5/VIIPER |
| usbip-win2 (`USBip-0.9.8.1-x64.exe`) | v0.9.8.1 | BSD-2-Clause | https://github.com/vadimgrn/usbip-win2 |

كل تنزيل مثبّت على بصمة SHA-256 داخل `scripts/fetch-thirdparty.ps1`؛ أي اختلاف يوقف البناء.

الملفان موقّعان رقميًا من ناشرهما الأصلي (تم التحقق من وجود جدول شهادات Authenticode في كليهما).

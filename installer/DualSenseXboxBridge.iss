; Inno Setup script for DualSense Xbox Bridge.
; Per-user install (no admin). Never installs drivers silently: the bundled usbip-win2
; installer runs only when the user ticks the optional task, and it self-elevates via UAC.

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif

#ifndef OutputDir
  #define OutputDir "..\artifacts"
#endif

#ifndef ThirdPartyDir
  #define ThirdPartyDir "..\thirdparty"
#endif

[Setup]
AppId={{A7F3B2C1-5D64-4E1B-9C2A-7E8D4B1F0A93}
AppName=DualSense Xbox Bridge
AppVersion={#AppVersion}
AppPublisher=DualSense Xbox Bridge
AppPublisherURL=https://github.com/moha700m/DualSenseXboxBridge
AppSupportURL=https://github.com/moha700m/DualSenseXboxBridge/issues
AppUpdatesURL=https://github.com/moha700m/DualSenseXboxBridge/releases
DefaultDirName={localappdata}\Programs\DualSense Xbox Bridge
DefaultGroupName=DualSense Xbox Bridge
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir={#OutputDir}
OutputBaseFilename=DualSenseXboxBridge-Setup-x64
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\DualSenseXboxBridge.exe
UninstallDisplayName=DualSense Xbox Bridge
CloseApplications=yes
AllowNoIcons=yes
VersionInfoVersion={#AppVersion}
VersionInfoProductName=DualSense Xbox Bridge
VersionInfoDescription=DualSense Xbox Bridge installer

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked
Name: "startup"; Description: "Start DualSense Xbox Bridge with &Windows"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#ThirdPartyDir}\viiper\viiper.exe"; DestDir: "{app}\thirdparty\viiper"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#ThirdPartyDir}\usbip-win2\*.exe"; DestDir: "{app}\thirdparty\usbip-win2"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#ThirdPartyDir}\licenses\*"; DestDir: "{app}\thirdparty\licenses"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\THIRD_PARTY_NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\DualSense Xbox Bridge"; Filename: "{app}\DualSenseXboxBridge.exe"
Name: "{group}\Install USB/IP driver (administrator)"; Filename: "{app}\thirdparty\usbip-win2\USBip-0.9.8.1-x64.exe"; Flags: createonlyiffileexists
Name: "{group}\Driver requirements"; Filename: "{app}\README-DRIVER.md"
Name: "{group}\Third-party notices"; Filename: "{app}\THIRD_PARTY_NOTICES.md"
Name: "{group}\Uninstall DualSense Xbox Bridge"; Filename: "{uninstallexe}"
Name: "{autodesktop}\DualSense Xbox Bridge"; Filename: "{app}\DualSenseXboxBridge.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "DualSenseXboxBridge"; ValueData: """{app}\DualSenseXboxBridge.exe"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\DualSenseXboxBridge.exe"; Description: "Launch DualSense Xbox Bridge"; Flags: nowait postinstall skipifsilent
Filename: "{app}\thirdparty\usbip-win2\USBip-0.9.8.1-x64.exe"; Description: "Install the USB/IP driver now (administrator, recommended)"; Flags: nowait postinstall skipifsilent unchecked; Check: FileExists(ExpandConstant('{app}\thirdparty\usbip-win2\USBip-0.9.8.1-x64.exe'))
Filename: "{app}\README-DRIVER.md"; Description: "Open the driver requirements guide"; Flags: shellexec nowait postinstall skipifsilent unchecked

; User settings in %LOCALAPPDATA%\DualSenseXboxBridge are intentionally kept on uninstall.

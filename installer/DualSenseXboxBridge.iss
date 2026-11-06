; Inno Setup script for DualSense Xbox Bridge.
; Installs per user (no admin), never installs drivers, never deletes user settings on uninstall.

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif

#ifndef OutputDir
  #define OutputDir "..\artifacts"
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
Source: "{#PublishDir}\README-DRIVER.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\DualSense Xbox Bridge"; Filename: "{app}\DualSenseXboxBridge.exe"
Name: "{group}\Driver requirements"; Filename: "{app}\README-DRIVER.md"
Name: "{group}\Uninstall DualSense Xbox Bridge"; Filename: "{uninstallexe}"
Name: "{autodesktop}\DualSense Xbox Bridge"; Filename: "{app}\DualSenseXboxBridge.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "DualSenseXboxBridge"; ValueData: """{app}\DualSenseXboxBridge.exe"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\DualSenseXboxBridge.exe"; Description: "Launch DualSense Xbox Bridge"; Flags: nowait postinstall skipifsilent
Filename: "{app}\README-DRIVER.md"; Description: "Open the driver requirements guide"; Flags: shellexec nowait postinstall skipifsilent unchecked

[UninstallDelete]
; Only generated logs are removed. User settings in %LOCALAPPDATA%\DualSenseXboxBridge are kept on purpose.
Type: filesandordirs; Name: "{app}\logs"

[Code]
procedure InitializeWizard();
begin
  { Nothing to configure: the installer never installs or modifies drivers. }
end;

; ============================================================================
;  Naval Architecture Engineering Suite v1.0
;  Inno Setup 6 installer script
;  Publisher : Alper Alacam Naval Architecture - Concept Design Studio
;  Target    : Windows 10 / 11 x64
;  Output    : NavalArchitectureSuite_v1.0_Setup.exe
; ============================================================================

#define AppName      "Naval Architecture Engineering Suite"
#define AppVersion   "1.0"
#define AppPublisher "Alper Alacam Naval Architecture"
#define AppURL       "https://www.linkedin.com/in/alperalacam"
#define AppExeName   "NavalArchitectureSuite.exe"
#define AppID        "{{A7B3C2D1-E4F5-4A6B-8C9D-0E1F2A3B4C5D}"
#define PublishDir   "C:\Temp\NavalArchitectureSuite\publish"
#define ProjectDir   "C:\Temp\NavalArchitectureSuite"

[Setup]
AppId={#AppID}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} v{#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
AppCopyright=Copyright (C) 2026 Alper Alacam Naval Architecture

OutputDir={#ProjectDir}\installer
OutputBaseFilename=NavalArchitectureSuite_v1.0_Setup
SetupIconFile={#ProjectDir}\naval_architecture_suite.ico
UninstallDisplayIcon={app}\NavalArchitectureSuite.exe
UninstallDisplayName={#AppName} v{#AppVersion}

DefaultDirName={autopf}\NavalArchitectureSuite
DefaultGroupName={#AppName}
AllowNoIcons=no
DisableProgramGroupPage=yes

PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

WizardStyle=modern
WizardResizable=no
ShowLanguageDialog=no

VersionInfoVersion=1.0.0.0
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Installer
VersionInfoCopyright=Copyright (C) 2026 Alper Alacam Naval Architecture
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon";   Description: "Create a &desktop shortcut";    GroupDescription: "Additional shortcuts:"
Name: "startmenuicon"; Description: "Create a &Start Menu shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#ProjectDir}\README.txt";        DestDir: "{app}"; Flags: ignoreversion
Source: "{#ProjectDir}\HOWTO.txt";         DestDir: "{app}"; Flags: ignoreversion
Source: "{#ProjectDir}\LICENSE.txt";       DestDir: "{app}"; Flags: ignoreversion
Source: "{#ProjectDir}\RELEASE_NOTES.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autodesktop}\{#AppName}";     Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"; Comment: "Naval Architecture Engineering Suite v1.0"; Tasks: desktopicon
Name: "{group}\{#AppName}";           Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"; Comment: "Naval Architecture Engineering Suite v1.0"; Tasks: startmenuicon
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"; Tasks: startmenuicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName} now"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\{#AppPublisher}\{#AppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey

[Messages]
WelcomeLabel1=Welcome to the [name] Setup Wizard
WelcomeLabel2=This will install [name/ver] on your computer.%n%nNaval Architecture Engineering Suite is a free desktop application with 14 calculation modules and 3,358 live formulas for ship design.%n%nClick Next to continue.
FinishedLabel=Setup has finished installing [name] on your computer.%n%nLaunch from the desktop shortcut or Start Menu.%n%nThank you for using Naval Architecture Engineering Suite.

[Code]
function InitializeSetup(): Boolean;
var
  OldPath: String;
begin
  Result := True;
  if RegQueryStringValue(HKCU,
      'Software\{#AppPublisher}\{#AppName}',
      'InstallPath', OldPath) then
  begin
    if MsgBox(
        'Naval Architecture Engineering Suite is already installed at:' + #13#10 +
        OldPath + #13#10 + #13#10 +
        'Do you want to continue and overwrite the existing installation?',
        mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end;
end;

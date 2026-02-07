[Setup]
AppName=Auto Vibrance
AppVersion=1.1.0
AppPublisher=Yasser Arafat
AppPublisherURL=https://github.com/CodeDude19/AutoVibrance
DefaultDirName={autopf}\Auto Vibrance
DefaultGroupName=Auto Vibrance
OutputDir=installer
OutputBaseFilename=AutoVibrance-Setup-v1.1.0
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\AutoVibrance.exe

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Auto Vibrance"; Filename: "{app}\AutoVibrance.exe"
Name: "{group}\Uninstall Auto Vibrance"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Auto Vibrance"; Filename: "{app}\AutoVibrance.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"
Name: "startup"; Description: "Start with Windows"; GroupDescription: "Startup:"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "AutoVibrance"; ValueData: """{app}\AutoVibrance.exe"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\AutoVibrance.exe"; Description: "Launch Auto Vibrance"; Flags: nowait postinstall skipifsilent

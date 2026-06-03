#define MyAppName "The Omega Strain"
#define MyAppExeName "TheOmegaStrain.exe"
#define MyAppPublisher "RetroMesh"
#define MyAppId "{{73E9B899-CA3D-4F78-A8B3-B2A912F238D3}"

#ifndef AppVersion
#define AppVersion "0.1.0"
#endif

#ifndef PublishDir
#error PublishDir must be supplied by Build-Installer.ps1
#endif

#ifndef OutputDir
#error OutputDir must be supplied by Build-Installer.ps1
#endif

#ifndef SecretsSource
#error SecretsSource must be supplied by Build-Installer.ps1
#endif

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\The Omega Strain
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=TheOmegaStrainSetup-{#AppVersion}
SetupIconFile=..\3dTesting\GameGraphics\OmegaStrain.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Dirs]
Name: "{userappdata}\OmegaStrain"; Flags: uninsneveruninstall

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SecretsSource}"; DestDir: "{userappdata}\OmegaStrain"; DestName: "secrets.json"; Flags: ignoreversion onlyifdoesntexist uninsneveruninstall

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

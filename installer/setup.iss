[Setup]
AppName=PDV Material de Construção
AppVersion=1.0.0
AppPublisher=Sua Empresa
DefaultDirName={autopf}\PDV
DefaultGroupName=PDV Material de Construção
OutputBaseFilename=PDV_Setup_v1.0.0
OutputDir=..\installer_output
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

[Files]
Source: "..\publish\PDV.WPF.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\PDV Material de Construção"; Filename: "{app}\PDV.WPF.exe"
Name: "{commondesktop}\PDV"; Filename: "{app}\PDV.WPF.exe"

[Run]
Filename: "{app}\PDV.WPF.exe"; Description: "Iniciar PDV"; Flags: postinstall nowait skipifsilent

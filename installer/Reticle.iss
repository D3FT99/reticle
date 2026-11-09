; Reticle installer. Build with tools\build-installer.ps1, which signs the exes first and passes:
;   /DAppVersion=1.0.0  /DCertFile=<path to the public .cer>  /DCertThumbprint=<SHA-1 thumbprint>
;
; Release builds of Reticle.exe request uiAccess. Windows starts them only when they are signed by a trusted
; publisher and installed under Program Files, so this installer also trusts the signing certificate
; (LocalMachine\Root and TrustedPublisher) and removes that trust again on uninstall.

#ifndef AppVersion
  #error AppVersion is required (build with tools\build-installer.ps1)
#endif
#ifndef CertFile
  #error CertFile is required (build with tools\build-installer.ps1)
#endif
#ifndef CertThumbprint
  #error CertThumbprint is required (build with tools\build-installer.ps1)
#endif

#define Dist "..\dist"

[Setup]
AppId={{07814A8E-2E98-4D9E-BF55-2254A513D8DD}
AppName=Reticle
AppVersion={#AppVersion}
AppVerName=Reticle {#AppVersion}
AppPublisher=Diogo Trigo
DefaultDirName={autopf}\Reticle
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
LicenseFile=..\LICENSE
SetupIconFile=..\src\Reticle\Assets\Reticle.ico
UninstallDisplayIcon={app}\Reticle.exe
UninstallDisplayName=Reticle
OutputDir={#Dist}
OutputBaseFilename=Reticle-{#AppVersion}-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ShowLanguageDialog=auto

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"

[CustomMessages]
english.LaunchReticle=Start Reticle
portuguese.LaunchReticle=Iniciar o Reticle
english.TrustingCertificate=Trusting the Reticle signing certificate...
portuguese.TrustingCertificate=A confiar no certificado de assinatura do Reticle...

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#Dist}\Reticle.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Dist}\Reticle.Launcher.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#CertFile}"; DestDir: "{tmp}"; DestName: "Reticle-signing.cer"; Flags: deleteafterinstall

[Icons]
Name: "{autoprograms}\Reticle"; Filename: "{app}\Reticle.exe"
Name: "{autodesktop}\Reticle"; Filename: "{app}\Reticle.exe"; Tasks: desktopicon

[Run]
Filename: "{sys}\certutil.exe"; Parameters: "-f -addstore Root ""{tmp}\Reticle-signing.cer"""; \
    StatusMsg: "{cm:TrustingCertificate}"; Flags: runhidden waituntilterminated
Filename: "{sys}\certutil.exe"; Parameters: "-f -addstore TrustedPublisher ""{tmp}\Reticle-signing.cer"""; \
    StatusMsg: "{cm:TrustingCertificate}"; Flags: runhidden waituntilterminated
; shellexec: CreateProcess refuses uiAccess executables (error 740); the shell starts them through AppInfo.
Filename: "{app}\Reticle.exe"; Description: "{cm:LaunchReticle}"; \
    Flags: postinstall nowait skipifsilent shellexec runasoriginaluser

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/IM Reticle.exe /F"; Flags: runhidden waituntilterminated; RunOnceId: "StopReticle"
Filename: "{app}\Reticle.exe"; Parameters: "--eoa unregister"; Flags: shellexec runhidden waituntilterminated; RunOnceId: "UnregisterAT"
Filename: "{sys}\certutil.exe"; Parameters: "-delstore TrustedPublisher {#CertThumbprint}"; Flags: runhidden waituntilterminated; RunOnceId: "UntrustPublisher"
Filename: "{sys}\certutil.exe"; Parameters: "-delstore Root {#CertThumbprint}"; Flags: runhidden waituntilterminated; RunOnceId: "UntrustRoot"

[Code]
// Upgrades replace Reticle.exe, so stop a running copy first. It may run elevated with uiAccess, out of reach of
// the Restart Manager's polite close.
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/IM Reticle.exe /F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;

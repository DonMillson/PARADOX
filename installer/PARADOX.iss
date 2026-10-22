#define AppVersion "0.7.0-dev.4"
[Setup]
AppId={{BAD31444-9D27-4F0C-A2A3-5D9668EACC12}
AppName=PARADOX Among Us Mod
AppVersion={#AppVersion}
AppVerName=PARADOX {#AppVersion} (Among Us 19 test)
AppPublisher=DonMillson
AppPublisherURL=https://github.com/DonMillson/PARADOX
AppSupportURL=https://github.com/DonMillson/PARADOX/issues
DefaultDirName={autopf}\PARADOX Mod Installer
DefaultGroupName=PARADOX
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist\installer\out
OutputBaseFilename=PARADOX-Setup-v{#AppVersion}-AmongUs19-x64
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
SetupLogging=yes
UninstallDisplayName=PARADOX Mod - Among Us
UsePreviousAppDir=yes
VersionInfoVersion=0.7.0.4
VersionInfoDescription=PARADOX offline installer for Among Us 19.0.0 (experimental)

[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\dist\installer\payload\*"; DestDir: "{app}\Payload"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Install-Paradox.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "Uninstall-Paradox.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\V19_INSTALL_PL.md"; DestDir: "{app}"; DestName: "README-PL.md"; Flags: ignoreversion
Source: "README-INSTALLER.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\PARADOX\Instrukcja instalacji"; Filename: "{app}\README-PL.md"
Name: "{autoprograms}\PARADOX\Odinstaluj PARADOX"; Filename: "{uninstallexe}"

[Code]
var
  GamePage: TInputDirWizardPage;

function IsAmongUsDir(const APath: String): Boolean;
begin
  Result := FileExists(AddBackslash(APath) + 'Among Us.exe') and
            FileExists(AddBackslash(APath) + 'GameAssembly.dll') and
            DirExists(AddBackslash(APath) + 'Among Us_Data');
end;

function ScanSteamLibraries(const SteamRoot: String): String;
var
  Vdf, Line, Fragment, Dir, Candidate: String;
  P, Q, R: Integer;
begin
  Result := '';
  Candidate := AddBackslash(SteamRoot) + 'steamapps\common\Among Us';
  if IsAmongUsDir(Candidate) then begin Result := Candidate; Exit; end;
  if not LoadStringFromFile(AddBackslash(SteamRoot) + 'steamapps\libraryfolders.vdf', Vdf) then Exit;
  while Vdf <> '' do begin
    R := Pos(#10, Vdf);
    if R <= 0 then begin
      Line := Vdf;
      Vdf := '';
    end else begin
      Line := Copy(Vdf, 1, R-1);
      Delete(Vdf, 1, R);
    end;
    P := Pos('"path"', Line);
    if P <= 0 then Continue;
    Fragment := Copy(Line, P+6, Length(Line));
    P := Pos('"', Fragment);
    if P <= 0 then Continue;
    Fragment := Copy(Fragment, P+1, Length(Fragment));
    Q := Pos('"', Fragment);
    if Q <= 0 then Continue;
    Dir := Copy(Fragment, 1, Q-1);
    StringChangeEx(Dir, '\\', '\', True);
    Candidate := AddBackslash(Dir) + 'steamapps\common\Among Us';
    if IsAmongUsDir(Candidate) then begin Result := Candidate; Exit; end;
  end;
end;

function DetectGameDir(): String;
var
  Candidate, SteamRoot, Drive: String;
  I: Integer;
begin
  Result := '';
  if RegQueryStringValue(HKLM64,
    'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 945360',
    'InstallLocation', Candidate) and IsAmongUsDir(Candidate) then begin
    Result := Candidate; Exit;
  end;
  if RegQueryStringValue(HKLM32,
    'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 945360',
    'InstallLocation', Candidate) and IsAmongUsDir(Candidate) then begin
    Result := Candidate; Exit;
  end;
  if RegQueryStringValue(HKCU, 'SOFTWARE\Valve\Steam',
    'SteamPath', SteamRoot) then begin
    Result := ScanSteamLibraries(SteamRoot);
    if Result <> '' then Exit;
  end;
  Result := ScanSteamLibraries(ExpandConstant('{pf32}\Steam'));
  if Result <> '' then Exit;
  for I := Ord('C') to Ord('Z') do begin
    Drive := Chr(I) + ':\';
    Candidate := Drive + 'SteamLibrary\steamapps\common\Among Us';
    if IsAmongUsDir(Candidate) then begin Result := Candidate; Exit; end;
    Candidate := Drive + 'Steam\steamapps\common\Among Us';
    if IsAmongUsDir(Candidate) then begin Result := Candidate; Exit; end;
    Candidate := Drive + 'Games\SteamLibrary\steamapps\common\Among Us';
    if IsAmongUsDir(Candidate) then begin Result := Candidate; Exit; end;
  end;
end;

procedure InitializeWizard();
var
  Found: String;
begin
  GamePage := CreateInputDirPage(wpWelcome,
    'Katalog Among Us / Among Us directory',
    'Wybierz zainstalowana gre Among Us 19.0.0 (x64).',
    'Instalator wyszukal folder Steam. W razie potrzeby wskaz katalog zawierajacy Among Us.exe:',
    False, '');
  GamePage.Add('Among Us:');
  Found := DetectGameDir();
  if Found <> '' then GamePage.Values[0] := Found
  else GamePage.Values[0] := ExpandConstant('{pf32}\Steam\steamapps\common\Among Us');
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = GamePage.ID then
    if not IsAmongUsDir(GamePage.Values[0]) then begin
      MsgBox('Nie znaleziono kompletnej instalacji Among Us: wymagane Among Us.exe, GameAssembly.dll i Among Us_Data.' + Chr(13) + Chr(10) +
             'Select the original Among Us game folder.', mbError, MB_OK);
      Result := False;
    end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Code: Integer;
  Args: String;
begin
  if CurStep <> ssPostInstall then Exit;
  Code := -1;
  Args := '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' +
          ExpandConstant('{app}\Install-Paradox.ps1') + '" -GamePath "' +
          GamePage.Values[0] + '" -PayloadRoot "' +
          ExpandConstant('{app}\Payload') + '" -SupportRoot "' +
          ExpandConstant('{app}') + '"';
  if (not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
      Args, '', SW_HIDE, ewWaitUntilTerminated, Code)) or (Code <> 0) then begin
    MsgBox('Instalacja PARADOX nie powiodla sie (kod ' + IntToStr(Code) + ').' + Chr(13) + Chr(10) +
           'Sprawdz log instalatora i zamknij Among Us.', mbError, MB_OK);
    RaiseException('Instalacja plikow gry nie powiodla sie.');
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Code: Integer;
  Args: String;
begin
  if CurUninstallStep <> usUninstall then Exit;
  Code := -1;
  Args := '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' +
          ExpandConstant('{app}\Uninstall-Paradox.ps1') +
          '" -SupportRoot "' + ExpandConstant('{app}') + '"';
  if (not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
      Args, '', SW_HIDE, ewWaitUntilTerminated, Code)) or (Code <> 0) then
    MsgBox('Nie mozna bezpiecznie wycofac plikow gry. Zachowano zmienione pliki i kopie.' +
           Chr(13) + Chr(10) + 'Uninstall error: ' + IntToStr(Code), mbError, MB_OK);
end;

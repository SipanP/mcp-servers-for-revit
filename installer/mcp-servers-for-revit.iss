; Inno Setup script for mcp-servers-for-revit.
;
; Builds a single wizard that installs the Revit plugin for whichever Revit versions are on the
; machine, then runs the server's own `setup` verb to enable the commands and point the AI clients
; at it. Everything installs per-user, so no administrator prompt appears.
;
; Compile with:
;   iscc /DAppVersion=1.2.3 installer\mcp-servers-for-revit.iss
;
; Expects a payload laid out by scripts/build-installer-payload.ps1:
;   installer\payload\mcp-server-for-revit.exe
;   installer\payload\<year>\mcp-servers-for-revit.addin
;   installer\payload\<year>\revit_mcp_plugin\...

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

#define AppName "mcp-servers-for-revit"
#define AppPublisher "mcp-servers-for-revit"
#define AppUrl "https://github.com/mcp-servers-for-revit/mcp-servers-for-revit"
#define ServerExe "mcp-server-for-revit.exe"

[Setup]
AppId={{8E4C2A93-7F16-4D5B-9C08-3A1E7B2D6F45}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
OutputDir=..\installer-output
OutputBaseFilename={#AppName}-Setup-v{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Per-user install: no UAC prompt, which matters for the audience this wizard exists for.
PrivilegesRequired=lowest
; Revit and the server are both 64-bit, and this makes {commonpf} resolve correctly.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
DisableDirPage=yes
UninstallDisplayName={#AppName}
LicenseFile=..\LICENSE

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; The server is shared by every Revit version, so it is installed once.
Source: "payload\{#ServerExe}"; DestDir: "{app}"; Flags: ignoreversion

; One entry per supported Revit version, installed only if the user ticked it.
Source: "payload\2020\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2020"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Check: ShouldInstallYear('2020')
Source: "payload\2021\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2021"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Check: ShouldInstallYear('2021')
Source: "payload\2022\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2022"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Check: ShouldInstallYear('2022')
Source: "payload\2023\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2023"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Check: ShouldInstallYear('2023')
Source: "payload\2024\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2024"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Check: ShouldInstallYear('2024')
Source: "payload\2025\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2025"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Check: ShouldInstallYear('2025')
Source: "payload\2026\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2026"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Check: ShouldInstallYear('2026')
Source: "payload\2027\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2027"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Check: ShouldInstallYear('2027')

[Icons]
Name: "{group}\Check my {#AppName} setup"; Filename: "{cmd}"; Parameters: "/k ""{app}\{#ServerExe}"" doctor"; Comment: "Reports anything wrong with the installation"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"

[Run]
; Enables the Revit commands and writes the AI client configuration.
Filename: "{app}\{#ServerExe}"; Parameters: "setup --revit {code:SelectedYears}"; StatusMsg: "Configuring Revit and your AI clients..."; Flags: runhidden waituntilterminated

[UninstallRun]
; Leaving a dead mcpServers entry behind would make Claude Desktop fail confusingly.
Filename: "{app}\{#ServerExe}"; Parameters: "uninstall"; RunOnceId: "UnwireClients"; Flags: runhidden waituntilterminated

[Code]
const
  FirstYear = 2020;
  LastYear  = 2027;

var
  RevitPage: TInputOptionWizardPage;
  DetectedYears: array of String;
  DoneMemo: TNewStaticText;

function RevitIsInstalled(Year: String): Boolean;
begin
  Result := DirExists(ExpandConstant('{commonpf}\Autodesk\Revit ' + Year))
         or DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\' + Year));
end;

procedure InitializeWizard;
var
  Year: Integer;
  YearText: String;
  Count: Integer;
begin
  RevitPage := CreateInputOptionPage(wpSelectTasks,
    'Choose your Revit versions',
    'Which versions of Revit should this be set up for?',
    'These are the Revit versions found on this computer. Leave them all ticked unless you have a reason not to.',
    False, False);

  Count := 0;
  SetArrayLength(DetectedYears, (LastYear - FirstYear) + 1);

  for Year := FirstYear to LastYear do
  begin
    YearText := IntToStr(Year);
    if RevitIsInstalled(YearText) then
    begin
      RevitPage.Add('Revit ' + YearText);
      RevitPage.Values[Count] := True;
      DetectedYears[Count] := YearText;
      Count := Count + 1;
    end;
  end;

  SetArrayLength(DetectedYears, Count);

  if Count = 0 then
    RevitPage.Add('No Revit installation was found on this computer');
end;

function ShouldInstallYear(Year: String): Boolean;
var
  Index: Integer;
begin
  Result := False;
  for Index := 0 to GetArrayLength(DetectedYears) - 1 do
    if DetectedYears[Index] = Year then
    begin
      Result := RevitPage.Values[Index];
      Exit;
    end;
end;

{ Comma-separated list handed to the server's setup verb. }
function SelectedYears(Param: String): String;
var
  Index: Integer;
begin
  Result := '';
  for Index := 0 to GetArrayLength(DetectedYears) - 1 do
    if RevitPage.Values[Index] then
    begin
      if Result <> '' then
        Result := Result + ',';
      Result := Result + DetectedYears[Index];
    end;

  { Nothing ticked: let setup fall back to whatever it detects rather than passing an empty list. }
  if Result = '' then
    Result := '0';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = RevitPage.ID then
  begin
    if GetArrayLength(DetectedYears) = 0 then
    begin
      Result := False;
      MsgBox('No Revit installation was found.' + #13#10#13#10 +
             'Install Revit first, then run this installer again.',
             mbError, MB_OK);
    end
    else if SelectedYears('') = '0' then
    begin
      Result := False;
      MsgBox('Please tick at least one Revit version.', mbError, MB_OK);
    end;
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpFinished then
  begin
    DoneMemo := TNewStaticText.Create(WizardForm);
    DoneMemo.Parent := WizardForm.FinishedPage;
    DoneMemo.Left := WizardForm.FinishedLabel.Left;
    DoneMemo.Top := WizardForm.FinishedLabel.Top + ScaleY(60);
    DoneMemo.Width := WizardForm.FinishedLabel.Width;
    DoneMemo.WordWrap := True;
    DoneMemo.AutoSize := False;
    DoneMemo.Height := ScaleY(90);
    DoneMemo.Caption :=
      'Two last things:' + #13#10#13#10 +
      '1. Start Revit. If it asks about an add-in it does not recognise, choose "Always Load".' + #13#10 +
      '2. Restart Claude Desktop so it picks up the new settings.';
  end;
end;

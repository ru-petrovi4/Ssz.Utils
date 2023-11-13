; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define dt GetDateTimeString('yyyy/mm/dd', '.', '');
#define Company "Ssz"
#define InitiaProgramDataDirectory Company + ".CentralServer"
#define InitiaProgramDataDirectoryFullName "C:\" + InitiaProgramDataDirectory

[Setup]
AllowNoIcons=yes
AppPublisher={#Company}
; AppPublisherURL=http://www.simcode.com/
AppName={#Company} DCS Central Server
AppVerName={#Company} DCS Central Server {#dt}
CreateUninstallRegKey=yes
DefaultDirName={pf}\{#Company}\
DefaultGroupName=DCS
ShowLanguageDialog=no
ArchitecturesInstallIn64BitMode=x64
OutputDir=SetupOutput
OutputBaseFilename={#Company} DCS Central Server Setup {#dt}

Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Dirs]
; CentralServer
Name: "{code:GetProgramDataDirectory}"

[Files]
; CentralServer
Source: ".\InputFilesStd\Ssz.Dcs.CentralServer\*"; DestDir: "{app}\Dcs.CentralServer"; Excludes: "*.pdb"; Flags: recursesubdirs replacesameversion
; Common
Source: ".\InputFilesStd\Prerequisites\Ssz.EditJson.exe"; DestDir: "{app}\Common"; Excludes: "*.pdb"; Flags: recursesubdirs replacesameversion
; Files Store
Source: ".\ProgramDataDirectory\*"; DestDir: "{code:GetProgramDataDirectory}"; Excludes: "*.pdb"; Flags: recursesubdirs replacesameversion uninsneveruninstall

[Icons]
; Name: "{group}\Xi Server Global (CTC Server) Admin"; Filename: "{app}\\Dcs.CentralServerSsz.Dcs.CentralServer.exe"; Parameters: --Admin;

[Run]
; Common
Filename: {app}\Common\Ssz.EditJson.exe; Parameters: """{app}\Dcs.CentralServer\appsettings.json"" ProgramDataDirectory ""{code:GetProgramDataDirectory}"""; Flags: runhidden
; Firewall CentralServer
Filename: netsh; Parameters: "advfirewall firewall delete rule name=""Ssz.Dcs.CentralServer60060"""; Flags: runhidden
Filename: netsh; Parameters: "advfirewall firewall add rule name=""Ssz.Dcs.CentralServer60060"" protocol=TCP dir=in localport=60060 action=allow"; Flags: runhidden
; Service CentralServer
Filename: sc; Parameters: "create Ssz.Dcs.CentralServer NameToDisplay= ""Ssz.Dcs.CentralServer"" binPath= ""{app}\Dcs.CentralServer\Ssz.Dcs.CentralServer.exe"" start= auto"; Flags: runhidden
Filename: sc; Parameters: "failure Ssz.Dcs.CentralServer reset= 5 actions= restart/5000"; Flags: runhidden
Filename: sc; Parameters: "failureflag Ssz.Dcs.CentralServer 1"; Flags: runhidden
Filename: sc; Parameters: "start Ssz.Dcs.CentralServer"; Flags: runhidden

[UninstallRun]
; Service CentralServer
Filename: sc; Parameters: "stop Ssz.Dcs.CentralServer"; Flags: runhidden
Filename: timeout; Parameters: "/t 20 /nobreak"; Flags: runhidden
Filename: sc; Parameters: "delete Ssz.Dcs.CentralServer"; Flags: runhidden
; Firewall CentralServer
Filename: netsh; Parameters: "advfirewall firewall delete rule name=""Ssz.Dcs.CentralServer60060"""; Flags: runhidden

[Code]

{Function AllocateAndInitializeSid( pIdentifierAuthority: Byte[], nSubAuthorityCount: Byte,  sa0, sa1, sa2, sa3, sa4, sa5, sa6, sa7 : Dword, pSid :^Byte ) : Integer;
;external 'AllocateAndInitializeSid@Advapi32.dll stdcall' }

{Function ConvertStringSidToSid( StringSid : String; pSid : ^Integer ) : Integer;}
Function ConvertStringSidToSid( StringSid : String; var pSid : LongWord ) : Integer;
#ifdef UNICODE
external 'ConvertStringSidToSidW@Advapi32.dll stdcall setuponly';
#else
external 'ConvertStringSidToSidA@Advapi32.dll stdcall setuponly';
#endif
{Procedure LookupAccountSid( sysName: WideString; pSid : Pointer; lpName : WideString; cchName : ^LongWord; lpReferencedDomainName : WideString; cchReferencedDomainName : ^LongWord, peUse : ^Integer) : Integer;}
Function LookupAccountSid( defNull: LongWord; pSid : LongWord; lpName : String; var cchName : LongWord; lpReferencedDomainName : String; var cchReferencedDomainName : LongWord; var peUse : Integer): Integer;
#ifdef UNICODE
external 'LookupAccountSidW@Advapi32.dll stdcall setuponly';
#else
external 'LookupAccountSidA@Advapi32.dll stdcall setuponly';
#endif
{BOOL WINAPI LookupAccountSid(
  _In_opt_   LPCTSTR lpSystemName,
  _In_       PSID lpSid,
  _Out_opt_  LPTSTR lpName,
  _Inout_    LPDWORD cchName,
  _Out_opt_  LPTSTR lpReferencedDomainName,
  _Inout_    LPDWORD cchReferencedDomainName,
  _Out_      PSID_NAME_USE peUse
)}


{Procedure FreeSid( pSid : Pointer ) ;}
Procedure FreeSid( pSid : LongWord ) ;
external 'FreeSid@Advapi32.dll stdcall setuponly';

Procedure LocalFree( p : LongWord ) ;
external 'LocalFree@Kernel32.dll stdcall setuponly';

                               //                                           "S-1-5-32-544",                "Administrators group");
                               //                                           "S-1-5-32-545",                "Users group");
                               //                                           "S-1-5-32-546",                "Guests group");
                               //                                           "S-1-5-32-547",                "Power Users group");
                               //                                           "S-1-5-18",                                           "System");
                               //                                           "S-1-5-19",                                           "LocalService");
                               //                                           "S-1-5-20",                                           "NetworkService");

Function GetLocalGroupOrUserName( SidString : String ) : String;
var
   Sid : LongWord;
   SidNameUseHolder : Integer;
   NameHolder : String;
   NameSize : LongWord;
   DomainNameHolder : String;
   DomainNameSize : LongWord;
   bResult : Integer;
begin

  Sid := 0;
  NameHolder := StringOfChar( chr(0), 255 );  { allocate buffer }
  NameSize := 254;
  DomainNameHolder := StringOfChar( ' ', 17 );  { allocate buffer }
  DomainNameSize := 15;
  Result:='';

#ifdef DEBUG
  MsgBox('Sid string= ' + SidString, mbInformation, MB_OK);
#endif
  bResult := ConvertStringSidToSid( SidString, Sid );
#ifdef DEBUG
  MsgBox('bResult =  '+IntToStr( bResult), mbInformation, MB_OK);
#endif
  if bResult <> 0 then
  begin
    if LookupAccountSid( 0, Sid, NameHolder, NameSize, DomainNameHolder, DomainNameSize, SidNameUseHolder) <> 0 then
    begin 
#ifdef DEBUG
      MsgBox('Group name =' + NameHolder + ' Domain name =' + DomainNameHolder, mbInformation, MB_OK);
#endif
      Result:=Copy( NameHolder, 0, NameSize);
    end
    else
    begin
#ifdef DEBUG
      MsgBox('LookupAccountSid unsuccessfull', mbInformation, MB_OK);
#endif
    end;
    LocalFree( Sid );
    {FreeSid( Sid );}
  end
  else
  begin
#ifdef DEBUG
    MsgBox('ConvertStringSidToSid unsuccessfull '+IntToStr( Sid), mbInformation, MB_OK)
#endif
  end;
end;

var
  Page: TInputDirWizardPage;

function GetProgramDataDirectory(param: String) : String;
var 
  res: String;
begin  
  Result:=Page.Values[0];
end;

procedure InitializeWizard;
begin
  // Create the page
  Page := CreateInputDirPage(wpWelcome,
  '���������� ������ �������', '',
  '������� ����������, ��� ����� ��������� ������ ������� Dcs, ����� ������� "�����".',
  False, '{#InitiaProgramDataDirectory}');

  // Add items (False means it's not a password edit)
  Page.Add('');
 
  // Set initial values (optional)
  Page.Values[0] := '{#InitiaProgramDataDirectoryFullName}'; 
end;

function InitializeSetup(): Boolean;
var
   oldVersion: String;
   uninstaller: String;
   ErrorCode: Integer;
   Install: Cardinal;
   OK: Boolean;   
begin
  // TODO: Add exact version check
  OK := False;
  if RegKeyExists(HKEY_LOCAL_MACHINE,
    'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full') then
  begin
    RegQueryDWordValue(HKEY_LOCAL_MACHINE,
      'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full',
      'Install', Install);
    if Install = 1 then OK := True;
  end;
  if Not OK Then
  begin
    MsgBox('�� ���� ���������� �� ���������� .NET Framework 4.5.2' + Chr(13) + '��������� {#emit SetupSetting("AppVerName")} ����������.',
      mbCriticalError, MB_OK);
    Result := False;
    Exit;
  end;
    
  if RegKeyExists(HKEY_LOCAL_MACHINE,
    'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppName")}_is1') then
  begin
    RegQueryStringValue(HKEY_LOCAL_MACHINE,
      'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppName")}_is1',
      'NameToDisplay', oldVersion);
    if MsgBox('�� ���� ���������� ��� ���������� ''' + oldVersion + '''.' + Chr(13) + '���������� ������� ����� ������������� ����������� ����������� ����� ���������� ������.' + Chr(13) + '��������������� ''' + oldVersion + ''' � ���������� ''{#emit SetupSetting("AppVerName")}''?',
      mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := False;
    end
    else
    begin
      RegQueryStringValue(HKEY_LOCAL_MACHINE,            'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppName")}_is1',
        'UninstallString', uninstaller);
      ShellExec('', uninstaller, '/SILENT', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
      Result := True;
    end;
  end
  else

  begin
    Result := True;        
  end;
end;
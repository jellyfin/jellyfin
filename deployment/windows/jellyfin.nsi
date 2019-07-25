; Shows a lot of debug information while compiling
; This can be removed once stable.
!verbose 4
;--------------------------------
!define SF_USELECTED  0 ; used to check selected options status, rest are inherited from Sections.nsh

    !include "MUI2.nsh"
	!include "Sections.nsh"
	!include "LogicLib.nsh"

; Global variables that we'll use
    Var _JELLYFINVERSION_
    Var _JELLYFINDATADIR_
	Var _INSTALLSERVICE_
	Var _SERVICESTART_
	Var _NETWORKSERVICEACCOUNT_
	Var _EXISTINGINSTALLATION_
	Var _EXISTINGSERVICE_
	Var _CUSTOMDATAFOLDER_

!if ${NSIS_PTR_SIZE} > 4
  !define BITS 64
  !define NAMESUFFIX " (64 bit)"
!else
  !define BITS 32
  !define NAMESUFFIX ""
!endif

;--------------------------------

	!define REG_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" ;Registry to show up in Add/Remove Programs
	
    !getdllversion "$%InstallLocation%\jellyfin.dll" ver_ ;Align installer version with jellyfin.dll version
    Name "Jellyfin Server ${ver_1}.${ver_2}.${ver_3}" ; This is referred in various header text labels	
    OutFile "jellyfin_${ver_1}.${ver_2}.${ver_3}_windows.exe" ; Naming convention jellyfin_{version}_windows-{arch].exe
    BrandingText "Jellyfin Server ${ver_1}.${ver_2}.${ver_3} Installer" ; This shows in just over the buttons

; installer attributes, these show up in details tab on installer properties
    VIProductVersion "${ver_1}.${ver_2}.${ver_3}.0" ; VIProductVersion format, should be X.X.X.X
    VIFileVersion "${ver_1}.${ver_2}.${ver_3}.0" ; VIFileVersion format, should be X.X.X.X
    VIAddVersionKey "ProductName" "Jellyfin Server"
    VIAddVersionKey "FileVersion" "${ver_1}.${ver_2}.${ver_3}.0"
	VIAddVersionKey "LegalCopyright" "Jellyfin, Free Software Media System"
	VIAddVersionKey "FileDescription" "Jellyfin Server"  
	
;TODO, check defaults
    InstallDir "$PROGRAMFILES\Jellyfin" ;Default installation folder
    InstallDirRegKey HKLM "Software\Jellyfin" "InstallFolder" ;Read the registry for install folder,
  
    RequestExecutionLevel admin ; ask it upfront for service control, and installing in priv folders
	
    CRCCheck on ; make sure the installer wasn't corrupted while downloading
	
    !define MUI_ABORTWARNING ;Prompts user in case of aborting install

; TODO: Replace with nice Jellyfin Icons
	!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\nsis3-install.ico" ; Installer Icon
	!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\nsis3-uninstall.ico" ; Uninstaller Icon
	
	!define MUI_HEADERIMAGE
	!define MUI_HEADERIMAGE_BITMAP "${NSISDIR}\Contrib\Graphics\Header\nsis3-branding.bmp"
	!define MUI_WELCOMEFINISHPAGE_BITMAP "${NSISDIR}\Contrib\Graphics\Wizard\nsis3-branding.bmp"

;--------------------------------
;Pages

; Welcome Page
	!define MUI_WELCOMEPAGE_TEXT "The installer will ask for details to install Jellyfin Server.$\r$\n$\r$\n$\r$\n\
	ADVANCED:$\r$\n\
	The default service install uses Local System account and is sufficient for most users. $\r$\n$\r$\n\
	You can choose to install using Network Service account under advanced options. This also affects where Jellyfin Server and Jellyfin data can be installed. The installer will NOT check this, you should know what you are doing.$\r$\n$\r$\n\
	You can choose the folder for Jellyfin Metadata under advanced options based on your needs."
	!insertmacro MUI_PAGE_WELCOME
; License Page
    !insertmacro MUI_PAGE_LICENSE "$%InstallLocation%\LICENSE" ; picking up generic GPL
; Components Page
	!define MUI_COMPONENTSPAGE_SMALLDESC 
    !insertmacro MUI_PAGE_COMPONENTS
    !define MUI_PAGE_CUSTOMFUNCTION_PRE HideInstallDirectoryPage ; Controls when to hide / show
	!define MUI_DIRECTORYPAGE_TEXT_DESTINATION "Install folder" ; shows just above the folder selection dialog
    !insertmacro MUI_PAGE_DIRECTORY

; Metadata folder Page
    !define MUI_PAGE_CUSTOMFUNCTION_PRE HideDataDirectoryPage ; Controls when to hide / show
	!define MUI_PAGE_HEADER_SUBTEXT "Choose the folder in which to install the Jellyfin Server metadata."
	!define MUI_DIRECTORYPAGE_TEXT_TOP "The installer will set the following folder for Jellyfin Server metadata. To install in a differenct folder, click Browse and select another folder. Please make sure the folder exists. Click Next to continue."
	!define MUI_DIRECTORYPAGE_TEXT_DESTINATION "Metadata folder"
	!define MUI_DIRECTORYPAGE_VARIABLE $_JELLYFINDATADIR_
	!insertmacro MUI_PAGE_DIRECTORY

; Confirmation Page	
	Page custom ConfirmationPage ; just letting the user know what they chose to install

; Actual Installion Page	
	!insertmacro MUI_PAGE_INSTFILES


    !insertmacro MUI_UNPAGE_CONFIRM
    !insertmacro MUI_UNPAGE_INSTFILES
	!insertmacro MUI_UNPAGE_FINISH
  
;--------------------------------
;Languages; Add more languages later here if needed 
    !insertmacro MUI_LANGUAGE "English"

;--------------------------------
;Installer Sections
Section "Jellyfin Server (required)" InstallJellyfin
    SectionIn RO ; Mandatory section, isn't this the whole purpose to run the installer.
	
	StrCmp "$_EXISTINGINSTALLATION_" "YES" RunUninstaller CarryOn ; Silently uninstall in case of previous installation
	
	RunUninstaller:
	DetailPrint "Looking for uninstaller at $INSTDIR"
    FindFirst $0 $1 "$INSTDIR\Uninstall.exe"
    FindClose $0
    StrCmp $1 "" CarryOn ; the registry key was there but uninstaller was not found

    DetailPrint "Silently running the uninstaller at $INSTDIR"
    ExecWait '"$INSTDIR\Uninstall.exe" /S _?=$INSTDIR' $0
	DetailPrint "Uninstall finished, $0"

	CarryOn:

    SetOutPath "$INSTDIR"

    File /r $%InstallLocation%\* 
	
; Write the InstallFolder, DataFolder, Network Service info into the registry for later use
    WriteRegExpandStr HKLM "Software\Jellyfin" "InstallFolder" "$INSTDIR"
	WriteRegExpandStr HKLM "Software\Jellyfin" "DataFolder" "$_JELLYFINDATADIR_"
	WriteRegStr HKLM "Software\Jellyfin" "NetworkService" "$_NETWORKSERVICEACCOUNT_"

    !getdllversion "$%InstallLocation%\jellyfin.dll" ver_
    StrCpy $_JELLYFINVERSION_ "${ver_1}.${ver_2}.${ver_3}" ;

; Write the uninstall keys for Windows
    WriteRegStr HKLM "${REG_UNINST_KEY}" "DisplayName" "Jellyfin $_JELLYFINVERSION_"
    WriteRegExpandStr HKLM "${REG_UNINST_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
    WriteRegStr HKLM "${REG_UNINST_KEY}" "DisplayIcon" '"$INSTDIR\Jellyfin.exe",0'
    WriteRegStr HKLM "${REG_UNINST_KEY}" "Publisher" "The Jellyfin project"
    WriteRegStr HKLM "${REG_UNINST_KEY}" "URLInfoAbout" "https://jellyfin.github.io/"
    WriteRegStr HKLM "${REG_UNINST_KEY}" "DisplayVersion" "$_JELLYFINVERSION_"
    WriteRegDWORD HKLM "${REG_UNINST_KEY}" "NoModify" 1
    WriteRegDWORD HKLM "${REG_UNINST_KEY}" "NoRepair" 1

;Create uninstaller
    WriteUninstaller "$INSTDIR\Uninstall.exe"

SectionEnd

Section "Jellyfin Service" InstallService

    ExecWait '"$INSTDIR\nssm.exe" install Jellyfin "$INSTDIR\jellyfin.exe" --datadir "$_JELLYFINDATADIR_"' $0
    DetailPrint "Jellyfin Service install, $0"
	
    Sleep 3000 ; Give time for Windows to catchup
	
    ExecWait '"$INSTDIR\nssm.exe" set Jellyfin Start SERVICE_DELAYED_AUTO_START' $0
    DetailPrint "Jellyfin Service setting, $0"
	
    Sleep 3000
	${If} $_NETWORKSERVICEACCOUNT_ == "YES"
		DetailPrint "Attempting to change service account to Network Service"
		ExecWait '"$INSTDIR\nssm.exe" set Jellyfin Objectname "Network Service"' $0
		DetailPrint "Jellyfin service account change, $0"	
	${EndIf}

SectionEnd

Section "Start Jellyfin service after install" StartService

    ExecWait '"$INSTDIR\nssm.exe" start Jellyfin' $0
    DetailPrint "Jellyfin service start, $0"
    
SectionEnd

SectionGroup "Advanced"
Section /o "Use Network Service account" NetworkServiceAccount
	; The section is for user choice, nothing to do here
SectionEnd
Section /o "Custom Jellyfin metadata folder" CustomDataFolder
	; The section is for user choice, nothing to do here
SectionEnd
SectionGroupEnd


;--------------------------------
;Descriptions

;Language strings
    LangString DESC_InstallJellyfin ${LANG_ENGLISH} "Install Jellyfin Server"
    LangString DESC_InstallService ${LANG_ENGLISH} "Install As a Service"
    LangString DESC_StartService ${LANG_ENGLISH} "Start Jellyfin service after Install"
    LangString DESC_NetworkServiceAccount ${LANG_ENGLISH} "Use Network Service account to start windows service"
    LangString DESC_CustomDataFolder ${LANG_ENGLISH} "Choose Jellyfin Server metadata folder in subsequent steps"

;Assign language strings to sections
    !insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
    !insertmacro MUI_DESCRIPTION_TEXT ${InstallJellyfin} $(DESC_InstallJellyfin)
    !insertmacro MUI_DESCRIPTION_TEXT ${InstallService} $(DESC_InstallService)
    !insertmacro MUI_DESCRIPTION_TEXT ${StartService} $(DESC_StartService)
    !insertmacro MUI_DESCRIPTION_TEXT ${NetworkServiceAccount} $(DESC_NetworkServiceAccount)
    !insertmacro MUI_DESCRIPTION_TEXT ${CustomDataFolder} $(DESC_CustomDataFolder)
    !insertmacro MUI_FUNCTION_DESCRIPTION_END

;--------------------------------
;Uninstaller Section

Section "Uninstall"

	ReadRegStr $INSTDIR HKLM "Software\Jellyfin" "InstallFolder"  ; read the installation folder
	ReadRegStr $_JELLYFINDATADIR_ HKLM "Software\Jellyfin" "DataFolder"  ; read the metadata folder

	DetailPrint "Jellyfin Install location : $INSTDIR"
	DetailPrint "Jellyfin data folder : $_JELLYFINDATADIR_"
	
	MessageBox MB_YESNO|MB_ICONINFORMATION "Do you want to retain Jellyfin metadata folder? The media will not be touched. $\r$\nIf unsure choose YES." /SD IDYES IDYES PreserveData

    RMDir /r /REBOOTOK "$_JELLYFINDATADIR_"
	
	PreserveData:

	DetailPrint "Attempting to stop Jellyfin Server"
    ExecWait '"$INSTDIR\nssm.exe" stop Jellyfin' $0
    DetailPrint "Jellyfin service stop, $0"
	DetailPrint "Attempting to remove Jellyfin service"
    ExecWait '"$INSTDIR\nssm.exe" remove Jellyfin confirm' $0
    DetailPrint "Jellyfin Service remove, $0"

    Delete "$INSTDIR\Uninstall.exe"
	
    RMDir /r /REBOOTOK "$INSTDIR" 
	
    DeleteRegKey HKLM "Software\Jellyfin"
    DeleteRegKey HKLM "${REG_UNINST_KEY}"

SectionEnd



Function .onInit
; Setting up defaults
	StrCpy $_INSTALLSERVICE_ "YES"
	StrCpy $_SERVICESTART_ "YES"
	StrCpy $_CUSTOMDATAFOLDER_ "NO"
	StrCpy $_NETWORKSERVICEACCOUNT_ "NO"
	StrCpy $_EXISTINGINSTALLATION_ "NO"
	StrCpy $_EXISTINGSERVICE_ "NO"
	
    SetShellVarContext current
    StrCpy $_JELLYFINDATADIR_ "$%ProgramData%\jellyfin\"

;Detect if Jellyfin is already installed.
; In case it is installed, let the user choose either
;	1. Exit installer
;   2. Upgrade without messing with data 
; 		2a. Don't ask for any details, uninstall and install afresh with old settings

; Read Registry for previous installation
	ClearErrors
	ReadRegStr "$0" HKLM "Software\Jellyfin" "InstallFolder"
	IfErrors NoExisitingInstall
	
	DetailPrint "Existing Jellyfin detected at: $0"
	StrCpy "$INSTDIR" "$0" ; set the location fro registry as new default
		
	StrCpy $_EXISTINGINSTALLATION_ "YES" ; Set our flag to be used later
	SectionSetText ${InstallJellyfin} "Upgrade Jellyfin Server(required)" ; Change install text to "Upgrade"
	
; check if there is a service called Jellyfin, there should be
; hack : nssm statuscode Jellyfin will return non zero return code in case it exists
    ExecWait '"$INSTDIR\nssm.exe" statuscode Jellyfin' $0
    DetailPrint "Jellyfin service statuscode, $0"
	IntCmp $0 0 NoService ; service doesn't exist, may be run from desktop shortcut

	; if service was detected, set defaults going forward.
	StrCpy $_EXISTINGSERVICE_ "YES"
	StrCpy $_INSTALLSERVICE_ "YES"
	StrCpy $_SERVICESTART_ "YES"
	
	; check if service was run using Network Service account
	ClearErrors
	ReadRegStr "$_NETWORKSERVICEACCOUNT_" HKLM "Software\Jellyfin" "NetworkService" ; in case of error _NETWORKSERVICEACCOUNT_ will be NO as default

	ClearErrors
	ReadRegStr $_JELLYFINDATADIR_ HKLM "Software\Jellyfin" "DataFolder" ; in case of error, the default holds
		
	; Hide sections which will not be needed in case of previous install
    SectionSetText ${InstallService} ""
	SectionSetText ${StartService} ""
	SectionSetText ${NetworkServiceAccount} ""
	SectionSetText ${CustomDataFolder} ""
 
	NoService: ; existing install was present but no service was detected
	
; Let the user know that we'll upgrade and provide an option to quit.
	MessageBox MB_OKCANCEL|MB_ICONINFORMATION "Existing installation of Jellyfin was detected, it'll be upgraded, settings will be retained. \
	$\r$\nClick OK to proceed, Cancel to exit installer." /SD IDOK IDOK ProceedWithUpgrade
	Quit ; Quit if the user is not sure about upgrade

	ProceedWithUpgrade:
	
	NoExisitingInstall:
; by this time, the variables have been correctly set to reflect previous install details

FunctionEnd

Function HideInstallDirectoryPage
	${If} $_EXISTINGINSTALLATION_ == "YES" ; Existing installation detected, so don't ask for InstallFolder
		Abort
	${EndIf}
FunctionEnd

; Don't show custom folder option in case it wasn't chosen
Function HideDataDirectoryPage
	${If} $_CUSTOMDATAFOLDER_ == "NO"
		Abort
	${EndIf}
FunctionEnd

; This function handles the choices during component selection
Function .onSelChange
	SectionGetFlags ${CustomDataFolder} $0
	${If} $0 = ${SF_SELECTED}
		StrCpy $_CUSTOMDATAFOLDER_ "YES"
	${Else}
		StrCpy $_CUSTOMDATAFOLDER_ "NO"
	${EndIf}
	
; If we are not installing service, we don't need to set the NetworkService account or StartService
	SectionGetFlags ${InstallService} $0
	${If} $0 = ${SF_SELECTED}
		StrCpy $_INSTALLSERVICE_ "YES"
		SectionGetFlags ${NetworkServiceAccount} $0
		IntOp $0 $0 | ${SF_RO}
		IntOp $0 $0 ^ ${SF_RO}	
		SectionSetFlags ${NetworkServiceAccount} $0
		SectionGetFlags ${StartService} $0
		IntOp $0 $0 | ${SF_RO}
		IntOp $0 $0 ^ ${SF_RO}	
		SectionSetFlags ${StartService} $0
	${Else}
		StrCpy $_INSTALLSERVICE_ "NO"
		IntOp $0 ${SF_USELECTED} | ${SF_RO}
		SectionSetFlags ${NetworkServiceAccount} $0
		SectionSetFlags ${StartService} $0
	${EndIf}
	
	SectionGetFlags ${StartService} $0
	${If} $0 = ${SF_SELECTED}
		StrCpy $_SERVICESTART_ "YES"
	${Else}
		StrCpy $_SERVICESTART_ "NO"
	${EndIf}	

	SectionGetFlags ${NetworkServiceAccount} $0
	${If} $0 = ${SF_SELECTED}
		StrCpy $_NETWORKSERVICEACCOUNT_ "YES"
	${Else}
		StrCpy $_NETWORKSERVICEACCOUNT_ "NO"
	${EndIf}
	
	  
FunctionEnd

Function ConfirmationPage
	!insertmacro MUI_HEADER_TEXT "Confirmation Page" "Please confirm your choices for Jellyfin Server installation"

	nsDialogs::Create 1018
	
	${NSD_CreateLabel} 0 0 100% 100% "The installer will proceed based on the following inputs gathered on earlier screens.$\r$\n$\r$\n\
	Installation Folder : $INSTDIR$\r$\n\
	Service install : $_INSTALLSERVICE_$\r$\n\
	Service start : $_SERVICESTART_$\r$\n\
	Network Service Account : $_NETWORKSERVICEACCOUNT_$\r$\n\
	Custom Metadata folder : $_CUSTOMDATAFOLDER_$\r$\n\
	Jellyfin Metadata Folder: $_JELLYFINDATADIR_"
	nsDialogs::Show

FunctionEnd

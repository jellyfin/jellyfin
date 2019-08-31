; Shows a lot of debug information while compiling
; This can be removed once stable.
!verbose 4
!define MUI_VERBOSE 4
SetCompress off ; This is for speed during development
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
    Var _SERVICEACCOUNTTYPE_
    Var _EXISTINGINSTALLATION_
    Var _EXISTINGSERVICE_
;
!ifdef x64
    !define ARCH "x64"
    !define NAMESUFFIX "(64 bit)"
    !define INSTALL_DIRECTORY "$PROGRAMFILES64\Jellyfin\Server"
!endif

!ifdef x84
    !define ARCH "x86"
    !define NAMESUFFIX "(32 bit)"
    !define INSTALL_DIRECTORY "$PROGRAMFILES32\Jellyfin\Server"
!endif

!ifndef ARCH
    !error "Set the Arch with /Dx86 or /Dx64"
!endif

;--------------------------------

    !define REG_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\JellyfinServer" ;Registry to show up in Add/Remove Programs
    !define REG_CONFIG_KEY "Software\Jellyfin\Server" ;Registry to store all configuration

    !getdllversion "$%InstallLocation%\jellyfin.dll" ver_ ;Align installer version with jellyfin.dll version

    Name "Jellyfin Server ${ver_1}.${ver_2}.${ver_3} ${NAMESUFFIX}" ; This is referred in various header text labels
    OutFile "jellyfin_${ver_1}.${ver_2}.${ver_3}_windows-${ARCH}.exe" ; Naming convention jellyfin_{version}_windows-{arch].exe
    BrandingText "Jellyfin Server ${ver_1}.${ver_2}.${ver_3} Installer" ; This shows in just over the buttons

; installer attributes, these show up in details tab on installer properties
    VIProductVersion "${ver_1}.${ver_2}.${ver_3}.0" ; VIProductVersion format, should be X.X.X.X
    VIFileVersion "${ver_1}.${ver_2}.${ver_3}.0" ; VIFileVersion format, should be X.X.X.X
    VIAddVersionKey "ProductName" "Jellyfin Server"
    VIAddVersionKey "FileVersion" "${ver_1}.${ver_2}.${ver_3}.0"
    VIAddVersionKey "LegalCopyright" "(c) 2019 Jellyfin Contributors. Code released under the GNU General Public License"
    VIAddVersionKey "FileDescription" "Jellyfin Server: The Free Software Media System"

;TODO, check defaults
    InstallDir ${INSTALL_DIRECTORY} ;Default installation folder
    InstallDirRegKey HKLM "${REG_CONFIG_KEY}" "InstallFolder" ;Read the registry for install folder,

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
    !define MUI_WELCOMEPAGE_TEXT "The installer will ask for details to install Jellyfin Server."
    !insertmacro MUI_PAGE_WELCOME
; License Page
    !insertmacro MUI_PAGE_LICENSE "$%InstallLocation%\LICENSE" ; picking up generic GPL
; Components Page
    !define MUI_COMPONENTSPAGE_SMALLDESC
    !insertmacro MUI_PAGE_COMPONENTS
    !define MUI_PAGE_CUSTOMFUNCTION_PRE HideInstallDirectoryPage ; Controls when to hide / show
    !define MUI_DIRECTORYPAGE_TEXT_DESTINATION "Install folder" ; shows just above the folder selection dialog
    !insertmacro MUI_PAGE_DIRECTORY

; Data folder Page
    !define MUI_PAGE_CUSTOMFUNCTION_PRE HideDataDirectoryPage ; Controls when to hide / show
    !define MUI_PAGE_HEADER_TEXT "Choose Data Location"
    !define MUI_PAGE_HEADER_SUBTEXT "Choose the folder in which to install the Jellyfin Server data."
    !define MUI_DIRECTORYPAGE_TEXT_TOP "The installer will set the following folder for Jellyfin Server data. To install in a different folder, click Browse and select another folder. Please make sure the folder exists and is accessible. Click Next to continue."
    !define MUI_DIRECTORYPAGE_TEXT_DESTINATION "Data folder"
    !define MUI_DIRECTORYPAGE_VARIABLE $_JELLYFINDATADIR_
    !insertmacro MUI_PAGE_DIRECTORY

; Custom Dialogs
    !include "CustomPageWrapper.nsh"
    !include "service-account-type.nsdinc"
    !include "confirmation.nsdinc"

; Select service account type
    !define MUI_PAGE_CUSTOMFUNCTION_PRE HideServiceAccountTypePage ; Controls when to hide / show (This does not work for Page, might need to go PageEx)
    !define MUI_PAGE_CUSTOMFUNCTION_SHOW fnc_service_account_type_Show
    !define MUI_PAGE_CUSTOMFUNCTION_LEAVE ServiceAccountTypePage_Config
    !insertmacro MUI_PAGE_CUSTOM ServiceAccountType
    #Page custom fnc_service_account_type_Show ServiceAccountTypePage_Config

; Confirmation Page
    !define MUI_PAGE_CUSTOMFUNCTION_PRE PreConfirm ; Controls when to hide / show (This does not work for Page, might need to go PageEx)
    !define MUI_PAGE_CUSTOMFUNCTION_SHOW fnc_confirmation_Show
    !insertmacro MUI_PAGE_CUSTOM Confirmation ; THIS CRASHES THE INSTALLER IS THE PREVIOUS PAGE WAS NOT HIDDEN
    #Page custom fnc_confirmation_Show ; just letting the user know what they chose to install

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
Section "Jellyfin Server (required)" InstallJellyfinServer
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
    WriteRegExpandStr HKLM "${REG_CONFIG_KEY}" "InstallFolder" "$INSTDIR"
    WriteRegExpandStr HKLM "${REG_CONFIG_KEY}" "DataFolder" "$_JELLYFINDATADIR_"
    WriteRegStr HKLM "${REG_CONFIG_KEY}" "ServiceAccountType" "$_SERVICEACCOUNTTYPE_"

    !getdllversion "$%InstallLocation%\jellyfin.dll" ver_
    StrCpy $_JELLYFINVERSION_ "${ver_1}.${ver_2}.${ver_3}" ;

; Write the uninstall keys for Windows
    WriteRegStr HKLM "${REG_UNINST_KEY}" "DisplayName" "Jellyfin Server $_JELLYFINVERSION_ ${NAMESUFFIX}"
    WriteRegExpandStr HKLM "${REG_UNINST_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
    WriteRegStr HKLM "${REG_UNINST_KEY}" "DisplayIcon" '"$INSTDIR\jellyfin.exe",0'
    WriteRegStr HKLM "${REG_UNINST_KEY}" "Publisher" "The Jellyfin Project"
    WriteRegStr HKLM "${REG_UNINST_KEY}" "URLInfoAbout" "https://jellyfin.media/"
    WriteRegStr HKLM "${REG_UNINST_KEY}" "DisplayVersion" "$_JELLYFINVERSION_"
    WriteRegDWORD HKLM "${REG_UNINST_KEY}" "NoModify" 1
    WriteRegDWORD HKLM "${REG_UNINST_KEY}" "NoRepair" 1

;Create uninstaller
    WriteUninstaller "$INSTDIR\Uninstall.exe"
SectionEnd

Section "Jellyfin Server Service" InstallService

    ExecWait '"$INSTDIR\nssm.exe" install JellyfinServer "$INSTDIR\jellyfin.exe" --datadir "$_JELLYFINDATADIR_"' $0
    DetailPrint "Jellyfin Server Service install, $0"

    Sleep 3000 ; Give time for Windows to catchup

    ExecWait '"$INSTDIR\nssm.exe" set Jellyfin Start SERVICE_DELAYED_AUTO_START' $0
    DetailPrint "Jellyfin Server Service setting, $0"

    Sleep 3000
    ${If} $_SERVICEACCOUNTTYPE_ == "NetworkService" ; the default install using NSSM is Local System
        DetailPrint "Attempting to change service account to Network Service"
        ExecWait '"$INSTDIR\nssm.exe" set JellyfinServer Objectname "Network Service"' $0
        DetailPrint "Jellyfin Server service account change, $0"
    ${EndIf}

SectionEnd

Section "-start service" StartService
${If} $_SERVICESTART_ == "YES"
${AndIf} $_INSTALLSERVICE_ == "YES"
    ExecWait '"$INSTDIR\nssm.exe" start JellyfinServer' $0
    DetailPrint "Jellyfin Server service start, $0"
${EndIf}
SectionEnd

;--------------------------------
;Descriptions

;Language strings
    LangString DESC_InstallJellyfinServer ${LANG_ENGLISH} "Install Jellyfin Server"
    LangString DESC_InstallService ${LANG_ENGLISH} "Install As a Service"

;Assign language strings to sections
    !insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
    !insertmacro MUI_DESCRIPTION_TEXT ${InstallJellyfinServer} $(DESC_InstallJellyfinServer)
    !insertmacro MUI_DESCRIPTION_TEXT ${InstallService} $(DESC_InstallService)
    !insertmacro MUI_FUNCTION_DESCRIPTION_END

;--------------------------------
;Uninstaller Section

Section "Uninstall"

    ReadRegStr $INSTDIR HKLM "Software\Jellyfin" "InstallFolder"  ; read the installation folder
    ReadRegStr $_JELLYFINDATADIR_ HKLM "Software\Jellyfin" "DataFolder"  ; read the data folder

    DetailPrint "Jellyfin Install location : $INSTDIR"
    DetailPrint "Jellyfin Data folder : $_JELLYFINDATADIR_"

    MessageBox MB_YESNO|MB_ICONINFORMATION "Do you want to retain the Jellyfin Server data folder? The media will not be touched. $\r$\nIf unsure choose YES." /SD IDYES IDYES PreserveData

    ExecWait '"$INSTDIR\nssm.exe" statuscode JellyfinServer' $0
    DetailPrint "Jellyfin Server service statuscode, $0"
    IntCmp $0 0 NoServiceUninstall ; service doesn't exist, may be run from desktop shortcut

    ExecWait '"$INSTDIR\nssm.exe" remove JellyfinServer' $0
    DetailPrint "Removing Jellyfin Server service, $0"

    Sleep 3000 ; Give time for Windows to catchup

    NoServiceUninstall: ; existing install was present but no service was detected

    RMDir /r /REBOOTOK "$_JELLYFINDATADIR_"

    PreserveData:

    DetailPrint "Attempting to stop Jellyfin Server"
    ExecWait '"$INSTDIR\nssm.exe" stop JellyfinServer' $0
    DetailPrint "Jellyfin Server service stop, $0"
    DetailPrint "Attempting to remove Jellyfin Server service"
    ExecWait '"$INSTDIR\nssm.exe" remove JellyfinServer confirm' $0
    DetailPrint "Jellyfin Server service remove, $0"

    Delete "$INSTDIR\Uninstall.exe"

    RMDir /r /REBOOTOK "$INSTDIR"

    DeleteRegKey HKLM "Software\Jellyfin"
    DeleteRegKey HKLM "${REG_UNINST_KEY}"

SectionEnd

Function .onInit
; Setting up defaults
    StrCpy $_INSTALLSERVICE_ "YES"
    StrCpy $_SERVICESTART_ "YES"
    StrCpy $_SERVICEACCOUNTTYPE_ "NetworkService"
    StrCpy $_EXISTINGINSTALLATION_ "NO"
    StrCpy $_EXISTINGSERVICE_ "NO"

    SetShellVarContext current
    StrCpy $_JELLYFINDATADIR_ "$%ProgramData%\Jellyfin\Server"

;Detect if Jellyfin is already installed.
; In case it is installed, let the user choose either
;	1. Exit installer
;   2. Upgrade without messing with data
; 		2a. Don't ask for any details, uninstall and install afresh with old settings

; Read Registry for previous installation
    ClearErrors
    ReadRegStr "$0" HKLM "${REG_CONFIG_KEY}" "InstallFolder"
    IfErrors NoExisitingInstall

    DetailPrint "Existing Jellyfin Server detected at: $0"
    StrCpy "$INSTDIR" "$0" ; set the location fro registry as new default

    StrCpy $_EXISTINGINSTALLATION_ "YES" ; Set our flag to be used later
    SectionSetText ${InstallJellyfinServer} "Upgrade Jellyfin Server (required)" ; Change install text to "Upgrade"

; check if there is a service called Jellyfin, there should be
; hack : nssm statuscode Jellyfin will return non zero return code in case it exists
    ExecWait '"$INSTDIR\nssm.exe" statuscode JellyfinServer' $0
    DetailPrint "Jellyfin Server service statuscode, $0"
    IntCmp $0 0 NoService ; service doesn't exist, may be run from desktop shortcut

    ; if service was detected, set defaults going forward.
    StrCpy $_EXISTINGSERVICE_ "YES"
    StrCpy $_INSTALLSERVICE_ "YES"
    StrCpy $_SERVICESTART_ "YES"

    ; check if service was run using Network Service account
    ClearErrors
    ReadRegStr $_SERVICEACCOUNTTYPE_ HKLM "${REG_CONFIG_KEY}" "ServiceAccountType" ; in case of error _SERVICEACCOUNTTYPE_ will be NetworkService as default

    ClearErrors
    ReadRegStr $_JELLYFINDATADIR_ HKLM "${REG_CONFIG_KEY}" "DataFolder" ; in case of error, the default holds

    ; Hide sections which will not be needed in case of previous install
    ; SectionSetText ${InstallService} ""

    NoService: ; existing install was present but no service was detected

; Let the user know that we'll upgrade and provide an option to quit.
    MessageBox MB_OKCANCEL|MB_ICONINFORMATION "Existing installation of Jellyfin Server was detected, it'll be upgraded, settings will be retained. \
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

Function HideDataDirectoryPage
    ${If} $_EXISTINGINSTALLATION_ == "YES" ; Existing installation detected, so don't ask for InstallFolder
        Abort
    ${EndIf}
FunctionEnd

Function HideServiceAccountTypePage

    ${If} $_INSTALLSERVICE_ == "NO" ; Not running as a service, don't ask for service type
    ${OrIf} $_EXISTINGINSTALLATION_ == "YES" ; Existing installation detected, so don't ask for InstallFolder
        Abort
    ${EndIf}
FunctionEnd

Function PreConfirm
    MessageBox MB_OK "PreConfirm" ; THIS RUNS BEFORE THE CRASH
FunctionEnd

; Declare temp variables to read the options from the custom page.
Var StartServiceAfterInstall
Var UseNetworkServiceAccount
Var UseLocalSystemAccount

Function ServiceAccountTypePage_Config
${NSD_GetState} $hCtl_service_account_type_StartServiceAfterInstall $StartServiceAfterInstall
${If} $StartServiceAfterInstall == 1
    StrCpy $_SERVICESTART_ "YES"
${Else}
    StrCpy $_SERVICESTART_ "NO"
${EndIf}
${NSD_GetState} $hCtl_service_account_type_UseNetworkServiceAccount $UseNetworkServiceAccount
${NSD_GetState} $hCtl_service_account_type_UseLocalSystemAccount $UseLocalSystemAccount

${If} $UseNetworkServiceAccount == 1
    StrCpy $_SERVICEACCOUNTTYPE_ "NetworkService"
${ElseIf} $UseLocalSystemAccount == 1
    StrCpy $_SERVICEACCOUNTTYPE_ "LocalSystem"
${Else}
    Abort
${EndIf}

FunctionEnd

; This function handles the choices during component selection
Function .onSelChange

; If we are not installing service, we don't need to set the NetworkService account or StartService
    SectionGetFlags ${InstallService} $0
    ${If} $0 = ${SF_SELECTED}
        StrCpy $_INSTALLSERVICE_ "YES"
    ${Else}
        StrCpy $_INSTALLSERVICE_ "NO"
    ${EndIf}
FunctionEnd

Function .onInstSuccess
    ExecShell "open" "http://localhost:8096"
FunctionEnd

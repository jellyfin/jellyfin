!verbose 3
SetCompressor /SOLID bzip2
ShowInstDetails show
ShowUninstDetails show
Unicode True

;--------------------------------
!define SF_USELECTED  0 ; used to check selected options status, rest are inherited from Sections.nsh

    !include "MUI2.nsh"
    !include "Sections.nsh"
    !include "LogicLib.nsh"

    !include "helpers\ShowError.nsh"

; Global variables that we'll use
    Var _JELLYFINVERSION_
    Var _JELLYFINDATADIR_
    Var _SETUPTYPE_
    Var _INSTALLSERVICE_
    Var _SERVICESTART_
    Var _SERVICEACCOUNTTYPE_
    Var _EXISTINGINSTALLATION_
    Var _EXISTINGSERVICE_
    Var _MAKESHORTCUTS_
    Var _FOLDEREXISTS_
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
!ifdef UXPATH
    !define MUI_ICON "${UXPATH}\branding\NSIS\modern-install.ico" ; Installer Icon
    !define MUI_UNICON "${UXPATH}\branding\NSIS\modern-install.ico" ; Uninstaller Icon

    !define MUI_HEADERIMAGE
    !define MUI_HEADERIMAGE_BITMAP "${UXPATH}\branding\NSIS\installer-header.bmp"
    !define MUI_WELCOMEFINISHPAGE_BITMAP "${UXPATH}\branding\NSIS\installer-right.bmp"
    !define MUI_UNWELCOMEFINISHPAGE_BITMAP "${UXPATH}\branding\NSIS\installer-right.bmp"
!endif

;--------------------------------
;Pages

; Welcome Page
    !define MUI_WELCOMEPAGE_TEXT "The installer will ask for details to install Jellyfin Server."
    !insertmacro MUI_PAGE_WELCOME
; License Page
    !insertmacro MUI_PAGE_LICENSE "$%InstallLocation%\LICENSE" ; picking up generic GPL

; Setup Type Page
    Page custom ShowSetupTypePage SetupTypePage_Config
    
; Components Page
    !define MUI_PAGE_CUSTOMFUNCTION_PRE HideComponentsPage
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
    !include "dialogs\setuptype.nsdinc"
    !include "dialogs\service-config.nsdinc"
    !include "dialogs\confirmation.nsdinc"

; Select service account type
    #!define MUI_PAGE_CUSTOMFUNCTION_PRE HideServiceConfigPage ; Controls when to hide / show (This does not work for Page, might need to go PageEx)
    #!define MUI_PAGE_CUSTOMFUNCTION_SHOW fnc_service_config_Show
    #!define MUI_PAGE_CUSTOMFUNCTION_LEAVE ServiceConfigPage_Config
    #!insertmacro MUI_PAGE_CUSTOM ServiceAccountType
    Page custom ShowServiceConfigPage ServiceConfigPage_Config

; Confirmation Page
    Page custom ShowConfirmationPage ; just letting the user know what they chose to install

; Actual Installion Page
    !insertmacro MUI_PAGE_INSTFILES

    !insertmacro MUI_UNPAGE_CONFIRM
    !insertmacro MUI_UNPAGE_INSTFILES
    #!insertmacro MUI_UNPAGE_FINISH

;--------------------------------
;Languages; Add more languages later here if needed
    !insertmacro MUI_LANGUAGE "English"

;--------------------------------
;Installer Sections
Section "!Jellyfin Server (required)" InstallJellyfinServer
    SectionIn RO ; Mandatory section, isn't this the whole purpose to run the installer.

    StrCmp "$_EXISTINGINSTALLATION_" "Yes" RunUninstaller CarryOn ; Silently uninstall in case of previous installation

    RunUninstaller:
    DetailPrint "Looking for uninstaller at $INSTDIR"
    FindFirst $0 $1 "$INSTDIR\Uninstall.exe"
    FindClose $0
    StrCmp $1 "" CarryOn ; the registry key was there but uninstaller was not found

    DetailPrint "Silently running the uninstaller at $INSTDIR"
    ExecWait '"$INSTDIR\Uninstall.exe" /S _?=$INSTDIR' $0
    DetailPrint "Uninstall finished, $0"

    CarryOn:
    ${If} $_EXISTINGSERVICE_ == 'Yes'
        ExecWait '"$INSTDIR\nssm.exe" stop JellyfinServer' $0
        ${If} $0 <> 0
            MessageBox MB_OK|MB_ICONSTOP "Could not stop the Jellyfin Server service."
            Abort
        ${EndIf}
        DetailPrint "Stopped Jellyfin Server service, $0"
    ${EndIf}

    SetOutPath "$INSTDIR"

    File "/oname=icon.ico" "${UXPATH}\branding\NSIS\modern-install.ico"
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
    WriteRegStr HKLM "${REG_UNINST_KEY}" "DisplayIcon" '"$INSTDIR\Uninstall.exe",0'
    WriteRegStr HKLM "${REG_UNINST_KEY}" "Publisher" "The Jellyfin Project"
    WriteRegStr HKLM "${REG_UNINST_KEY}" "URLInfoAbout" "https://jellyfin.org/"
    WriteRegStr HKLM "${REG_UNINST_KEY}" "DisplayVersion" "$_JELLYFINVERSION_"
    WriteRegDWORD HKLM "${REG_UNINST_KEY}" "NoModify" 1
    WriteRegDWORD HKLM "${REG_UNINST_KEY}" "NoRepair" 1

;Create uninstaller
    WriteUninstaller "$INSTDIR\Uninstall.exe"
SectionEnd

Section "Jellyfin Server Service" InstallService
${If} $_INSTALLSERVICE_ == "Yes" ; Only run this if we're going to install the service!
    ExecWait '"$INSTDIR\nssm.exe" statuscode JellyfinServer' $0
    DetailPrint "Jellyfin Server service statuscode, $0"
    ${If} $0 == 0
        InstallRetry:
        ExecWait '"$INSTDIR\nssm.exe" install JellyfinServer "$INSTDIR\jellyfin.exe" --service --datadir \"$_JELLYFINDATADIR_\"' $0
        ${If} $0 <> 0
            !insertmacro ShowError "Could not install the Jellyfin Server service." InstallRetry
        ${EndIf}
        DetailPrint "Jellyfin Server Service install, $0"
    ${Else}
        DetailPrint "Jellyfin Server Service exists, updating..."

        ConfigureApplicationRetry:
        ExecWait '"$INSTDIR\nssm.exe" set JellyfinServer Application "$INSTDIR\jellyfin.exe"' $0
        ${If} $0 <> 0
            !insertmacro ShowError "Could not configure the Jellyfin Server service." ConfigureApplicationRetry
        ${EndIf}
        DetailPrint "Jellyfin Server Service setting (Application), $0"

        ConfigureAppParametersRetry:
        ExecWait '"$INSTDIR\nssm.exe" set JellyfinServer AppParameters --service --datadir \"$_JELLYFINDATADIR_\"' $0
        ${If} $0 <> 0
            !insertmacro ShowError "Could not configure the Jellyfin Server service." ConfigureAppParametersRetry
        ${EndIf}
        DetailPrint "Jellyfin Server Service setting (AppParameters), $0"
    ${EndIf}


    Sleep 3000 ; Give time for Windows to catchup
    ConfigureStartRetry:
    ExecWait '"$INSTDIR\nssm.exe" set JellyfinServer Start SERVICE_DELAYED_AUTO_START' $0
    ${If} $0 <> 0
        !insertmacro ShowError "Could not configure the Jellyfin Server service." ConfigureStartRetry
    ${EndIf}
    DetailPrint "Jellyfin Server Service setting (Start), $0"

    ConfigureDescriptionRetry:
    ExecWait '"$INSTDIR\nssm.exe" set JellyfinServer Description "Jellyfin Server: The Free Software Media System"' $0
    ${If} $0 <> 0
        !insertmacro ShowError "Could not configure the Jellyfin Server service." ConfigureDescriptionRetry
    ${EndIf}
    DetailPrint "Jellyfin Server Service setting (Description), $0"
    ConfigureDisplayNameRetry:
    ExecWait '"$INSTDIR\nssm.exe" set JellyfinServer DisplayName "Jellyfin Server"' $0
    ${If} $0 <> 0
        !insertmacro ShowError "Could not configure the Jellyfin Server service." ConfigureDisplayNameRetry

    ${EndIf}
    DetailPrint "Jellyfin Server Service setting (DisplayName), $0"

    Sleep 3000
    ${If} $_SERVICEACCOUNTTYPE_ == "NetworkService" ; the default install using NSSM is Local System
        ConfigureNetworkServiceRetry:
        ExecWait '"$INSTDIR\nssm.exe" set JellyfinServer Objectname "Network Service"' $0
        ${If} $0 <> 0
            !insertmacro ShowError "Could not configure the Jellyfin Server service account." ConfigureNetworkServiceRetry
        ${EndIf}
        DetailPrint "Jellyfin Server service account change, $0"
    ${EndIf}

    Sleep 3000
    ConfigureDefaultAppExit:
        ExecWait '"$INSTDIR\nssm.exe" set JellyfinServer AppExit Default Exit' $0
        ${If} $0 <> 0
            !insertmacro ShowError "Could not configure the Jellyfin Server service app exit action." ConfigureDefaultAppExit
        ${EndIf}
        DetailPrint "Jellyfin Server service exit action set, $0"
${EndIf}

SectionEnd

Section "-start service" StartService
${If} $_SERVICESTART_ == "Yes"
${AndIf} $_INSTALLSERVICE_ == "Yes"
    StartRetry:
    ExecWait '"$INSTDIR\nssm.exe" start JellyfinServer' $0
    ${If} $0 <> 0
        !insertmacro ShowError "Could not start the Jellyfin Server service." StartRetry
    ${EndIf}
    DetailPrint "Jellyfin Server service start, $0"
${EndIf}
SectionEnd

Section "Create Shortcuts" CreateWinShortcuts
    ${If} $_MAKESHORTCUTS_ == "Yes"
        CreateDirectory "$SMPROGRAMS\Jellyfin Server"
        CreateShortCut "$SMPROGRAMS\Jellyfin Server\Jellyfin (View Console).lnk" "$INSTDIR\jellyfin.exe" "--datadir $\"$_JELLYFINDATADIR_$\"" "$INSTDIR\icon.ico" 0 SW_SHOWMAXIMIZED
        CreateShortCut "$SMPROGRAMS\Jellyfin Server\Jellyfin Tray App.lnk" "$INSTDIR\jellyfintray.exe" "" "$INSTDIR\icon.ico" 0
        ;CreateShortCut "$DESKTOP\Jellyfin Server.lnk" "$INSTDIR\jellyfin.exe" "--datadir $\"$_JELLYFINDATADIR_$\"" "$INSTDIR\icon.ico" 0 SW_SHOWMINIMIZED
        CreateShortCut "$DESKTOP\Jellyfin Server\Jellyfin Server.lnk" "$INSTDIR\jellyfintray.exe" "" "$INSTDIR\icon.ico" 0
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

    ReadRegStr $INSTDIR HKLM "${REG_CONFIG_KEY}" "InstallFolder"  ; read the installation folder
    ReadRegStr $_JELLYFINDATADIR_ HKLM "${REG_CONFIG_KEY}" "DataFolder"  ; read the data folder
    ReadRegStr $_SERVICEACCOUNTTYPE_ HKLM "${REG_CONFIG_KEY}" "ServiceAccountType"  ; read the account name

    DetailPrint "Jellyfin Install location: $INSTDIR"
    DetailPrint "Jellyfin Data folder: $_JELLYFINDATADIR_"

    MessageBox MB_YESNO|MB_ICONINFORMATION "Do you want to retain the Jellyfin Server data folder? The media will not be touched. $\r$\nIf unsure choose YES." /SD IDYES IDYES PreserveData

    RMDir /r /REBOOTOK "$_JELLYFINDATADIR_"

    PreserveData:

    ExecWait '"$INSTDIR\nssm.exe" statuscode JellyfinServer' $0
    DetailPrint "Jellyfin Server service statuscode, $0"
    IntCmp $0 0 NoServiceUninstall ; service doesn't exist, may be run from desktop shortcut

    Sleep 3000 ; Give time for Windows to catchup

    UninstallStopRetry:
    ExecWait '"$INSTDIR\nssm.exe" stop JellyfinServer' $0
    ${If} $0 <> 0
        !insertmacro ShowError "Could not stop the Jellyfin Server service." UninstallStopRetry
    ${EndIf}
    DetailPrint "Stopped Jellyfin Server service, $0"

    UninstallRemoveRetry:
    ExecWait '"$INSTDIR\nssm.exe" remove JellyfinServer confirm' $0
    ${If} $0 <> 0
        !insertmacro ShowError "Could not remove the Jellyfin Server service." UninstallRemoveRetry
    ${EndIf}
    DetailPrint "Removed Jellyfin Server service, $0"

    Sleep 3000 ; Give time for Windows to catchup

    NoServiceUninstall: ; existing install was present but no service was detected. Remove shortcuts if account is set to none
        ${If} $_SERVICEACCOUNTTYPE_ == "None"
            RMDir /r "$SMPROGRAMS\Jellyfin Server"
            Delete "$DESKTOP\Jellyfin Server.lnk"
            DetailPrint "Removed old shortcuts..."
        ${EndIf}

    Delete "$INSTDIR\*.*"
    RMDir /r /REBOOTOK "$INSTDIR\jellyfin-web"
    Delete "$INSTDIR\Uninstall.exe"
    RMDir /r /REBOOTOK "$INSTDIR"
    
    DeleteRegKey HKLM "Software\Jellyfin"
    DeleteRegKey HKLM "${REG_UNINST_KEY}"

SectionEnd

Function .onInit
; Setting up defaults
    StrCpy $_INSTALLSERVICE_ "Yes"
    StrCpy $_SERVICESTART_ "Yes"
    StrCpy $_SERVICEACCOUNTTYPE_ "NetworkService"
    StrCpy $_EXISTINGINSTALLATION_ "No"
    StrCpy $_EXISTINGSERVICE_ "No"
    StrCpy $_MAKESHORTCUTS_ "No"

    SetShellVarContext current
    StrCpy $_JELLYFINDATADIR_ "$%ProgramData%\Jellyfin\Server"

    System::Call 'kernel32::CreateMutex(p 0, i 0, t "JellyfinServerMutex") p .r1 ?e'
    Pop $R0

    StrCmp $R0 0 +3
    !insertmacro ShowErrorFinal "The installer is already running."

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

    StrCpy $_EXISTINGINSTALLATION_ "Yes" ; Set our flag to be used later
    SectionSetText ${InstallJellyfinServer} "Upgrade Jellyfin Server (required)" ; Change install text to "Upgrade"

  ; check if service was run using Network Service account
    ClearErrors
    ReadRegStr $_SERVICEACCOUNTTYPE_ HKLM "${REG_CONFIG_KEY}" "ServiceAccountType" ; in case of error _SERVICEACCOUNTTYPE_ will be NetworkService as default

    ClearErrors
    ReadRegStr $_JELLYFINDATADIR_ HKLM "${REG_CONFIG_KEY}" "DataFolder" ; in case of error, the default holds

    ; Hide sections which will not be needed in case of previous install
    ; SectionSetText ${InstallService} ""

; check if there is a service called Jellyfin, there should be
; hack : nssm statuscode Jellyfin will return non zero return code in case it exists
    ExecWait '"$INSTDIR\nssm.exe" statuscode JellyfinServer' $0
    DetailPrint "Jellyfin Server service statuscode, $0"
    IntCmp $0 0 NoService ; service doesn't exist, may be run from desktop shortcut

    ; if service was detected, set defaults going forward.
    StrCpy $_EXISTINGSERVICE_ "Yes"
    StrCpy $_INSTALLSERVICE_ "Yes"
    StrCpy $_SERVICESTART_ "Yes"
    StrCpy $_MAKESHORTCUTS_ "No"
    SectionSetText ${CreateWinShortcuts} ""

  
    NoService: ; existing install was present but no service was detected
        ${If} $_SERVICEACCOUNTTYPE_ == "None"
            StrCpy $_SETUPTYPE_ "Basic"
            StrCpy $_INSTALLSERVICE_ "No"
            StrCpy $_SERVICESTART_ "No"
            StrCpy $_MAKESHORTCUTS_ "Yes"
        ${EndIf}

; Let the user know that we'll upgrade and provide an option to quit.
    MessageBox MB_OKCANCEL|MB_ICONINFORMATION "Existing installation of Jellyfin Server was detected, it'll be upgraded, settings will be retained. \
    $\r$\nClick OK to proceed, Cancel to exit installer." /SD IDOK IDOK ProceedWithUpgrade
    Quit ; Quit if the user is not sure about upgrade

    ProceedWithUpgrade:

    NoExisitingInstall: ; by this time, the variables have been correctly set to reflect previous install details

FunctionEnd

Function HideInstallDirectoryPage
    ${If} $_EXISTINGINSTALLATION_ == "Yes" ; Existing installation detected, so don't ask for InstallFolder
        Abort
    ${EndIf}
FunctionEnd

Function HideDataDirectoryPage
    ${If} $_EXISTINGINSTALLATION_ == "Yes" ; Existing installation detected, so don't ask for InstallFolder
        Abort
    ${EndIf}
FunctionEnd

Function HideServiceConfigPage
    ${If} $_INSTALLSERVICE_ == "No" ; Not running as a service, don't ask for service type
    ${OrIf} $_EXISTINGINSTALLATION_ == "Yes" ; Existing installation detected, so don't ask for InstallFolder
        Abort
    ${EndIf}
FunctionEnd

Function HideConfirmationPage
    ${If} $_EXISTINGINSTALLATION_ == "Yes" ; Existing installation detected, so don't ask for InstallFolder
        Abort
    ${EndIf}
FunctionEnd

Function HideSetupTypePage
    ${If} $_EXISTINGINSTALLATION_ == "Yes" ; Existing installation detected, so don't ask for SetupType
        Abort
    ${EndIf}
FunctionEnd

Function HideComponentsPage
     ${If} $_SETUPTYPE_ == "Basic" ; Basic installation chosen, don't show components choice
        Abort
    ${EndIf}
FunctionEnd

; Setup Type dialog show function
Function ShowSetupTypePage
  Call HideSetupTypePage
  Call fnc_setuptype_Create
  nsDialogs::Show
FunctionEnd

; Service Config dialog show function
Function ShowServiceConfigPage
  Call HideServiceConfigPage
  Call fnc_service_config_Create
  nsDialogs::Show
FunctionEnd

; Confirmation dialog show function
Function ShowConfirmationPage
  Call HideConfirmationPage
  Call fnc_confirmation_Create
  nsDialogs::Show
FunctionEnd

; Declare temp variables to read the options from the custom page.
Var StartServiceAfterInstall
Var UseNetworkServiceAccount
Var UseLocalSystemAccount
Var BasicInstall


Function SetupTypePage_Config
${NSD_GetState} $hCtl_setuptype_BasicInstall $BasicInstall
 IfFileExists "$LOCALAPPDATA\Jellyfin" folderfound foldernotfound ; if the folder exists, use this, otherwise, go with new default
        folderfound:
            StrCpy $_FOLDEREXISTS_ "Yes"
            Goto InstallCheck
        foldernotfound:
            StrCpy $_FOLDEREXISTS_ "No"
            Goto InstallCheck

InstallCheck:
${If} $BasicInstall == 1
    StrCpy $_SETUPTYPE_ "Basic"
    StrCpy $_INSTALLSERVICE_ "No"
    StrCpy $_SERVICESTART_ "No"
    StrCpy $_SERVICEACCOUNTTYPE_ "None"
    StrCpy $_MAKESHORTCUTS_ "Yes"
    ${If} $_FOLDEREXISTS_ == "Yes"
        StrCpy $_JELLYFINDATADIR_ "$LOCALAPPDATA\Jellyfin\"
    ${EndIf}
${Else}
    StrCpy $_SETUPTYPE_ "Advanced"
    StrCpy $_INSTALLSERVICE_ "Yes"
    StrCpy $_MAKESHORTCUTS_ "No"
    ${If} $_FOLDEREXISTS_ == "Yes"
            MessageBox MB_OKCANCEL|MB_ICONINFORMATION "An existing data folder was detected.\
            $\r$\nBasic Setup is highly recommended.\
            $\r$\nIf you proceed, you will need to set up Jellyfin again." IDOK GoAhead IDCANCEL GoBack
        GoBack:
            Abort
    ${EndIf}
        GoAhead:
            StrCpy $_JELLYFINDATADIR_ "$%ProgramData%\Jellyfin\Server"
            SectionSetText ${CreateWinShortcuts} ""
${EndIf}
    
FunctionEnd

Function ServiceConfigPage_Config
${NSD_GetState} $hCtl_service_config_StartServiceAfterInstall $StartServiceAfterInstall
${If} $StartServiceAfterInstall == 1
    StrCpy $_SERVICESTART_ "Yes"
${Else}
    StrCpy $_SERVICESTART_ "No"
${EndIf}
${NSD_GetState} $hCtl_service_config_UseNetworkServiceAccount $UseNetworkServiceAccount
${NSD_GetState} $hCtl_service_config_UseLocalSystemAccount $UseLocalSystemAccount

${If} $UseNetworkServiceAccount == 1
    StrCpy $_SERVICEACCOUNTTYPE_ "NetworkService"
${ElseIf} $UseLocalSystemAccount == 1
    StrCpy $_SERVICEACCOUNTTYPE_ "LocalSystem"
${Else}
    !insertmacro ShowErrorFinal "Service account type not properly configured."
${EndIf}

FunctionEnd

; This function handles the choices during component selection
Function .onSelChange

; If we are not installing service, we don't need to set the NetworkService account or StartService
    SectionGetFlags ${InstallService} $0
    ${If} $0 = ${SF_SELECTED}
        StrCpy $_INSTALLSERVICE_ "Yes"
    ${Else}
        StrCpy $_INSTALLSERVICE_ "No"
        StrCpy $_SERVICESTART_ "No"
        StrCpy $_SERVICEACCOUNTTYPE_ "None"
    ${EndIf}
FunctionEnd

Function .onInstSuccess
    #ExecShell "open" "http://localhost:8096"
FunctionEnd

; Shows a lot of debug information while compiling
; This can be removed once stable.
!verbose 4
;--------------------------------
;Include Modern UI

    !include "MUI2.nsh"
    Var _JELLYFINVERSION_
;    Var _DEFAULTEMBYDATADIR_
    Var _JELLYFINDATADIR_
    Var _SERVICEINSTALLED_
	Var _EXISTINGINSTALLATION_
	Var _EXISTINGSERVICE_
;--------------------------------
;General

; Align installer version with jellyfin.dll version
    !getdllversion "$%InstallLocation%\jellyfin.dll" ver_
    !echo "jellyfin.dll version is ${ver_1}.${ver_2}.${ver_3}.${ver_4}" ;!echo will print it while building

    Name "Jellyfin ${ver_1}.${ver_2}.${ver_3}.${ver_4}"
    OutFile "Jellyfin.Installer.${ver_1}.${ver_2}.${ver_3}.${ver_4}.exe"
    BrandingText "Jellyfin ${ver_1}.${ver_2}.${ver_3}.${ver_4} Installer"

; installer attributes
    VIProductVersion "${ver_1}.${ver_2}.${ver_3}.${ver_4}"
    VIFileVersion "${ver_1}.${ver_2}.${ver_3}.${ver_4}"
    VIAddVersionKey "ProductName" "Jellyfin"
    VIAddVersionKey "FileVersion" "${ver_1}.${ver_2}.${ver_3}.${ver_4}"
	VIAddVersionKey "LegalCopyright" "Jellyfin, Free Software Media System"
	VIAddVersionKey "FileDescription" "Jellyfin, Free Software Media System"  
	
;Default installation folder
    InstallDir "$APPDATA\Jellyfin"
  
;Get installation folder from registry if available
    InstallDirRegKey HKLM "Software\Jellyfin" "InstallLocation"
  
;Request application privileges for service installation
    RequestExecutionLevel admin
    CRCCheck on	    
    !define MUI_ABORTWARNING

;--------------------------------
;Pages

    !insertmacro MUI_PAGE_LICENSE "$%InstallLocation%\LICENSE"
    !insertmacro MUI_PAGE_COMPONENTS
    !define MUI_PAGE_CUSTOMFUNCTION_PRE HideDirectoryPage
    !insertmacro MUI_PAGE_DIRECTORY

; Custom Directory page to ask for Emby Library location in case its needed
; Commented for now to avoid showing this. 
; This can be uncommented in case Emby Migration is planned later
;    !define MUI_PAGE_HEADER_TEXT "Emby Library locaton"
;    !define MUI_PAGE_HEADER_SUBTEXT  ""
;    !define MUI_DIRECTORYPAGE_TEXT_TOP "Please select the folder where Emby library is present. This will have Enby folders like config, cache, data, metadata, etc."
;    !define MUI_DIRECTORYPAGE_TEXT_DESTINATION "Emby Library location"
;    !define MUI_PAGE_CUSTOMFUNCTION_PRE ShowEmbyLibraryPage
;    !define MUI_DIRECTORYPAGE_VARIABLE $_DEFAULTEMBYDATADIR_
;    !insertmacro MUI_PAGE_DIRECTORY
	
    !insertmacro MUI_PAGE_INSTFILES

    !insertmacro MUI_UNPAGE_CONFIRM
    !insertmacro MUI_UNPAGE_INSTFILES
  
;--------------------------------
;Languages
 
    !insertmacro MUI_LANGUAGE "English"

;--------------------------------
;Installer Sections

Section "Install Jellyfin (required)" InstallJellyfin
	StrCmp $_EXISTINGINSTALLATION_ "YES" CheckService CarryOn
	
	CheckService:
	StrCmp $_EXISTINGSERVICE_ "YES" StopService ExistingInstallButNotService
	
	StopService: ; we stop the service to copy files in use
    ExecWait '"$INSTDIR\nssm.exe" stop Jellyfin' $0
    DetailPrint "Jellyfin service stop, $0"
	
	ExistingInstallButNotService:
;TODO, find a way to kill the process in case it was started as standalone
	MessageBox MB_OK|MB_ICONINFORMATION "Please stop Jellyfin manually before proceeding further." 	
	
	CarryOn:
    SetOutPath "$INSTDIR"
    SetShellVarContext current
    StrCpy $_JELLYFINDATADIR_ "$LOCALAPPDATA\jellyfin\"

; Pack all the files that were just compiled
    File /r $%InstallLocation%\* 
	
; Write the installation path into the registry
    WriteRegStr HKLM "Software\Jellyfin" "InstallLocation" "$INSTDIR"

; Write the uninstall keys for Windows
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "DisplayName" "Jellyfin $_JELLYFINVERSION_"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "UninstallString" '"$INSTDIR\Uninstall.exe"'
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "DisplayIcon" '"$INSTDIR\Jellyfin.exe",0'
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "Publisher" "The Jellyfin project"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "URLInfoAbout" "https://jellyfin.github.io/"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "DisplayVersion" "$_JELLYFINVERSION_"
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "NoModify" 1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "NoRepair" 1

;Create uninstaller
    WriteUninstaller "$INSTDIR\Uninstall.exe"

SectionEnd

Section /o "Jellyfin desktop shortcut" DesktopShortcut
    SetShellVarContext current
    DetailPrint "Creating desktop shortcut"
    CreateShortCut "$DESKTOP\Jellyfin.lnk" "$INSTDIR\jellyfin.exe"
SectionEnd

Section /o "Jellyfin Service" InstallService
    ExecWait '"$INSTDIR\nssm.exe" install Jellyfin "$INSTDIR\jellyfin.exe" --datadir "$_JELLYFINDATADIR_"' $0
    DetailPrint "Jellyfin Service install, $0"
    Sleep 3000
    ExecWait '"$INSTDIR\nssm.exe" set Jellyfin Start SERVICE_DELAYED_AUTO_START' $0
    DetailPrint "Jellyfin Service setting, $0"
    StrCpy $_SERVICEINSTALLED_ "YES"
SectionEnd

Section /o "Start Jellyfin after installation" LaunchJellyfin
; either start the service or launch jellyfin standalone
    StrCmp $_SERVICEINSTALLED_ "YES" ServiceStart Standalone
    
    ServiceStart:
    ExecWait '"$INSTDIR\nssm.exe" start Jellyfin' $0
    DetailPrint "Jellyfin service start, $0"
    Return
    
    Standalone:
    ExecWait '"$INSTDIR\jellyfin.exe"' $0
    DetailPrint "$INSTDIR\jellyfin.exe start, $0"
	Return

SectionEnd

; This can be uncommented in case Emby Migration is planned later
;Section /o "Migrate Emby Library" MigrateEmbyLibrary
;    DetailPrint "Migrating Emby Library"
;    CopyFiles $_DEFAULTEMBYDATADIR_/config $_JELLYFINDATADIR_
;    CopyFiles $_DEFAULTEMBYDATADIR_/cache $_JELLYFINDATADIR_
;    CopyFiles $_DEFAULTEMBYDATADIR_/data $_JELLYFINDATADIR_
;    CopyFiles $_DEFAULTEMBYDATADIR_/metadata $_JELLYFINDATADIR_
;    CopyFiles $_DEFAULTEMBYDATADIR_/root $_JELLYFINDATADIR_

;SectionEnd


;--------------------------------
;Descriptions

  ;Language strings
    LangString DESC_InstallJellyfin ${LANG_ENGLISH} "Install Jellyfin"
    LangString DESC_InstallService ${LANG_ENGLISH} "Install As a Service"
    LangString DESC_DesktopShortcut ${LANG_ENGLISH} "Create a desktop shortcut"
    LangString DESC_LaunchJellyfin ${LANG_ENGLISH} "Start Jellyfin after Install"
;    LangString DESC_MigrateEmbyLibrary ${LANG_ENGLISH} "Migrate existing Emby Library"

  ;Assign language strings to sections
    !insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
    !insertmacro MUI_DESCRIPTION_TEXT ${InstallJellyfin} $(DESC_InstallJellyfin)
    !insertmacro MUI_DESCRIPTION_TEXT ${DesktopShortcut} $(DESC_DesktopShortcut)
    !insertmacro MUI_DESCRIPTION_TEXT ${InstallService} $(DESC_InstallService)
    !insertmacro MUI_DESCRIPTION_TEXT ${LaunchJellyfin} $(DESC_LaunchJellyfin)
;    !insertmacro MUI_DESCRIPTION_TEXT ${MigrateEmbyLibrary} $(DESC_MigrateEmbyLibrary)
    !insertmacro MUI_FUNCTION_DESCRIPTION_END

;--------------------------------
;Uninstaller Section

Section "Uninstall"
    SetShellVarContext current
    StrCpy $_JELLYFINDATADIR_ "$LOCALAPPDATA\jellyfin\"
	
; Currently we try to stop & remove a running service even if it doesn't exist
; not really sure about nssm statuscode detection method
; nothing to loose with brute force stop & remove method
    ExecWait '"$INSTDIR\nssm.exe" stop Jellyfin' $0
    DetailPrint "Jellyfin service stop, $0"
    ExecWait '"$INSTDIR\nssm.exe" remove Jellyfin confirm' $0
    DetailPrint "Jellyfin Service remove, $0"

    Delete "$INSTDIR\Uninstall.exe"
	
;TODO
; stop running instance gracefully, in case its running, the /REBOOTOK flag will delete it on reboot.
    RMDir /r /REBOOTOK "$INSTDIR" ;
	
	MessageBox MB_YESNO|MB_ICONINFORMATION "Do you want to retain Jellyfin settings ? The media will not be touched in any case." /SD IDYES IDYES PreserveData
    RMDir /r /REBOOTOK "$_JELLYFINDATADIR_"

	PreserveData:
	
    DeleteRegKey HKLM "Software\Jellyfin"
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin"
    Delete "$DESKTOP\Jellyfin.lnk"

SectionEnd


Function .onInit
	StrCpy $_EXISTINGINSTALLATION_ "NO"
	StrCpy $_EXISTINGSERVICE_ "NO"
;Detect if Jellyfin is already installed.
; In case it is installed, let the user choose either
;	1. Exit installer
;   2. Upgrade without messing with data 
; 		2a. Don't ask for any installation folder
; 		2b. If there is a service, don't ask for service installation or launch
; 		2c. If no service, ask for autolaunch, and start as standalone

; Read Registry for installation
	ClearErrors
	ReadRegStr "$0" HKLM "Software\Jellyfin" "InstallLocation"
	IfErrors NoExisitingInstall
	
	DetailPrint "Existing Jellyfin detected at: $0"
	StrCpy "$INSTDIR" "$0"
	StrCpy $_EXISTINGINSTALLATION_ "YES"	
	SectionSetText ${InstallJellyfin} "Upgrade Jellyfin" ; Change install text to "Upgrade"

; check if there is a service called Jellyfin
; hack : nssm statuscode Jellyfin will return non zero return code in case it exists
    ExecWait '"$INSTDIR\nssm.exe" statuscode Jellyfin' $0
    DetailPrint "Jellyfin service statuscode, $0"
	IntCmp $0 0 NoService ; service doesn't exist 
	
	StrCpy $_EXISTINGSERVICE_ "YES"
    SectionSetText ${InstallService} "" ; hide service install option if old install was a service
	SectionSetText ${LaunchJellyfin} "" ; hide service start option if old install was a service 
	SectionSetText ${DesktopShortcut} "" ; hide desktop shortcut option too
 
	NoService:	
; if detected, let the user know that we'll upgrade and its ok to quit
	MessageBox MB_OKCANCEL|MB_ICONINFORMATION "Existing installation of Jellyfin was detected, it'll be upgraded, settings will be retained" /SD IDOK IDOK Proceed
	Quit ; Quit if the user is not sure about upgrade

	Proceed:

	NoExisitingInstall:
	
    SetShellVarContext all
; Align installer version with jellyfin.dll version
    !getdllversion "$%InstallLocation%\jellyfin.dll" ver_
    StrCpy $_JELLYFINVERSION_ "${ver_1}.${ver_2}.${ver_3}.${ver_4}"
    SetShellVarContext current
    StrCpy $_JELLYFINDATADIR_ "$LOCALAPPDATA\jellyfin\"
	DetailPrint "_JELLYFINDATADIR_ : $_JELLYFINDATADIR_"
    StrCpy $_SERVICEINSTALLED_ "NO"
    SectionSetFlags ${InstallJellyfin} 17 ; this makes the InstallJellyfin section mandatory
	
FunctionEnd

Function HideDirectoryPage
    StrCmp $_EXISTINGINSTALLATION_ "NO" show 
	
    Abort ; Dont show folder selection if just upgrading

    show: 
FunctionEnd

; This can be uncommented in case Emby Migration is planned later
;Function ShowEmbyLibraryPage
;    SectionGetFlags ${MigrateEmbyLibrary} $R0 
;    IntOp $R0 $R0 & ${SF_SELECTED} 
;    IntCmp $R0 ${SF_SELECTED} show 
    
;    Abort ; Dont show the Emby folder selection window if Emby migrartion is not selected

;    show: 
;FunctionEnd

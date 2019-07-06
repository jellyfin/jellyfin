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
  
  ;Default installation folder
    InstallDir "$APPDATA\Jellyfin"
  
  ;Get installation folder from registry if available
    InstallDirRegKey HKLM "Software\Jellyfin" "InstallLocation"
  
  ;Request application privileges for Windows Vista
    RequestExecutionLevel admin
    CRCCheck on	    
    !define MUI_ABORTWARNING

;--------------------------------
;Pages

    !insertmacro MUI_PAGE_LICENSE "$%InstallLocation%\LICENSE"
    !insertmacro MUI_PAGE_COMPONENTS
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

Section "Jellyfin desktop shortcut" DesktopShortcut
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
    ExecWait '"$INSTDIR"\jellyfin.exe' $0
    DetailPrint "$INSTDIR\jellyfin.exe start, $0"

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
;TODO
; stop running instance
    ExecWait '"$INSTDIR\nssm.exe" stop Jellyfin' $0
    DetailPrint "Jellyfin service stop, $0"
    ExecWait '"$INSTDIR\nssm.exe" remove Jellyfin confirm' $0
    DetailPrint "Jellyfin Service remove, $0"

    Delete "$INSTDIR\Uninstall.exe"
    RMDir /r "$INSTDIR"
    RMDir /r "$_JELLYFINDATADIR_"
    DeleteRegKey HKLM "Software\Jellyfin"
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin"
    Delete "$DESKTOP\Jellyfin.lnk"

SectionEnd


Function .onInit
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

; This can be uncommented in case Emby Migration is planned later
;Function ShowEmbyLibraryPage
;    SectionGetFlags ${MigrateEmbyLibrary} $R0 
;    IntOp $R0 $R0 & ${SF_SELECTED} 
;    IntCmp $R0 ${SF_SELECTED} show 
    
;    Abort ; Dont show the Emby folder selection window if Emby migrartion is not selected

;    show: 
;FunctionEnd

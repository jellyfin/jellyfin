!verbose 4
;--------------------------------
;Include Modern UI

    !include "MUI2.nsh"
    Var JellyfinVersion
    Var defaultEmbyDataDir
    Var JELLYFINDATADIR
    Var ServiceInstalled
;--------------------------------
;General

  ;Name and file
    !getdllversion "$%InstallLocation%\jellyfin.dll" expv_
    !echo "jellyfin.dll version is ${expv_1}.${expv_2}.${expv_3}.${expv_4}"
    Name "Jellyfin ${expv_1}.${expv_2}.${expv_3}.${expv_4}"
    OutFile "Jellyfin.Installer.${expv_1}.${expv_2}.${expv_3}.${expv_4}.exe"
    BrandingText "Jellyfin ${expv_1}.${expv_2}.${expv_3}.${expv_4} Installer"
    VIProductVersion "${expv_1}.${expv_2}.${expv_3}.${expv_4}"
    VIFileVersion "${expv_1}.${expv_2}.${expv_3}.${expv_4}"
    VIAddVersionKey "ProductName" "Jellyfin"
    VIAddVersionKey "FileVersion" "${expv_1}.${expv_2}.${expv_3}.${expv_4}"
  
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

;    !insertmacro MUI_PAGE_LICENSE "${NSISDIR}\Docs\Modern UI\License.txt"
    !insertmacro MUI_PAGE_COMPONENTS
    !insertmacro MUI_PAGE_DIRECTORY

    !define MUI_PAGE_HEADER_TEXT "MUI_PAGE_HEADER_TEXT"
    !define MUI_PAGE_HEADER_SUBTEXT  "MUI_PAGE_HEADER_SUBTEXT"
    !define MUI_DIRECTORYPAGE_TEXT_TOP "MUI_DIRECTORYPAGE_TEXT_TOP"  
    !define MUI_DIRECTORYPAGE_TEXT_DESTINATION "APP Folder"
    !define MUI_PAGE_CUSTOMFUNCTION_PRE ShowEmbyLibraryPage
    !define MUI_DIRECTORYPAGE_VARIABLE $defaultEmbyDataDir
    !insertmacro MUI_PAGE_DIRECTORY


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
;Create uninstaller

    File /r $%InstallLocation%\* 
; Write the installation path into the registry
    WriteRegStr HKLM "Software\Jellyfin" "InstallLocation" "$INSTDIR"

; Write the uninstall keys for Windows
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "DisplayName" "Jellyfin $JellyfinVersion"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "UninstallString" '"$INSTDIR\Uninstall.exe"'
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "DisplayIcon" '"$INSTDIR\Jellyfin.exe",0'
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "Publisher" "The Jellyfin project"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "URLInfoAbout" "https://jellyfin.github.io/"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "DisplayVersion" "$JellyfinVersion"
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "NoModify" 1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin" "NoRepair" 1
    WriteUninstaller "$INSTDIR\Uninstall.exe"

SectionEnd


Section "Jellyfin Service" InstallService
    ExecWait '"$INSTDIR"\nssm.exe install Jellyfin "$INSTDIR"\jellyfin.exe --datadir "$JELLYFINDATADIR"' $0
    MessageBox MB_OK "Service install Error : $0"
    Sleep 3000
    ExecWait '"$INSTDIR"\nssm.exe set Jellyfin Start SERVICE_DELAYED_AUTO_START' $0
    MessageBox MB_OK "Service setting Error : $0"
    StrCpy $ServiceInstalled "YES"
SectionEnd

Section "Desktop shortcut" DesktopShortcut
    SetShellVarContext current
    CreateShortCut "$DESKTOP\Jellyfin.lnk" "$INSTDIR\jellyfin.exe"
SectionEnd

;TODO
Section "Launch Jellyfin" LaunchJellyfin
    !echo "Binaries at : $%InstallLocation%"
; either start the service or launch jellyfin standalone
    StrCmp $ServiceInstalled "YES" ServiceStart Standalone
    
    ServiceStart:
    ExecWait 'C:\Windows\System32\sc.exe start Jellyfin' $0
    MessageBox MB_OK "Service start Error : $0"
    Return
    
    Standalone:
    ExecWait '"$INSTDIR"\jellyfin.exe' $0
    MessageBox MB_OK "start Error : $0"

SectionEnd

;TODO
Section "Migrate Emby Library" MigrateEmbyLibrary

    CopyFiles $defaultEmbyDataDir/config $JELLYFINDATADIR
    CopyFiles $defaultEmbyDataDir/cache $JELLYFINDATADIR
    CopyFiles $defaultEmbyDataDir/data $JELLYFINDATADIR
    CopyFiles $defaultEmbyDataDir/metadata $JELLYFINDATADIR
    CopyFiles $defaultEmbyDataDir/root $JELLYFINDATADIR

SectionEnd


;--------------------------------
;Descriptions

  ;Language strings
    LangString DESC_InstallJellyfin ${LANG_ENGLISH} "Install Jellyfin"
    LangString DESC_InstallService ${LANG_ENGLISH} "Install As a Service"
    LangString DESC_DesktopShortcut ${LANG_ENGLISH} "Create a desktop shortcut"
    LangString DESC_LaunchJellyfin ${LANG_ENGLISH} "Start Jellyfin after Install"
    LangString DESC_MigrateEmbyLibrary ${LANG_ENGLISH} "Migrate existing Emby Library"

  ;Assign language strings to sections
    !insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
    !insertmacro MUI_DESCRIPTION_TEXT ${InstallJellyfin} $(DESC_InstallJellyfin)
    !insertmacro MUI_DESCRIPTION_TEXT ${InstallService} $(DESC_InstallService)
    !insertmacro MUI_DESCRIPTION_TEXT ${LaunchJellyfin} $(DESC_LaunchJellyfin)
    !insertmacro MUI_DESCRIPTION_TEXT ${MigrateEmbyLibrary} $(DESC_MigrateEmbyLibrary)
    !insertmacro MUI_FUNCTION_DESCRIPTION_END

;--------------------------------
;Uninstaller Section

Section "Uninstall"


;TODO
; stop service or running instance
    MessageBox MB_OK "uninstall $INSTDIR, $JELLYFINDATADIR"

    Delete "$INSTDIR\Uninstall.exe"
    RMDir /r "$INSTDIR"
    RMDir /r "$JELLYFINDATADIR"
    DeleteRegKey HKLM "Software\Jellyfin"
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Jellyfin"
    Delete "$DESKTOP\Jellyfin.lnk"

SectionEnd


Function .onInit
    SetShellVarContext all
    !getdllversion "$%InstallLocation%\jellyfin.dll" expv_
    StrCpy $JellyfinVersion "${expv_1}.${expv_2}.${expv_3}.${expv_4}"
    StrCpy $JELLYFINDATADIR "$LOCALAPPDATA\jellyfin\"    
    StrCpy $ServiceInstalled "NO"
    SectionSetFlags ${InstallJellyfin} 17
FunctionEnd

Function ShowEmbyLibraryPage
    SectionGetFlags ${MigrateEmbyLibrary} $R0 
    IntOp $R0 $R0 & ${SF_SELECTED} 
    IntCmp $R0 ${SF_SELECTED} show 
    
    Abort ; Dont show the Emby folder selection window if Emby migrartion is not selected

    show: 
FunctionEnd

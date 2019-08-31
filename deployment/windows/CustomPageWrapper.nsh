/*

NSIS Modern User Interface
Custom page

*/

;--------------------------------


;--------------------------------
;Page declaration

!macro MUI_PAGEDECLARATION_CUSTOM NAME

  #!insertmacro MUI_SET MUI_${MUI_PAGE_UNINSTALLER_PREFIX}CUSTOMPAGE ""

  #!insertmacro MUI_DEFAULT MUI_CUSTOMPAGE_TEXT_TOP ""
  #!insertmacro MUI_DEFAULT MUI_CUSTOMPAGE_TEXT_DESTINATION ""

  PageEx ${MUI_PAGE_UNINSTALLER_FUNCPREFIX}directory ; SHOULD BE custom, but then the PageCallback breaks.

    PageCallbacks ${MUI_PAGE_UNINSTALLER_FUNCPREFIX}mui.Custom${NAME}Pre_${MUI_UNIQUEID} ${MUI_PAGE_UNINSTALLER_FUNCPREFIX}mui.Custom${NAME}Show_${MUI_UNIQUEID} ${MUI_PAGE_UNINSTALLER_FUNCPREFIX}mui.Custom${NAME}Leave_${MUI_UNIQUEID}

    Caption " "

  PageExEnd

  !insertmacro MUI_FUNCTION_CUSTOMPAGE ${MUI_PAGE_UNINSTALLER_FUNCPREFIX}mui.Custom${NAME}Pre_${MUI_UNIQUEID} ${MUI_PAGE_UNINSTALLER_FUNCPREFIX}mui.Custom${NAME}Show_${MUI_UNIQUEID} ${MUI_PAGE_UNINSTALLER_FUNCPREFIX}mui.Custom${NAME}Leave_${MUI_UNIQUEID}

  #!undef MUI_CUSTOMPAGE_TEXT_TOP
  #!undef MUI_CUSTOMPAGE_TEXT_DESTINATION
  #!insertmacro MUI_UNSET MUI_CUSTOMPAGE_VARIABLE
  #!insertmacro MUI_UNSET MUI_CUSTOMPAGE_VERIFYONLEAVE

!macroend

!macro MUI_PAGE_CUSTOM NAME

  !verbose push
  !verbose ${MUI_VERBOSE}

  !insertmacro MUI_PAGE_INIT
  !insertmacro MUI_PAGEDECLARATION_CUSTOM ${NAME}

  !verbose pop

!macroend

;--------------------------------
;Page functions

!macro MUI_FUNCTION_CUSTOMPAGE PRE SHOW LEAVE

  Function "${PRE}"
    !insertmacro MUI_PAGE_FUNCTION_CUSTOM PRE
  FunctionEnd

  Function "${SHOW}"
    !insertmacro MUI_PAGE_FUNCTION_CUSTOM SHOW
  FunctionEnd

  Function "${LEAVE}"
    !insertmacro MUI_PAGE_FUNCTION_CUSTOM LEAVE
  FunctionEnd

!macroend

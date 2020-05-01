; Show error
!macro ShowError TEXT RETRYLABEL
  MessageBox MB_ABORTRETRYIGNORE|MB_ICONSTOP "${TEXT}" IDIGNORE +2 IDRETRY ${RETRYLABEL}
  Abort
!macroend

!macro ShowErrorFinal TEXT
  MessageBox MB_OK|MB_ICONSTOP "${TEXT}"
  Abort
!macroend

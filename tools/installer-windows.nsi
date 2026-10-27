Unicode true
!include MUI2.nsh
!include LogicLib.nsh
!include FileFunc.nsh
Name "IOI Desk"
OutFile "${OUTPUT}"
InstallDir "$LOCALAPPDATA\Programs\IOI Desk"
InstallDirRegKey HKCU "Software\IOI\IOI Desk" "InstallDir"
RequestExecutionLevel user
SetCompressor /SOLID lzma
!define MUI_ABORTWARNING
!define MUI_ICON "${PAYLOAD}\assets\IOIDesk.ico"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${PAYLOAD}\LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\IOIDesk.exe"
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "PortugueseBR"
Var WaitCount
Function .onInit
  ; WinSparkle starts the installer, then requests an orderly IOI Desk exit.
  StrCpy $WaitCount 0
  wait_for_app:
    System::Call 'kernel32::OpenMutexW(i 0x100000, i 0, w "Local\IOI.Desk.Workspace") p.r0'
    ${If} $0 != 0
      System::Call 'kernel32::CloseHandle(p r0)'
      IntOp $WaitCount $WaitCount + 1
      ${If} $WaitCount < 40
        Sleep 500
        Goto wait_for_app
      ${EndIf}
      MessageBox MB_OK|MB_ICONEXCLAMATION "Encerre o IOI Desk antes de instalar. Seus outros aplicativos e sessÃµes podem continuar abertos."
      Abort
    ${EndIf}
FunctionEnd
Function .onInstSuccess
  IfSilent 0 no_restart
  ${GetParameters} $0
  ClearErrors
  ${GetOptions} $0 "/NORESTART" $1
  IfErrors 0 no_restart
  ExecShell "open" "$INSTDIR\IOIDesk.exe"
  no_restart:
FunctionEnd
Section "IOI Desk"
  SetOutPath "$INSTDIR"
  File /r "${PAYLOAD}\*"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "Software\IOI\IOI Desk" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\IOIDesk" "DisplayName" "IOI Desk"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\IOIDesk" "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\IOIDesk" "Publisher" "IOI"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\IOIDesk" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\IOIDesk" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\IOIDesk" "NoRepair" 1
  CreateShortcut "$SMPROGRAMS\IOI Desk.lnk" "$INSTDIR\IOIDesk.exe"
  CreateShortcut "$DESKTOP\IOI Desk.lnk" "$INSTDIR\IOIDesk.exe"
SectionEnd
Section "Uninstall"
  ; Application data lives outside this directory and is deliberately preserved.
  Delete "$DESKTOP\IOI Desk.lnk"
  Delete "$SMPROGRAMS\IOI Desk.lnk"
  !include "${DELETE_MANIFEST}"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\IOIDesk"
  DeleteRegValue HKCU "Software\IOI\IOI Desk" "InstallDir"
SectionEnd

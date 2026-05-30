@echo off
title XboxChargeMon Build
color 0A

echo.
echo  XboxChargeMon - Build
echo  =====================
echo.

set "SRC=XboxChargeMon.cs"
set "OUT=XboxChargeMon.exe"
set "CSC="

for %%v in (v4.0.30319) do (
    if exist "%SystemRoot%\Microsoft.NET\Framework64\%%v\csc.exe" (
        set "CSC=%SystemRoot%\Microsoft.NET\Framework64\%%v\csc.exe"
        goto :build
    )
    if exist "%SystemRoot%\Microsoft.NET\Framework\%%v\csc.exe" (
        set "CSC=%SystemRoot%\Microsoft.NET\Framework\%%v\csc.exe"
        goto :build
    )
)

echo  [ERROR] csc.exe not found.
echo  .NET Framework 4.x is required. It ships with Windows 10 and 11.
echo.
pause
exit /b 1

:build
echo  Compiler : %CSC%
echo  Output   : %OUT%
echo.

"%CSC%" /nologo /target:winexe /optimize+ /out:"%OUT%" ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  /resource:"icons\disconnected.png",disconnected.png ^
  /resource:"icons\full.png",full.png ^
  /resource:"icons\medium.png",medium.png ^
  /resource:"icons\low.png",low.png ^
  /resource:"icons\wired.png",wired.png ^
  "%SRC%"

if %errorlevel% equ 0 (
    echo.
    echo  [OK] Build successful.
    echo  XboxChargeMon.exe is self-contained -- icons are embedded.
    echo  Run it and the app will appear in the system tray.
    echo.
    start "" "%OUT%"
) else (
    echo.
    echo  [FAIL] Build failed. See errors above.
    echo.
    pause
)

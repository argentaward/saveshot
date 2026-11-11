@echo off
setlocal
echo ===================================
echo   Compiling Saveshot for Windows
echo ===================================

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo Error: C# compiler not found at %CSC%
    pause
    exit /b 1
)

"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /win32icon:app.ico /win32manifest:app.manifest /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll /out:Saveshot.exe src\*.cs

if %ERRORLEVEL% equ 0 (
    echo.
    echo [SUCCESS] Saveshot.exe built successfully!
    echo File size:
    for %%I in (Saveshot.exe) do echo %%~zI bytes
) else (
    echo.
    echo [ERROR] Build failed!
)
endlocal

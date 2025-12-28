@echo off
title Build Portable CattosTracker
color 0E
cls

echo ================================================================================
echo                  BUILD PORTABLE VERSION - CATTOS TRACKER
echo ================================================================================
echo.

cd /d "%~dp0Source\CattosTracker"

echo Cleaning previous builds...
rmdir /S /Q "bin\Release\net8.0\win-x64\publish" 2>nul
echo.

echo Building portable Windows x64 version...
echo This will include all .NET dependencies...
echo.

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

if %ERRORLEVEL% EQ 0 (
    echo.
    echo ================================================================================
    echo                         BUILD SUCCESSFUL!
    echo ================================================================================
    echo.
    echo Portable version created at:
    echo %~dp0Source\CattosTracker\bin\Release\net8.0\win-x64\publish\
    echo.
    echo Files:
    echo - CattosTracker.exe (standalone executable)
    echo - config.json (settings - will be created on first run)
    echo.
    echo You can copy this folder anywhere and run without .NET installed!
    echo.
) else (
    echo.
    echo ================================================================================
    echo                         BUILD FAILED!
    echo ================================================================================
    echo.
    echo Please ensure .NET 8 SDK is installed.
    echo.
)

pause
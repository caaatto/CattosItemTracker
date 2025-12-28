@echo off
title Catto's Item Tracker
color 0A
cls

echo ================================================================================
echo                        🐱 CATTO'S ITEM TRACKER 🐱
echo ================================================================================
echo.

cd /d "%~dp0"

REM Quick check if everything is ready
echo Checking setup...

if not exist "G:\World of Warcraft\_classic_era_\WowClassic.exe" (
    echo [!] WoW Classic Era not found at expected location
    echo.
)

if not exist "G:\World of Warcraft\_classic_era_\WTF\Account\126652900#2\SavedVariables\CattosItemTracker.lua" (
    echo [!] No SavedVariables found yet
    echo.
    echo Please do in WoW first:
    echo   1. Log in with your character
    echo   2. Type: /catto check
    echo   3. Type: /reload
    echo.
)

if not exist "Source\CattosTracker\bin\Debug\net8.0\CattosTracker.exe" (
    echo Building application...
    cd "Source\CattosTracker"
    dotnet build -c Debug >nul 2>&1
    cd "%~dp0"
)

echo Starting Catto's Item Tracker...
echo.

"Source\CattosTracker\bin\Debug\net8.0\CattosTracker.exe"

pause
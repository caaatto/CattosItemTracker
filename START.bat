@echo off
title CattosTracker - WoW Equipment Monitor
color 0A
cls

echo ================================================================================
echo                      CATTOS ITEM TRACKER v1.2.0
echo                   WoW Classic Equipment Monitor with API
echo ================================================================================
echo.

cd /d "%~dp0Source\CattosTracker"

echo Starting CattosTracker...
echo.
dotnet run

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ================================================================================
    echo ERROR: Application failed to start
    echo ================================================================================
    echo.
    echo Please ensure:
    echo - .NET 8 SDK is installed
    echo - All project files are present
    echo.
    pause
)
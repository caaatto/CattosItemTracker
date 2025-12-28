@echo off
echo ===================================
echo Building CattosItemTracker...
echo ===================================
echo.
echo Cleaning old build...
rmdir /S /Q AUTO 2>nul
echo.
echo Building Release version...
dotnet publish Source\CattosTracker\CattosTracker.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=false -o AUTO
echo.
echo ===================================
echo Build complete! Run START_CATTOS.bat to start the app.
echo ===================================
pause
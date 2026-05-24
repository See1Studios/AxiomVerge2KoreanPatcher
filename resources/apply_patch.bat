@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ============================================================
echo Axiom Verge 2 Korean Patch - Windows One-Click Installer
echo ============================================================
echo.

set "SCRIPT_DIR=%~dp0"
set "CLI_TOOL=%SCRIPT_DIR%AV2Patcher.CLI.exe"
set "CONTENT_ZIP=%SCRIPT_DIR%Content.zip"
set "FONTS_DIR=%SCRIPT_DIR%Fonts"

:: ── 1. Check prerequisites ──────────────────────────────────
if not exist "%CLI_TOOL%" (
    echo [ERROR] AV2Patcher.CLI.exe not found.
    echo Please make sure all files are extracted in the same folder.
    goto ERROR_EXIT
)

if not exist "%CONTENT_ZIP%" (
    echo [ERROR] Content.zip not found.
    echo Please make sure Content.zip is in the same folder as this script.
    goto ERROR_EXIT
)

:: ── 2. Detect game directory ────────────────────────────────
set "GAME_DIR="

:: Case A: Script is placed directly in the game folder
if exist "%SCRIPT_DIR%AxiomVerge2.exe" (
    set "GAME_DIR=%SCRIPT_DIR%"
    echo [INFO] Detected game in the current folder.
)

:: Case B: Detect from Registry & Default Steam paths
if not defined GAME_DIR (
    :: Try to read Steam InstallPath from registry
    for /f "tokens=2*" %%A in ('reg query "HKCU\Software\Valve\Steam" /v "SteamPath" 2^>nul') do set "STEAM_PATH=%%B"
    if not defined STEAM_PATH (
        for /f "tokens=2*" %%A in ('reg query "HKLM\SOFTWARE\WOW6432Node\Valve\Steam" /v "InstallPath" 2^>nul') do set "STEAM_PATH=%%B"
    )
    
    if defined STEAM_PATH (
        :: Replace slashes in registry path
        set "STEAM_PATH=!STEAM_PATH:/=\!"
        set "CHECK_PATH=!STEAM_PATH!\steamapps\common\Axiom Verge 2\"
        if exist "!CHECK_PATH!AxiomVerge2.exe" (
            set "GAME_DIR=!CHECK_PATH!"
            echo [INFO] Detected Steam installation path: !GAME_DIR!
        )
    )
)

:: Case C: Check common drive letters
if not defined GAME_DIR (
    for %%D in (C D E F G H) do (
        set "CHECK_PATH=%%D:\SteamLibrary\steamapps\common\Axiom Verge 2\"
        if exist "!CHECK_PATH!AxiomVerge2.exe" (
            set "GAME_DIR=!CHECK_PATH!"
            echo [INFO] Detected game path on drive %%D: !GAME_DIR!
        )
    )
)

:: Case D: Fallback to manual entry or drag-and-drop
if not defined GAME_DIR (
    echo [WARNING] Axiom Verge 2 installation directory could not be auto-detected.
    echo.
    echo Please drag and drop the "AxiomVerge2.exe" file onto this window,
    echo or type the full path of the game directory and press Enter:
    set /p "USER_INPUT="
    
    :: Remove quotes if drag-and-dropped
    set "USER_INPUT=!USER_INPUT:"=!"
    
    if exist "!USER_INPUT!" (
        if exist "!USER_INPUT!\AxiomVerge2.exe" (
            set "GAME_DIR=!USER_INPUT!\"
        ) else if /i "%%~nxI"=="AxiomVerge2.exe" (
            for %%I in ("!USER_INPUT!") do set "GAME_DIR=%%~dpI"
        ) else (
            for %%I in ("!USER_INPUT!\AxiomVerge2.exe") do set "GAME_DIR=%%~dpI"
        )
    )
)

if not defined GAME_DIR (
    echo [ERROR] Invalid game directory specified.
    goto ERROR_EXIT
)

if not exist "%GAME_DIR%AxiomVerge2.exe" (
    echo [ERROR] AxiomVerge2.exe not found in: %GAME_DIR%
    goto ERROR_EXIT
)

echo.
echo Target Game Directory: %GAME_DIR%
echo.

:: ── 3. Step 1: Copy Fonts ───────────────────────────────────
set "GAME_FONTS_DIR=%GAME_DIR%Content\Fonts"
if exist "%FONTS_DIR%" (
    if exist "%GAME_FONTS_DIR%" (
        echo [1/2] Applying font patch...
        for %%F in ("%FONTS_DIR%"\*.xnb) do (
            set "fname=%%~nxF"
            :: Backup original (first time only)
            if not exist "%GAME_FONTS_DIR%\!fname!.original" (
                copy "%GAME_FONTS_DIR%\!fname!" "%GAME_FONTS_DIR%\!fname!.original" >nul 2>&1
            )
            copy "%%F" "%GAME_FONTS_DIR%\!fname!" >nul
            echo   ✓ !fname!
        )
    ) else (
        echo [1/2] Warning: Game Fonts directory not found at: %GAME_FONTS_DIR%
    )
) else (
    echo [1/2] Warning: Source Fonts directory not found. Skipping font copying.
)

:: ── 4. Step 2: Inject Content.zip ───────────────────────────
echo.
echo [2/2] Injecting translation resources...
"%CLI_TOOL%" "%GAME_DIR%AxiomVerge2.exe" "%CONTENT_ZIP%"
if errorlevel 1 (
    echo [ERROR] Failed to inject resources into AxiomVerge2.exe.
    goto ERROR_EXIT
)

echo.
echo ============================================================
echo SUCCESS: Axiom Verge 2 Korean Patch has been applied!
echo ============================================================
echo.
pause
exit /b 0

:ERROR_EXIT
echo.
echo [FAILED] Patch application aborted due to errors.
echo.
pause
exit /b 1

@echo off
setlocal
cd /d "%~dp0"
set "GIT_PATH=%LOCALAPPDATA%\MinGit\cmd\git.exe"

if not exist "%GIT_PATH%" (
    where git >nul 2>nul
    if %errorlevel% equ 0 (
        set "GIT_PATH=git"
    ) else (
        echo [ERROR] Git is not found!
        pause
        exit /b 1
    )
)

echo ========================================================
echo        MultiWall FX - Push to GitHub (v1te0k)
echo ========================================================
echo.
echo Make sure you have created the empty repository on GitHub:
echo https://github.com/new with name: MultiWallFX
echo.
"%GIT_PATH%" remote set-url origin https://github.com/v1te0k/MultiWallFX.git
"%GIT_PATH%" push -u origin main

if %errorlevel% equ 0 (
    echo.
    echo [SUCCESS] Repository successfully pushed to https://github.com/v1te0k/MultiWallFX!
) else (
    echo.
    echo [INFO] If it asked for credentials, use your GitHub username and a Personal Access Token (PAT).
)
echo.
pause

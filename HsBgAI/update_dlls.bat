@echo off
chcp 65001 >nul 2>&1
setlocal enabledelayedexpansion

set "HS_DIR=D:\Program Files\Hearthstone\Hearthstone_Data\Managed"
set "LIB_DIR=%~dp0LibHearthstone"

if not exist "%HS_DIR%" (
    echo [ERROR] Hearthstone dir not found: %HS_DIR%
    pause
    exit /b 1
)

echo Sync Hearthstone DLLs to LibHearthstone...
echo.

set "SKIP_LIST=UniTask.dll UniTask.Linq.dll QRCoderUnity.dll"

set COUNT=0
set MISSING=0
set SKIPPED=0

for %%F in ("%LIB_DIR%\*.dll") do (
    set "name=%%~nxF"
    set "do_skip=0"
    for %%X in (%SKIP_LIST%) do (
        if /i "!name!"=="%%X" set "do_skip=1"
    )
    if "!do_skip!"=="1" (
        set /a SKIPPED+=1
    ) else if exist "%HS_DIR%\!name!" (
        echo [CP] !name!
        copy /y "%HS_DIR%\!name!" "%LIB_DIR%\" >nul
        set /a COUNT+=1
    ) else (
        echo [MISS] !name!
        set /a MISSING+=1
    )
)

echo.
echo Done: Copied %COUNT%, Missing %MISSING%
pause

@echo off
setlocal enabledelayedexpansion
REM ---------------------------------------------------------------------------
REM  Install Bloodlines into an existing GTA V folder.
REM
REM  Safe to re-run: the DLL and the campaign data are always replaced, but your
REM  own files are never touched once they exist -- Bloodlines.ini keeps your
REM  keybinds, Bloodlines.Locations.ini keeps your surveyed coordinates, and
REM  savegame.json keeps your campaign progress.
REM
REM  Usage:  install-bloodlines.bat "C:\Program Files\Rockstar Games\Grand Theft Auto V"
REM ---------------------------------------------------------------------------

set "GAME=%~1"
if "%GAME%"=="" set "GAME=%BLOODLINES_GTA_PATH%"
if "%GAME%"=="" (
  echo Usage: install-bloodlines.bat "path\to\Grand Theft Auto V"
  echo    or set BLOODLINES_GTA_PATH once and just run this file.
  exit /b 1
)

if not exist "%GAME%\GTA5.exe" (
  echo [X] No GTA5.exe in "%GAME%" -- wrong folder?
  exit /b 1
)

REM The package is built by: python3 tools/package.py --build
set "SRC=%~dp0..\..\build\deploy\scripts"
if not exist "%SRC%\Bloodlines.dll" (
  echo [X] build\deploy\scripts\Bloodlines.dll not found.
  echo     Run:  python tools\package.py --build
  exit /b 1
)

echo.
echo === Prerequisites ===
if exist "%GAME%\ScriptHookV.dll" (echo  [ok] ScriptHookV) else (echo  [X] ScriptHookV.dll missing -- the mod cannot load)
if exist "%GAME%\ScriptHookVDotNet3.dll" (echo  [ok] ScriptHookVDotNet 3) else (echo  [X] ScriptHookVDotNet3.dll missing -- required, v3.6 or newer)
if exist "%GAME%\dinput8.dll" (echo  [ok] ASI loader) else (echo  [!] dinput8.dll missing -- ScriptHookV usually installs it)

echo.
echo === Installing ===
if not exist "%GAME%\scripts" mkdir "%GAME%\scripts"
if not exist "%GAME%\scripts\Bloodlines" mkdir "%GAME%\scripts\Bloodlines"
if not exist "%GAME%\scripts\Bloodlines\data" mkdir "%GAME%\scripts\Bloodlines\data"
if not exist "%GAME%\scripts\Bloodlines\audio" mkdir "%GAME%\scripts\Bloodlines\audio"
if not exist "%GAME%\scripts\Bloodlines\missions" mkdir "%GAME%\scripts\Bloodlines\missions"

REM Code and campaign data: always replaced.
copy /Y "%SRC%\Bloodlines.dll" "%GAME%\scripts\" >nul && echo  [ok] Bloodlines.dll
copy /Y "%SRC%\Bloodlines\data\*.tsv" "%GAME%\scripts\Bloodlines\data\" >nul && echo  [ok] campaign data
copy /Y "%SRC%\Bloodlines\data\*.json" "%GAME%\scripts\Bloodlines\data\" >nul 2>&1

REM Your files: only placed the first time.
for %%F in (Bloodlines.ini Bloodlines.Locations.ini) do (
  if exist "%GAME%\scripts\Bloodlines\%%F" (
    echo  [keep] %%F ^(yours, left alone^)
  ) else (
    copy /Y "%SRC%\Bloodlines\%%F" "%GAME%\scripts\Bloodlines\" >nul && echo  [ok] %%F ^(fresh^)
  )
)

echo.
echo Done. Launch in STORY MODE only.
echo Turn on the dev menu: scripts\Bloodlines\Bloodlines.ini  ->  [Dev] Enabled = True
endlocal

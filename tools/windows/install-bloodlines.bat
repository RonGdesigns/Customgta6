@echo off
setlocal enabledelayedexpansion
REM ---------------------------------------------------------------------------
REM  Install Bloodlines into an existing GTA V folder.
REM
REM  Works against either PC build:
REM    Legacy   -> GTA5.exe
REM    Enhanced -> GTA5_Enhanced.exe
REM  Both are accepted; the script tells you which one it found, because the
REM  script hook you need is build-specific and they are NOT interchangeable.
REM
REM  Safe to re-run: the DLL and the campaign data are always replaced, but your
REM  own files are never touched once they exist -- Bloodlines.ini keeps your
REM  keybinds, Bloodlines.Locations.ini keeps your surveyed coordinates, and
REM  savegame.json keeps your campaign progress.
REM
REM  Paths are expanded with !GAME! rather than %GAME% wherever they sit inside a
REM  parenthesised block: "C:\Program Files (x86)\..." would otherwise close the
REM  block early, quotes and all.
REM
REM  Usage:  install-bloodlines.bat "C:\Program Files\Rockstar Games\Grand Theft Auto V Enhanced"
REM ---------------------------------------------------------------------------

set "GAME=%~1"
if "%GAME%"=="" set "GAME=%BLOODLINES_GTA_PATH%"
if "%GAME%"=="" (
  echo Usage: install-bloodlines.bat "path\to\Grand Theft Auto V"
  echo    or set BLOODLINES_GTA_PATH once and just run this file.
  exit /b 1
)

REM Strip a trailing backslash so "C:\path\" does not become "C:\path\\GTA5.exe".
if "%GAME:~-1%"=="\" set "GAME=%GAME:~0,-1%"

set "BUILD="
if exist "%GAME%\GTA5_Enhanced.exe" set "BUILD=Enhanced"
if exist "%GAME%\GTA5.exe" if not defined BUILD set "BUILD=Legacy"

if not defined BUILD (
  echo [X] Neither GTA5.exe nor GTA5_Enhanced.exe is in:
  echo     !GAME!
  echo.
  echo     That is not a GTA V install root. The .exe files it does hold:
  dir /b "!GAME!\*.exe" 2>nul
  exit /b 1
)

echo.
echo === Game ===
echo  build : %BUILD%
echo  folder: %GAME%

REM The package is laid out by: python tools\package.py
REM (--build only if you have the .NET SDK; otherwise prebuilt\Bloodlines.dll is used)
set "SRC=%~dp0..\..\build\deploy\scripts"
if not exist "%SRC%\Bloodlines.dll" (
  echo.
  echo [X] build\deploy\scripts\Bloodlines.dll not found.
  echo     Run this first, from the repo root:  python tools\package.py
  exit /b 1
)

echo.
echo === Prerequisites ===
if exist "%GAME%\ScriptHookV.dll" (
  echo  [ok] ScriptHookV.dll
) else (
  echo  [X] ScriptHookV.dll missing -- the mod cannot load.
  echo       It must be the %BUILD% build of ScriptHookV specifically.
)
if exist "%GAME%\ScriptHookVDotNet.asi" (
  echo  [ok] ScriptHookVDotNet.asi ^(the part that actually runs .NET scripts^)
) else (
  echo  [X] ScriptHookVDotNet.asi missing -- no .NET script will ever be loaded.
)
if exist "%GAME%\ScriptHookVDotNet3.dll" (
  echo  [ok] ScriptHookVDotNet3.dll ^(v3 API, 3.6 or newer^)
) else (
  echo  [X] ScriptHookVDotNet3.dll missing -- required, v3.6 or newer.
)
if exist "%GAME%\dinput8.dll" (
  echo  [ok] dinput8.dll ASI loader
) else (
  echo  [!] dinput8.dll missing -- ScriptHookV usually installs it
)

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
  if exist "!GAME!\scripts\Bloodlines\%%F" (
    echo  [keep] %%F ^(yours, left alone^)
  ) else (
    copy /Y "%SRC%\Bloodlines\%%F" "!GAME!\scripts\Bloodlines\" >nul && echo  [ok] %%F ^(fresh^)
  )
)

echo.
echo Done. Launch in STORY MODE only.
echo Turn on the dev menu: scripts\Bloodlines\Bloodlines.ini  -^>  [Dev] Enabled = True
endlocal

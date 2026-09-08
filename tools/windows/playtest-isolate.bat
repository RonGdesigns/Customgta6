@echo off
setlocal enabledelayedexpansion
REM ---------------------------------------------------------------------------
REM  Park every OTHER script mod so a playtest is unambiguous.
REM
REM  With a trainer and half a dozen script mods loaded, a bug could belong to
REM  any of them -- and several of them use the same keys Bloodlines does. This
REM  moves everything except Bloodlines into scripts\_parked, runs your test,
REM  and puts it all back on the second run.
REM
REM  Paths use !VAR! inside parenthesised blocks: a "(x86)" in the path would
REM  otherwise close the block early.
REM
REM  Usage:  playtest-isolate.bat "path\to\Grand Theft Auto V"          (park)
REM          playtest-isolate.bat "path\to\Grand Theft Auto V" restore  (restore)
REM ---------------------------------------------------------------------------

set "GAME=%~1"
if "%GAME%"=="" set "GAME=%BLOODLINES_GTA_PATH%"
if "%GAME%"=="" (echo Usage: playtest-isolate.bat "path\to\Grand Theft Auto V" [restore] & exit /b 1)
if "%GAME:~-1%"=="\" set "GAME=%GAME:~0,-1%"

if not exist "%GAME%\GTA5.exe" if not exist "%GAME%\GTA5_Enhanced.exe" (
  echo [X] !GAME! is not a GTA V install root ^(no GTA5.exe / GTA5_Enhanced.exe^).
  exit /b 1
)

set "SCRIPTS=%GAME%\scripts"
set "PARKED=%SCRIPTS%\_parked"
set /a COUNT=0

if /i "%~2"=="restore" (
  if not exist "%PARKED%" (echo Nothing parked. & exit /b 0)
  for %%F in ("!PARKED!\*") do (
    move /Y "%%F" "!SCRIPTS!\" >nul
    set /a COUNT+=1
    echo  restored %%~nxF
  )
  rmdir "!PARKED!" 2>nul
  echo !COUNT! script^(s^) restored.
  exit /b 0
)

if not exist "%SCRIPTS%" (
  echo [X] !SCRIPTS! does not exist.
  echo     Nothing is parked because no scripts folder is here at all -- which
  echo     also means no .NET script mod is currently loading from this folder.
  echo     Check that this is the install your existing mods actually run in.
  exit /b 1
)

if not exist "%PARKED%" mkdir "%PARKED%"
for %%F in ("!SCRIPTS!\*.dll" "!SCRIPTS!\*.cs" "!SCRIPTS!\*.vb" "!SCRIPTS!\*.asi") do (
  if /i not "%%~nxF"=="Bloodlines.dll" (
    move /Y "%%F" "!PARKED!\" >nul
    set /a COUNT+=1
    echo  parked %%~nxF
  )
)

if !COUNT!==0 (
  rmdir "!PARKED!" 2>nul
  echo  nothing to park -- !SCRIPTS! holds no other .dll/.cs/.vb/.asi scripts.
  echo  If you expected your other mods here, they live somewhere else: this is
  echo  probably not the install your .bat setup launches.
) else (
  echo.
  echo !COUNT! other script^(s^) parked. Only Bloodlines loads now.
)
echo.
echo When you are done:  playtest-isolate.bat "%GAME%" restore
endlocal

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
REM  Usage:  playtest-isolate.bat "path\to\Grand Theft Auto V"          (park)
REM          playtest-isolate.bat "path\to\Grand Theft Auto V" restore  (restore)
REM ---------------------------------------------------------------------------

set "GAME=%~1"
if "%GAME%"=="" set "GAME=%BLOODLINES_GTA_PATH%"
if "%GAME%"=="" (echo Usage: playtest-isolate.bat "path\to\Grand Theft Auto V" [restore] & exit /b 1)

set "SCRIPTS=%GAME%\scripts"
set "PARKED=%SCRIPTS%\_parked"

if /i "%~2"=="restore" (
  if not exist "%PARKED%" (echo Nothing parked. & exit /b 0)
  for %%F in ("%PARKED%\*") do (
    move /Y "%%F" "%SCRIPTS%\" >nul
    echo  restored %%~nxF
  )
  rmdir "%PARKED%" 2>nul
  echo All other scripts restored.
  exit /b 0
)

if not exist "%PARKED%" mkdir "%PARKED%"
for %%F in ("%SCRIPTS%\*.dll" "%SCRIPTS%\*.cs" "%SCRIPTS%\*.vb" "%SCRIPTS%\*.asi") do (
  if /i not "%%~nxF"=="Bloodlines.dll" (
    move /Y "%%F" "%PARKED%\" >nul
    echo  parked %%~nxF
  )
)

echo.
echo Only Bloodlines is loaded now. Run your playtest, then:
echo   playtest-isolate.bat "%GAME%" restore
endlocal

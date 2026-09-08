@echo off
setlocal enabledelayedexpansion
REM ---------------------------------------------------------------------------
REM  Turn Bloodlines on or off without touching the rest of your mod setup.
REM
REM  Renames the DLL only, so your existing "mods on / mods off" batch file for
REM  going Online keeps working exactly as it does now. This is for A/B testing
REM  Bloodlines against your other scripts, not for Online safety -- for Online,
REM  keep using whatever already disables ScriptHookV.
REM
REM  Usage:  bloodlines-toggle.bat "path\to\Grand Theft Auto V"
REM ---------------------------------------------------------------------------

REM Keep the window open when this is launched by double-click instead of from a
REM terminal -- otherwise the output flashes past and closes. %cmdcmdline% holds
REM this script's own name only in the double-click case.
set "PAUSE_AT_END="
echo(%cmdcmdline% | find /i "%~nx0" >nul && set "PAUSE_AT_END=1"

set "GAME=%~1"
if "%GAME%"=="" set "GAME=%BLOODLINES_GTA_PATH%"
if "%GAME%"=="" (echo Usage: bloodlines-toggle.bat "path\to\Grand Theft Auto V" & goto :bail)
if "%GAME:~-1%"=="\" set "GAME=%GAME:~0,-1%"

if exist "%GAME%\scripts\Bloodlines.dll" (
  ren "!GAME!\scripts\Bloodlines.dll" "Bloodlines.dll.off"
  echo Bloodlines is now OFF.
) else if exist "%GAME%\scripts\Bloodlines.dll.off" (
  ren "!GAME!\scripts\Bloodlines.dll.off" "Bloodlines.dll"
  echo Bloodlines is now ON.
) else (
  echo Bloodlines is not installed in "!GAME!\scripts".
  echo Install it first:  install-bloodlines.bat "path\to\Grand Theft Auto V"
)
if defined PAUSE_AT_END pause
endlocal
exit /b 0

:bail
if defined PAUSE_AT_END pause
endlocal
exit /b 1

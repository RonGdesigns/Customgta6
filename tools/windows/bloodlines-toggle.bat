@echo off
setlocal
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

set "GAME=%~1"
if "%GAME%"=="" set "GAME=%BLOODLINES_GTA_PATH%"
if "%GAME%"=="" (echo Usage: bloodlines-toggle.bat "path\to\Grand Theft Auto V" & exit /b 1)

if exist "%GAME%\scripts\Bloodlines.dll" (
  ren "%GAME%\scripts\Bloodlines.dll" "Bloodlines.dll.off"
  echo Bloodlines is now OFF.
) else if exist "%GAME%\scripts\Bloodlines.dll.off" (
  ren "%GAME%\scripts\Bloodlines.dll.off" "Bloodlines.dll"
  echo Bloodlines is now ON.
) else (
  echo Bloodlines is not installed in "%GAME%\scripts".
)
endlocal

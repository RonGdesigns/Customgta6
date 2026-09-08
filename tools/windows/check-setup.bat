@echo off
setlocal enabledelayedexpansion
REM ---------------------------------------------------------------------------
REM  Report exactly what is in a GTA V folder. Read-only -- changes nothing.
REM
REM  Run this before install-bloodlines.bat when anything is unclear: which PC
REM  build it is, whether a script hook is present, and what other mods live
REM  there. Paste the output back and there is nothing left to guess about.
REM
REM  Usage:  check-setup.bat "C:\Program Files\Rockstar Games\Grand Theft Auto V Enhanced"
REM          check-setup.bat                (scans the usual install locations)
REM ---------------------------------------------------------------------------

if not "%~1"=="" (
  call :report "%~1"
  goto :done
)

echo No path given -- scanning the usual locations.
for %%R in (
  "C:\Program Files\Rockstar Games\Grand Theft Auto V"
  "C:\Program Files\Rockstar Games\Grand Theft Auto V Enhanced"
  "C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V"
  "C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V Enhanced"
  "C:\Program Files\Epic Games\GTAV"
) do (
  if exist "%%~R\GTA5.exe" call :report "%%~R"
  if exist "%%~R\GTA5_Enhanced.exe" call :report "%%~R"
)
goto :done

REM ---------------------------------------------------------------------------
:report
set "G=%~1"
if "%G:~-1%"=="\" set "G=%G:~0,-1%"
echo.
echo ===========================================================================
echo  %G%
echo ===========================================================================

set "BUILD=not a GTA V root"
if exist "%G%\GTA5.exe" set "BUILD=Legacy (GTA5.exe)"
if exist "%G%\GTA5_Enhanced.exe" set "BUILD=Enhanced (GTA5_Enhanced.exe)"
if exist "%G%\GTA5.exe" if exist "%G%\GTA5_Enhanced.exe" set "BUILD=BOTH executables present"
echo  build          : %BUILD%

call :flag "%G%\ScriptHookV.dll"          "ScriptHookV          "
call :flag "%G%\ScriptHookVDotNet.asi"    "ScriptHookVDotNet.asi"
call :flag "%G%\ScriptHookVDotNet2.dll"   "SHVDN v2 API         "
call :flag "%G%\ScriptHookVDotNet3.dll"   "SHVDN v3 API         "
call :flag "%G%\dinput8.dll"              "dinput8 ASI loader   "
call :flag "%G%\OpenIV.asi"               "OpenIV.asi           "
call :flag "%G%\version.dll"              "version.dll          "
call :flag "%G%\scripts"                  "scripts\ folder      "
call :flag "%G%\mods"                     "mods\ folder         "
call :flag "%G%\update\update.rpf"        "update.rpf           "

echo.
echo  --- .exe in the root ---
dir /b "%G%\*.exe" 2>nul
echo.
echo  --- .asi in the root ---
dir /b "%G%\*.asi" 2>nul || echo  (none)
echo.
echo  --- scripts\ contents ---
if exist "%G%\scripts" (
  dir /b "!G!\scripts" 2>nul || echo  ^(empty^)
) else (
  echo  ^(no scripts folder^)
)
echo.
echo  --- ScriptHookV log ---
if exist "%G%\ScriptHookV.log" (
  echo  ScriptHookV.log last modified:
  for %%F in ("!G!\ScriptHookV.log") do echo    %%~tF   %%~zF bytes
) else (
  echo  no ScriptHookV.log -- ScriptHookV has never run from this folder
)
if exist "%G%\ScriptHookVDotNet.log" (
  for %%F in ("!G!\ScriptHookVDotNet.log") do echo  ScriptHookVDotNet.log: %%~tF   %%~zF bytes
)
if exist "%G%\Bloodlines.log" (
  for %%F in ("!G!\Bloodlines.log") do echo  Bloodlines.log: %%~tF   %%~zF bytes
)
exit /b 0

:flag
if exist "%~1" (echo  %~2: yes) else (echo  %~2: NO)
exit /b 0

:done
echo.
echo Read-only scan complete. Nothing was changed.
endlocal

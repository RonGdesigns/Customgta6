# prebuilt/

`Bloodlines.dll` compiled for .NET Framework 4.8, committed so the mod can be
installed on a machine with **no .NET SDK**.

Why it exists: `package.py --build` needs the .NET SDK. Installing a 900 MB SDK
just to play the mod is a bad trade, so the CI-built DLL lives here and
`package.py` falls back to it automatically.

    python tools\package.py                        # uses this DLL
    python tools\package.py --build                # rebuilds from source instead

The DLL takes priority in this order:

1. `src/Bloodlines/bin/Release/Bloodlines.dll` — a local build, if you have one
2. `prebuilt/Bloodlines.dll` — this file

So a developer with the SDK never accidentally installs a stale prebuilt binary,
and a player without it never has to install one.

**Refresh it after changing C# source:**

    dotnet build src/Bloodlines/Bloodlines.csproj -c Release
    cp src/Bloodlines/bin/Release/Bloodlines.dll prebuilt/Bloodlines.dll

Compiled against `ScriptHookVDotNet3` 3.6.0 as a compile-time-only reference —
it contains no Rockstar or ScriptHookV code.

# lib/

Empty on purpose.

The build pulls `ScriptHookVDotNet3` from NuGet as a **compile-time reference only**,
so nothing needs to be dropped in here for `dotnet build` to work.

If you would rather build against the exact SHVDN DLL you have installed (recommended
once you are testing against a specific game build), copy `ScriptHookVDotNet3.dll` from
your GTA V folder into this directory and swap the `PackageReference` in
`src/Bloodlines/Bloodlines.csproj` for the commented-out `Reference` form.

Do not commit the DLL — it is not ours to redistribute, and `.gitignore` already
excludes `lib/*.dll`.

"""Build Bloodlines.dll on Windows without the .NET SDK.

    python3 tools/build_roslyn.py

`dotnet build` is still the documented path and the one CI runs. This is for the
machine the mod is actually played on, which frequently has Visual Studio Build
Tools (the C++ workload pulls them in, so does anything that touches MSBuild) but
no .NET SDK -- and installing a ~900 MB SDK to change one line of a script mod is
a bad trade. Without this, that machine can edit the C# and never see the result.

It compiles the same sources against the same pinned references `dotnet build`
would use, with the same warnings-as-errors, straight through Roslyn's csc:

  * the ScriptHookVDotNet3 version is read out of Bloodlines.csproj, never
    hardcoded here, so the two cannot drift apart;
  * both reference packages are fetched from nuget.org once and cached under
    build/refs/, which is gitignored -- nothing Rockstar or Microsoft owns is
    committed;
  * output goes to src/Bloodlines/bin/Release/Bloodlines.dll, which is where
    tools/package.py already looks first.

The build is deterministic, so the same sources always produce the same bytes
and `cmp` against prebuilt/Bloodlines.dll is a real staleness check. It will not
match an SDK build byte for byte -- that is a different compiler build -- but it
is the same assembly against the same API. Refresh prebuilt/Bloodlines.dll from
it exactly as you would from a `dotnet build`.
"""

import io
import os
import re
import subprocess
import sys
import urllib.request
import zipfile

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECT = os.path.join(REPO, 'src', 'Bloodlines', 'Bloodlines.csproj')
SOURCE_DIR = os.path.join(REPO, 'src')
REFS = os.path.join(REPO, 'build', 'refs')
OUT_DIR = os.path.join(REPO, 'src', 'Bloodlines', 'bin', 'Release')

NUGET = 'https://www.nuget.org/api/v2/package/{}/{}'
REFERENCE_ASSEMBLIES = ('Microsoft.NETFramework.ReferenceAssemblies.net48', '1.0.3')

# Two files ship in the reference-assembly package that are not managed assemblies
# at all; csc refuses the whole build over them rather than skipping them.
NOT_ASSEMBLIES = ('System.EnterpriseServices.Wrapper.dll',
                  'System.EnterpriseServices.Thunk.dll')

# Where Build Tools and full Visual Studio both put Roslyn, newest first. vswhere
# is asked before any of these; this is the fallback when it is missing.
CSC_GLOBS = [
    r'C:\Program Files (x86)\Microsoft Visual Studio\*\*\MSBuild\Current\Bin\Roslyn\csc.exe',
    r'C:\Program Files\Microsoft Visual Studio\*\*\MSBuild\Current\Bin\Roslyn\csc.exe',
]


def read_project():
    """Pull the pinned SHVDN version and language version out of the csproj.

    Reading them rather than repeating them is the whole point: a build that
    silently targets a different ScriptHookVDotNet than the project declares
    produces a DLL that loads and then misbehaves in ways no log explains.
    """
    text = io.open(PROJECT, encoding='utf-8').read()

    shvdn = re.search(r'Include="ScriptHookVDotNet3"\s+Version="([^"]+)"', text)
    if not shvdn:
        raise SystemExit('could not find the ScriptHookVDotNet3 PackageReference in ' + PROJECT)

    language = re.search(r'<LangVersion>([^<]+)</LangVersion>', text)
    assembly = re.search(r'<AssemblyName>([^<]+)</AssemblyName>', text)
    version = re.search(r'<AssemblyVersion>([^<]+)</AssemblyVersion>', text)

    return {
        'shvdn': shvdn.group(1),
        'langversion': language.group(1) if language else 'latest',
        'assembly': assembly.group(1) if assembly else 'Bloodlines',
        'version': version.group(1) if version else '0.0.0.0',
    }


def fetch_package(name, version):
    """Download and unpack one NuGet package into build/refs/, once."""
    target = os.path.join(REFS, '{}.{}'.format(name, version))
    marker = os.path.join(target, '.unpacked')
    if os.path.exists(marker):
        return target

    os.makedirs(REFS, exist_ok=True)
    archive = target + '.nupkg'
    url = NUGET.format(name, version)
    print('fetching {} {}...'.format(name, version))
    try:
        with urllib.request.urlopen(url, timeout=180) as response:
            data = response.read()
    except Exception as error:            # noqa: BLE001 - the message is the point
        raise SystemExit('could not download {}: {}\n'
                         'Fetch it by hand from {} and unzip it into {}'
                         .format(name, error, url, target))

    with io.open(archive, 'wb') as handle:
        handle.write(data)
    with zipfile.ZipFile(archive) as bundle:
        bundle.extractall(target)
    os.remove(archive)

    io.open(marker, 'w', encoding='utf-8').write(version + '\n')
    return target


def find_csc():
    """Locate Roslyn's csc.exe, preferring whatever vswhere reports."""
    vswhere = os.path.join(os.environ.get('ProgramFiles(x86)', r'C:\Program Files (x86)'),
                           'Microsoft Visual Studio', 'Installer', 'vswhere.exe')
    if os.path.exists(vswhere):
        result = subprocess.run(
            [vswhere, '-latest', '-products', '*', '-prerelease', '-property', 'installationPath'],
            capture_output=True, text=True)
        root = result.stdout.strip().splitlines()
        if root:
            candidate = os.path.join(root[0], 'MSBuild', 'Current', 'Bin', 'Roslyn', 'csc.exe')
            if os.path.exists(candidate):
                return candidate

    import glob
    for pattern in CSC_GLOBS:
        matches = sorted(glob.glob(pattern), reverse=True)
        if matches:
            return matches[0]

    raise SystemExit(
        'Roslyn csc.exe not found.\n'
        'Install Visual Studio Build Tools (any workload that includes MSBuild), or\n'
        'install the .NET SDK and use the documented build instead:\n'
        '    dotnet build src/Bloodlines/Bloodlines.csproj -c Release')


def collect_sources():
    sources = []
    for base, _, files in os.walk(SOURCE_DIR):
        if os.sep + 'obj' in base or os.sep + 'bin' in base:
            continue
        for name in sorted(files):
            if name.endswith('.cs'):
                sources.append(os.path.join(base, name))
    if not sources:
        raise SystemExit('no .cs files under ' + SOURCE_DIR)
    return sorted(sources)


def write_assembly_info(project, path):
    """MSBuild generates these attributes; csc on its own does not."""
    io.open(path, 'w', encoding='utf-8', newline='\r\n').write(
        'using System.Reflection;\n'
        '[assembly: AssemblyCompany("Los Santos: Bloodlines")]\n'
        '[assembly: AssemblyProduct("Los Santos: Bloodlines")]\n'
        '[assembly: AssemblyTitle("{name}")]\n'
        '[assembly: AssemblyVersion("{version}")]\n'
        '[assembly: AssemblyFileVersion("{version}")]\n'
        .format(name=project['assembly'], version=project['version']))


def main():
    if os.name != 'nt':
        raise SystemExit('This is the Windows no-SDK fallback. On Linux or macOS use:\n'
                         '    dotnet build src/Bloodlines/Bloodlines.csproj -c Release')

    project = read_project()
    csc = find_csc()
    print('compiler: ' + csc)

    framework = os.path.join(fetch_package(*REFERENCE_ASSEMBLIES),
                             'build', '.NETFramework', 'v4.8')
    shvdn = os.path.join(fetch_package('ScriptHookVDotNet3', project['shvdn']),
                         'lib', 'net48', 'ScriptHookVDotNet3.dll')
    if not os.path.exists(shvdn):
        raise SystemExit('ScriptHookVDotNet3 {} has no lib/net48 assembly'.format(project['shvdn']))

    os.makedirs(OUT_DIR, exist_ok=True)
    output = os.path.join(OUT_DIR, project['assembly'] + '.dll')
    generated = os.path.join(REFS, 'AssemblyInfo.generated.cs')
    write_assembly_info(project, generated)

    arguments = [
        '/nostdlib+', '/target:library', '/platform:x64',
        '/langversion:' + project['langversion'],
        '/optimize+', '/debug:portable', '/warnaserror+', '/utf8output', '/nologo',
        # Same sources in, same bytes out. Without this every rebuild differs in
        # its timestamp and MVID, and there is no way to tell a stale installed
        # DLL from a current one by comparing it against the repo -- which is
        # exactly the check worth having.
        '/deterministic+',
        '/pathmap:"{}=/_/Bloodlines"'.format(REPO),
        '/out:"{}"'.format(output),
    ]
    for name in sorted(os.listdir(framework)):
        if name.endswith('.dll') and name not in NOT_ASSEMBLIES:
            arguments.append('/reference:"{}"'.format(os.path.join(framework, name)))
    arguments.append('/reference:"{}"'.format(shvdn))
    arguments.extend('"{}"'.format(source) for source in collect_sources())
    arguments.append('"{}"'.format(generated))

    # csc takes its arguments through a response file: the full reference and
    # source list is well past the command-line length limit on Windows.
    response = os.path.join(REFS, 'build.rsp')
    io.open(response, 'w', encoding='utf-8', newline='\r\n').write('\n'.join(arguments) + '\n')

    print('compiling {} sources against ScriptHookVDotNet3 {}...'
          .format(len(collect_sources()), project['shvdn']))
    # /noconfig has to be a real argument: csc ignores it, with a warning, when it
    # arrives inside a response file, and then quietly reads csc.rsp anyway.
    result = subprocess.run([csc, '/noconfig', '@' + response])
    if result.returncode != 0:
        raise SystemExit('build failed')

    print('wrote {} ({:,} bytes)'.format(os.path.relpath(output, REPO), os.path.getsize(output)))
    print('\nRefresh the committed binary with:')
    print('    cp {} prebuilt/Bloodlines.dll'.format(os.path.relpath(output, REPO).replace('\\', '/')))
    return 0


if __name__ == '__main__':
    sys.exit(main())

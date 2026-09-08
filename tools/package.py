"""Build the mod and lay it out exactly as it installs into GTA V.

Produces build/deploy/, which mirrors the game root. Copy its contents over your
Grand Theft Auto V folder and the mod is installed — no hand-placing of files, no
guessing which ini goes where.

    python3 tools/package.py                       # uses prebuilt/Bloodlines.dll
    python3 tools/package.py --build               # rebuilds first (needs the SDK)
    python3 tools/package.py --audio build/audio   # include a generated voice pack

Nothing Rockstar owns is touched: the DLC asset pack folder is staged as loose
files for OpenIV to import, never as a modified game archive.
"""

import argparse
import io
import os
import shutil
import subprocess
import sys

# Every generated file in this repo is committed with CRLF, because they were
# all first written on Windows. Writing them with the platform default instead
# turns one regeneration on Linux into a whole-file diff on every line, which
# hides the handful of rows that actually changed. Pinning it makes the output
# the same artifact wherever the tool runs.
CRLF = '\r\n'

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECT = os.path.join(REPO, 'src', 'Bloodlines', 'Bloodlines.csproj')
BINARY = os.path.join(REPO, 'src', 'Bloodlines', 'bin', 'Release', 'Bloodlines.dll')
PREBUILT = os.path.join(REPO, 'prebuilt', 'Bloodlines.dll')
DEPLOY = os.path.join(REPO, 'build', 'deploy')

DATA_FILES = ['missions.tsv', 'dialogue.tsv', 'anchors.tsv', 'locations.tsv',
              'campaign_registry.json']
CONFIG_FILES = ['Bloodlines.ini', 'Bloodlines.Locations.ini']


def build():
    # Visual Studio users can skip --build entirely; DOTNET lets CI point at an SDK
    # that is not on PATH.
    dotnet = os.environ.get('DOTNET') or shutil.which('dotnet')
    if not dotnet:
        raise SystemExit('dotnet not found on PATH — install the .NET SDK, set DOTNET, or\n'
                         'drop --build to use the committed prebuilt/Bloodlines.dll.')

    print('building Release...')
    result = subprocess.run([dotnet, 'build', PROJECT, '-c', 'Release', '--nologo', '-v', 'q'],
                            cwd=REPO)
    if result.returncode != 0:
        raise SystemExit('build failed')


def resolve_binary():
    """A local build wins; the committed prebuilt DLL is the no-SDK fallback.

    Installing a 900 MB SDK to play a mod is a bad trade, so prebuilt/ exists —
    but a developer who has just built must never ship a stale binary by
    accident, which is why the build output is checked first.
    """
    if os.path.exists(BINARY):
        return BINARY, 'local build'
    if os.path.exists(PREBUILT):
        return PREBUILT, 'prebuilt (no .NET SDK needed)'
    raise SystemExit('Bloodlines.dll not found — run with --build, or build the project first.')


def copy_into(source, target_dir, name=None):
    os.makedirs(target_dir, exist_ok=True)
    target = os.path.join(target_dir, name or os.path.basename(source))
    shutil.copy2(source, target)
    return target


def main():
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--build', action='store_true',
                        help='run dotnet build first (needs the .NET SDK)')
    parser.add_argument('--audio', help='a generated audio bank to fold into the package')
    parser.add_argument('--clean', action='store_true', help='wipe build/deploy first')
    args = parser.parse_args()

    if args.build:
        build()

    binary, origin = resolve_binary()
    print('using Bloodlines.dll: {}'.format(origin))

    if args.clean and os.path.exists(DEPLOY):
        shutil.rmtree(DEPLOY)

    scripts = os.path.join(DEPLOY, 'scripts')
    root = os.path.join(scripts, 'Bloodlines')
    data = os.path.join(root, 'data')
    audio = os.path.join(root, 'audio')
    missions = os.path.join(root, 'missions')

    for folder in (scripts, root, data, audio, missions):
        os.makedirs(folder, exist_ok=True)

    copy_into(binary, scripts)

    for name in DATA_FILES:
        source = os.path.join(REPO, 'data', name)
        if os.path.exists(source):
            copy_into(source, data)
        else:
            print('WARNING: missing data file {} — run tools/parse_bible.py'.format(name))

    for name in CONFIG_FILES:
        copy_into(os.path.join(REPO, 'config', name), root)

    if args.audio:
        if not os.path.isdir(args.audio):
            raise SystemExit('audio bank not found: ' + args.audio)
        shutil.copytree(args.audio, audio, dirs_exist_ok=True)

    # Mission packs from external assemblies are dropped in here; the registry's
    # assembly column names them.
    with io.open(os.path.join(missions, 'README.txt'), 'w', encoding='utf-8', newline=CRLF) as handle:
        handle.write('Drop external mission-pack DLLs here and name them in the\n'
                     'assembly column of data/missions.tsv. Missions built into\n'
                     'Bloodlines.dll need nothing in this folder.\n')

    # The DLC asset pack stays loose: OpenIV imports it, this never writes an .rpf.
    assets_source = os.path.join(REPO, 'assets', 'bloodlines_assets')
    if os.path.isdir(assets_source):
        assets_target = os.path.join(DEPLOY, '_openiv_import', 'bloodlines_assets')
        shutil.copytree(assets_source, assets_target, dirs_exist_ok=True)

    print('\npackaged into {}'.format(os.path.relpath(DEPLOY, REPO)))
    for base, _, files in sorted(os.walk(DEPLOY)):
        depth = base[len(DEPLOY):].count(os.sep)
        print('  ' * depth + os.path.basename(base) + '/')
        for name in sorted(files):
            print('  ' * (depth + 1) + name)

    print('\nCopy the contents of scripts/ into "Grand Theft Auto V/scripts/".')
    print('Import _openiv_import/bloodlines_assets with OpenIV (see assets/README.md).')


if __name__ == '__main__':
    sys.exit(main())

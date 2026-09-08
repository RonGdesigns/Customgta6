"""Build the mod and lay it out exactly as it installs into GTA V.

Produces build/deploy/, which mirrors the game root. Copy its contents over your
Grand Theft Auto V folder and the mod is installed — no hand-placing of files, no
guessing which ini goes where.

    dotnet build src/Bloodlines/Bloodlines.csproj -c Release
    python3 tools/package.py                       # or --build to do both
    python3 tools/package.py --audio build/audio   # include a generated voice pack

Nothing Rockstar owns is touched: the DLC asset pack folder is staged as loose
files for OpenIV to import, never as a modified game archive.
"""

import argparse
import os
import shutil
import subprocess
import sys

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECT = os.path.join(REPO, 'src', 'Bloodlines', 'Bloodlines.csproj')
BINARY = os.path.join(REPO, 'src', 'Bloodlines', 'bin', 'Release', 'Bloodlines.dll')
DEPLOY = os.path.join(REPO, 'build', 'deploy')

DATA_FILES = ['missions.tsv', 'dialogue.tsv', 'anchors.tsv', 'campaign_registry.json']
CONFIG_FILES = ['Bloodlines.ini', 'Bloodlines.Locations.ini']


def build():
    # Visual Studio users can skip --build entirely; DOTNET lets CI point at an SDK
    # that is not on PATH.
    dotnet = os.environ.get('DOTNET') or shutil.which('dotnet')
    if not dotnet:
        raise SystemExit('dotnet not found on PATH — build the project first, or set DOTNET.')

    print('building Release...')
    result = subprocess.run([dotnet, 'build', PROJECT, '-c', 'Release', '--nologo', '-v', 'q'],
                            cwd=REPO)
    if result.returncode != 0:
        raise SystemExit('build failed')


def copy_into(source, target_dir, name=None):
    os.makedirs(target_dir, exist_ok=True)
    target = os.path.join(target_dir, name or os.path.basename(source))
    shutil.copy2(source, target)
    return target


def main():
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--build', action='store_true', help='run dotnet build first')
    parser.add_argument('--audio', help='a generated audio bank to fold into the package')
    parser.add_argument('--clean', action='store_true', help='wipe build/deploy first')
    args = parser.parse_args()

    if args.build:
        build()

    if not os.path.exists(BINARY):
        raise SystemExit('Bloodlines.dll not found — run with --build, or build the project first.')

    if args.clean and os.path.exists(DEPLOY):
        shutil.rmtree(DEPLOY)

    scripts = os.path.join(DEPLOY, 'scripts')
    root = os.path.join(scripts, 'Bloodlines')
    data = os.path.join(root, 'data')
    audio = os.path.join(root, 'audio')
    missions = os.path.join(root, 'missions')

    for folder in (scripts, root, data, audio, missions):
        os.makedirs(folder, exist_ok=True)

    copy_into(BINARY, scripts)

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
    with open(os.path.join(missions, 'README.txt'), 'w') as handle:
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

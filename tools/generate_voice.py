"""Batch-generate the campaign's voice lines from data/dialogue.tsv.

Track 3 of the omnibus bible calls for ElevenLabs output as 44.1kHz 16-bit mono
WAV, dropped into scripts/Bloodlines/audio/ where DialogueDirector picks them up
by cue id. This is that script, with three changes from the bible's listing:

  * the API key comes from the ELEVENLABS_API_KEY environment variable rather than
    being typed into a file that ends up in version control;
  * lines are read from the generated data files, so the bible stays the single
    source of record;
  * existing files are skipped unless --force is passed, because re-running this
    over 255 cues is billable every time.

    export ELEVENLABS_API_KEY=...
    python3 tools/generate_voice.py --out "/path/to/GTA V/scripts/Bloodlines/audio"
    python3 tools/generate_voice.py --mission M01 --dry-run

Voice ids are the bible's defaults and are meant to be replaced with whatever
voices you actually cast; put the replacements in tools/voices.json.
"""

import argparse
import csv
import json
import os
import sys
import time
import urllib.error
import urllib.request

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DIALOGUE = os.path.join(REPO, 'data', 'dialogue.tsv')
MISSIONS = os.path.join(REPO, 'data', 'missions.tsv')
VOICES_OVERRIDE = os.path.join(REPO, 'tools', 'voices.json')

DEFAULT_VOICES = {
    'ICE':   {'voice_id': 'EXAVITQu4vr4xnSDxMaL', 'stability': 0.65, 'similarity_boost': 0.85},
    'GOHAN': {'voice_id': 'ErXwobaYiN019PkySvjV', 'stability': 0.50, 'similarity_boost': 0.80},
    'GUESS': {'voice_id': 'VR6AewLTigWG4xSOukaG', 'stability': 0.45, 'similarity_boost': 0.75},
}

API = 'https://api.elevenlabs.io/v1/text-to-speech/{voice_id}?output_format=pcm_44100'


def load_voices():
    voices = dict(DEFAULT_VOICES)
    if os.path.exists(VOICES_OVERRIDE):
        with open(VOICES_OVERRIDE) as handle:
            voices.update(json.load(handle))
        print('using voice overrides from tools/voices.json')
    return voices


def wav_header(pcm_bytes, sample_rate=44100, channels=1, bits=16):
    """ElevenLabs returns raw PCM for the pcm_* formats; SoundPlayer needs a RIFF header."""
    import struct
    byte_rate = sample_rate * channels * bits // 8
    block_align = channels * bits // 8
    return (b'RIFF' + struct.pack('<I', 36 + len(pcm_bytes)) + b'WAVEfmt ' +
            struct.pack('<IHHIIHH', 16, 1, channels, sample_rate, byte_rate, block_align, bits) +
            b'data' + struct.pack('<I', len(pcm_bytes)))


def audio_dirs():
    """Mission id -> its audio bank folder, e.g. M01 -> audio/Act1/M01."""
    banks = {}
    with open(MISSIONS) as handle:
        for row in csv.DictReader(handle, delimiter='\t'):
            banks[row['id']] = row.get('audio_dir', '')
    return banks


def rows(mission_filter):
    with open(DIALOGUE) as handle:
        for row in csv.DictReader(handle, delimiter='\t'):
            if mission_filter and row['mission'].upper() != mission_filter.upper():
                continue
            yield row


def main():
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--out', default=os.path.join(REPO, 'build', 'audio'),
                        help='audio bank root (default: build/audio)')
    parser.add_argument('--flat', action='store_true',
                        help='write every cue into one folder instead of the partitioned bank')
    parser.add_argument('--mission', help='only this mission, e.g. M01')
    parser.add_argument('--force', action='store_true', help='regenerate files that already exist')
    parser.add_argument('--dry-run', action='store_true', help='list what would be generated')
    args = parser.parse_args()

    if not os.path.exists(DIALOGUE):
        raise SystemExit('data/dialogue.tsv not found — run tools/parse_bible.py first.')

    queue = [row for row in rows(args.mission)]
    if not queue:
        raise SystemExit('no cues matched.')

    os.makedirs(args.out, exist_ok=True)
    voices = load_voices()
    banks = {} if args.flat else audio_dirs()

    key = os.environ.get('ELEVENLABS_API_KEY')
    if not key and not args.dry_run:
        raise SystemExit('set ELEVENLABS_API_KEY (or pass --dry-run).')

    generated = skipped = failed = 0
    for row in queue:
        # The bank is partitioned per act and mission so no folder holds hundreds of
        # files; DialogueDirector resolves the same layout at runtime.
        bank = banks.get(row['mission'], '')
        folder = os.path.join(args.out, *bank.split('/')[1:]) if bank.startswith('audio/') else args.out
        os.makedirs(folder, exist_ok=True)
        target = os.path.join(folder, row['cue_id'] + '.wav')
        if os.path.exists(target) and not args.force:
            skipped += 1
            continue

        speaker = row['speaker'].upper()
        profile = voices.get(speaker)
        if not profile:
            print('no voice cast for speaker {} ({}), skipping'.format(speaker, row['cue_id']))
            skipped += 1
            continue

        if args.dry_run:
            print('{}  [{}]  {}'.format(row['cue_id'], speaker, row['line'][:70]))
            generated += 1
            continue

        payload = json.dumps({
            'text': row['line'],
            'model_id': profile.get('model_id', 'eleven_monolingual_v1'),
            'voice_settings': {
                'stability': profile['stability'],
                'similarity_boost': profile['similarity_boost'],
            },
        }).encode()

        request = urllib.request.Request(
            API.format(voice_id=profile['voice_id']), data=payload,
            headers={'xi-api-key': key, 'Content-Type': 'application/json'})

        try:
            with urllib.request.urlopen(request, timeout=120) as response:
                pcm = response.read()
            with open(target, 'wb') as handle:
                handle.write(wav_header(pcm) + pcm)
            generated += 1
            print('generated {}'.format(os.path.basename(target)))
            time.sleep(0.4)  # stay clear of the rate limit
        except urllib.error.HTTPError as error:
            failed += 1
            print('FAILED {}: HTTP {} {}'.format(row['cue_id'], error.code,
                                                 error.read()[:200].decode('utf-8', 'replace')),
                  file=sys.stderr)
        except OSError as error:
            failed += 1
            print('FAILED {}: {}'.format(row['cue_id'], error), file=sys.stderr)

    print('\n{} generated, {} skipped, {} failed -> {}'.format(generated, skipped, failed, args.out))


if __name__ == '__main__':
    main()

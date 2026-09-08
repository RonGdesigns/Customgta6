"""Minimal PDF text extractor for the Bloodlines design bibles.

The bibles are WeasyPrint output with subset fonts and Identity-H encoding, so the
text only comes back readable if glyph codes are mapped through each font's
ToUnicode CMap. Nothing here needs a third-party dependency, which matters: the
parse step has to keep working on any machine that can run python3.

Usage:  python3 tools/pdf_text.py <bible.pdf> <out.txt>
"""

import re
import sys
import zlib


def _decompress(body):
    if body is None:
        return None
    match = re.search(rb'stream\r?\n', body)
    if not match:
        return None
    raw = body[match.end():body.rfind(b'endstream')]
    if b'FlateDecode' not in body[:match.start()]:
        return raw
    try:
        return zlib.decompress(raw)
    except zlib.error:
        try:
            return zlib.decompressobj().decompress(raw)
        except zlib.error:
            return None


def _load_objects(data):
    objects = {}
    for match in re.finditer(rb'(\d+)\s+(\d+)\s+obj', data):
        number = int(match.group(1))
        objects[number] = data[match.end():data.find(b'endobj', match.end())]

    # Objects packed inside object streams have to be unpacked before fonts resolve.
    for number, body in list(objects.items()):
        if b'/ObjStm' not in body[:400]:
            continue
        stream = _decompress(body)
        count = re.search(rb'/N\s+(\d+)', body)
        first = re.search(rb'/First\s+(\d+)', body)
        if not (stream and count and first):
            continue
        count, first = int(count.group(1)), int(first.group(1))
        header = stream[:first].split()
        for i in range(count):
            offset = int(header[2 * i + 1])
            end = int(header[2 * i + 3]) + first if i + 1 < count else len(stream)
            objects.setdefault(int(header[2 * i]), stream[first + offset:end])
    return objects


def _load_cmaps(objects):
    cmaps = {}
    for number, body in objects.items():
        ref = re.search(rb'/ToUnicode\s+(\d+)\s+0\s+R', body)
        if not ref:
            continue
        stream = _decompress(objects.get(int(ref.group(1))))
        if not stream:
            continue
        cmap = {}
        for block in re.findall(rb'beginbfchar(.*?)endbfchar', stream, re.S):
            for code, value in re.findall(rb'<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>', block):
                cmap[int(code, 16)] = bytes.fromhex(value.decode()).decode('utf-16-be', 'replace')
        for block in re.findall(rb'beginbfrange(.*?)endbfrange', stream, re.S):
            for low, high, dest in re.findall(
                    rb'<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>', block):
                low, high, base = int(low, 16), int(high, 16), int(dest, 16)
                for i in range(high - low + 1):
                    cmap[low + i] = chr(base + i)
        cmaps[number] = cmap
    return cmaps


def _unescape(token):
    body = token[1:-1]
    out = bytearray()
    i = 0
    escapes = {0x6e: 10, 0x72: 13, 0x74: 9, 0x62: 8, 0x66: 12, 0x28: 40, 0x29: 41, 0x5c: 92}
    while i < len(body):
        char = body[i]
        if char == 0x5c and i + 1 < len(body):
            nxt = body[i + 1]
            if nxt in escapes:
                out.append(escapes[nxt])
                i += 2
                continue
            if 0x30 <= nxt <= 0x37:
                digits = chr(nxt)
                i += 2
                while i < len(body) and len(digits) < 3 and 0x30 <= body[i] <= 0x37:
                    digits += chr(body[i])
                    i += 1
                out.append(int(digits, 8))
                continue
            i += 1
            continue
        out.append(char)
        i += 1
    return bytes(out)


TOKENS = re.compile(
    rb'/([\w.+-]+)\s+[\d.-]+\s+Tf|\((?:\\.|[^()\\])*\)|<([0-9A-Fa-f\s]+)>|TJ|Tj|T\*|Td|TD|Tm|ET|BT',
    re.S)


def extract(path):
    data = open(path, 'rb').read()
    objects = _load_objects(data)
    cmaps = _load_cmaps(objects)

    pages = []
    for number, body in objects.items():
        if b'/Page' not in body or b'/Contents' not in body or b'/Pages' in body[:60]:
            continue
        contents = re.search(rb'/Contents\s+(\d+)\s+0\s+R', body)
        resources = re.search(rb'/Resources\s+(\d+)\s+0\s+R', body)
        if contents:
            pages.append((number, int(contents.group(1)),
                          int(resources.group(1)) if resources else None, body))
    pages.sort()

    out = []
    for _, contents_ref, resources_ref, body in pages:
        resource_body = objects.get(resources_ref, body) if resources_ref else body
        font_ref = re.search(rb'/Font\s+(\d+)\s+0\s+R', resource_body)
        if font_ref:
            font_body = objects.get(int(font_ref.group(1)), b'')
        else:
            inline = re.search(rb'/Font\s*<<(.*?)>>', resource_body, re.S)
            font_body = inline.group(1) if inline else b''
        fonts = {name.decode(): int(ref)
                 for name, ref in re.findall(rb'/([\w.+-]+)\s+(\d+)\s+0\s+R', font_body)}

        content = _decompress(objects.get(contents_ref))
        if not content:
            continue

        cmap, two_byte, buffer = None, False, []
        for match in TOKENS.finditer(content):
            token = match.group(0)
            if token.endswith(b'Tf'):
                ref = fonts.get(match.group(1).decode())
                cmap = cmaps.get(ref)
                two_byte = b'/Type0' in objects.get(ref, b'')
                continue
            if match.group(2) is not None:
                digits = re.sub(rb'\s', b'', match.group(2))
                if len(digits) % 2:
                    digits += b'0'
                try:
                    raw = bytes.fromhex(digits.decode())
                except ValueError:
                    continue
            elif token.startswith(b'('):
                raw = _unescape(token)
            elif token in (b'T*', b'Td', b'TD') or token.endswith(b'Tm'):
                # Every line of body text is placed with its own matrix, so these
                # operators are where the document's line breaks actually live.
                buffer.append('\n')
                continue
            else:
                continue

            if cmap is None:
                buffer.append(raw.decode('latin-1'))
            elif two_byte:
                codes = [int.from_bytes(raw[i:i + 2], 'big') for i in range(0, len(raw) - 1, 2)]
                buffer.append(''.join(cmap.get(code, '') for code in codes))
            else:
                buffer.append(''.join(cmap.get(code, '') for code in raw))

        text = ''.join(buffer)
        if text.strip():
            out.append(text)

    text = '\n'.join(out)
    text = re.sub(r'[ \t]+\n', '\n', text)
    return re.sub(r'\n{3,}', '\n\n', text)


if __name__ == '__main__':
    if len(sys.argv) != 3:
        raise SystemExit(__doc__)
    open(sys.argv[2], 'w').write(extract(sys.argv[1]))
    print('wrote', sys.argv[2])

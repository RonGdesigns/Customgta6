#!/usr/bin/env python3
"""Propose a price for every vehicle in the dealer catalog.

Why a tool and not a formula in the game
----------------------------------------
The obvious idea is to work a price out at runtime from the model's own speed and
acceleration. That was tried and measured: fitted against the sixty-five prices already
set by hand it is 40 to 45 per cent out, and the misses are systematic. The Virgo is
priced at $18,000 and any performance curve says $39,000; the Oppressor Mk II is priced at
$300,000 and the curve says $44,000, because its value is that it flies and shoots and no
specification captures that.

Those hand prices are not mistakes. They are an affordability ladder: the campaign pays
$55,000 across its first three missions, $345,000 by M10 and $3,722,000 in total, and the
cheap cars exist so an Act I player has something to buy. A runtime formula would fight
that design every time somebody retuned a handling file.

So: a table, generated once, reviewable, diffable, and correctable by hand where a
vehicle's worth is a story fact rather than a number. Prices already set are anchors and
are never moved.

    python tools/price_vehicles.py            # print the proposal
    python tools/price_vehicles.py --apply    # write it into Garages.cs
"""
import argparse
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CATALOG = os.path.join(ROOT, 'src', 'Bloodlines', 'Core', 'StoryVehicles.cs')
GARAGES = os.path.join(ROOT, 'src', 'Bloodlines', 'Core', 'Garages.cs')
DUMP = os.path.join(ROOT, 'build', 'vehicles.json')

# The ladder, anchored to the income curve. Each band is the range a vehicle of that kind
# is priced inside; where it lands in the range is decided by its own ratings.
#
#   Starter      to  $30,000   affordable after M03, which pays $55,000 all told
#   Street       to  $70,000   Act I money
#   Performance  to $120,000   affordable around M10 ($345,000 banked)
#   Exotic       to $240,000   Act I finale money ($1.1M)
#   Hardware     to $500,000   trophies, and nothing costs more than this
BANDS = {
    'COMPACT': (16000, 38000), 'SEDAN': (18000, 45000),
    'MUSCLE': (20000, 72000), 'COUPE': (26000, 70000),
    'SPORT': (45000, 120000), 'SPORT_CLASSIC': (55000, 160000),
    'SUPER': (110000, 240000), 'SUV': (40000, 95000),
    'OFF_ROAD': (32000, 110000), 'MOTORCYCLE': (16000, 70000),
    'MILITARY': (240000, 460000), 'OPEN_WHEEL': (280000, 380000),
    'EMERGENCY': (60000, 120000), 'COMMERCIAL': (90000, 200000),
    'VAN': (30000, 70000), 'UTILITY': (35000, 90000),
    'INDUSTRIAL': (70000, 150000), 'SERVICE': (30000, 70000),
    'BOAT': (38000, 110000), 'HELICOPTER': (190000, 400000),
    'PLANE': (200000, 420000), 'CYCLE': (1000, 4000),
}
# A category the catalog names can override the class band: a weapon or a jet engine is
# worth more than the chassis under it.
CATEGORY_BANDS = {
    'Weaponized': (200000, 460000),
    'Jets': (300000, 500000),
    'Planes': (200000, 420000),
    'Helicopters': (190000, 400000),
    'Boats': (38000, 110000),
    'Special': (140000, 400000),
    'Electric': None,   # priced as its class, because an EV supercar is a supercar
}
FLOOR, CEILING = 1000, 500000
STEP = 1000


def catalog():
    text = open(CATALOG, encoding='utf-8').read()
    return [(n, m, c) for n, m, c in
            re.findall(r'new Choice\("([^"]*)",\s*"([^"]+)",\s*"([^"]+)"\)', text)]


def anchors():
    text = open(GARAGES, encoding='utf-8').read()
    body = text.split('ModelPrices = new Dictionary<string, int>', 1)[1].split('};', 1)[0]
    return {m.lower(): int(p) for m, p in re.findall(r'\{\s*"([A-Za-z0-9_]+)",\s*(\d+)\s*\}', body)}


def models():
    return {v['Name'].lower(): v for v in json.load(open(DUMP, encoding='utf-8'))}


def grade(spec, pool):
    """Where this vehicle sits among its peers, nothing to everything.

    Speed carries more than acceleration because speed is what the buyer is reading off
    the phone's performance rows, and a rank rather than a raw number because the classes
    do not share a scale: 50 m/s is a fast car and a slow jet.
    """
    def rank(key, weight):
        values = sorted(x.get(key) or 0 for x in pool)
        mine = spec.get(key) or 0
        below = sum(1 for v in values if v < mine)
        return weight * (below / max(1, len(values) - 1))
    return rank('MaxSpeed', .6) + rank('Acceleration', .4)


def propose():
    dump, fixed, rows = models(), anchors(), catalog()
    missing = [m for _, m, _ in rows if m.lower() not in dump]
    if missing:
        print('not in the public dump, cannot be priced: ' + ', '.join(missing), file=sys.stderr)

    # Grouped by the page a buyer actually sees. Uniqueness has to be decided there and not
    # per band: Cars holds nine different classes, and two of them independently landing on
    # $18,000 is still two cars sharing a line on the same list.
    pages = {}
    for name, model, category in rows:
        spec = dump.get(model.lower())
        if spec:
            pages.setdefault(category, []).append((name, model, spec))

    proposed = {}
    for category, members in sorted(pages.items()):
        override = CATEGORY_BANDS.get(category)
        taken = {fixed[m.lower()] for _, m, _ in members if m.lower() in fixed}
        # Priced best-first so the fastest thing on the page gets the top of its band and
        # the nudging for uniqueness pushes downward through the also-rans.
        by_class = {}
        for name, model, spec in members:
            by_class.setdefault(spec.get('Class'), []).append(spec)
        for name, model, spec in sorted(members, key=lambda x: -grade(x[2], by_class[x[2].get('Class')])):
            if model.lower() in fixed:
                continue
            low, high = override or BANDS.get(spec.get('Class'), (40000, 90000))
            value = low + (high - low) * grade(spec, by_class[spec.get('Class')])
            value = max(FLOOR, min(CEILING, int(round(value / STEP)) * STEP))
            while value in taken:
                value += STEP
            taken.add(value)
            proposed[model] = value
    return rows, dump, fixed, proposed


def report(rows, dump, fixed, proposed):
    by_category = {}
    for name, model, category in rows:
        price = fixed.get(model.lower(), proposed.get(model))
        if price is None:
            continue
        by_category.setdefault(category, []).append((price, name, model, model.lower() in fixed))
    lines = []
    for category in sorted(by_category):
        items = sorted(by_category[category], reverse=True)
        lines.append('')
        lines.append('%s — %d vehicles, $%s to $%s'
                     % (category, len(items), format(items[-1][0], ','), format(items[0][0], ',')))
        for price, name, model, anchored in items:
            lines.append('  %10s  %-38s %-16s %s'
                         % ('$' + format(price, ','), name, model, 'set by hand' if anchored else ''))
    return '\n'.join(lines)


def apply(proposed):
    text = open(GARAGES, encoding='utf-8').read()
    marker = '        };\n        public static int Of(StoryVehicles.Choice choice)'
    assert marker in text, 'the price table does not look the way this tool expects'
    ordered = sorted(proposed.items(), key=lambda kv: (-kv[1], kv[0]))
    block = [',',
             '            // Generated by tools/price_vehicles.py from the affordability ladder in',
             '            // that file. Every catalog vehicle has its own number, so the price-sorted',
             '            // list a buyer reads has a real order instead of a hundred and thirty-seven',
             '            // ties at the category default. Edit a line freely: the tool never moves a',
             '            // price that is already here.']
    # The Cypher is Rockstar's own vehicle name and the dialect lint reads it as a
    # misspelling of cipher. Correcting a name would be falsifying it, so the line it
    # lands on carries the suppression the hand-written table already carried.
    line, suppress = '           ', False
    for model, price in ordered:
        piece = ' { "%s", %d },' % (model, price)
        if len(line) + len(piece) > 96:
            block.append(line + (" // dialect-ok: the Cypher is the game's own vehicle name" if suppress else ''))
            line, suppress = '           ', False
        line += piece
        suppress = suppress or model.lower() == 'cypher'
    if line.strip():
        block.append(line.rstrip(',') + (" // dialect-ok: the Cypher is the game's own vehicle name" if suppress else ''))
    text = text.replace(marker, '\n'.join(block) + '\n' + marker, 1)
    open(GARAGES, 'w', encoding='utf-8', newline='').write(text)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true', help='write the proposal into Garages.cs')
    parser.add_argument('--out', help='also write the proposal to this file')
    args = parser.parse_args()

    rows, dump, fixed, proposed = propose()
    text = report(rows, dump, fixed, proposed)
    print(text)
    print('\n%d vehicles: %d already priced by hand, %d proposed.'
          % (len(rows), sum(1 for _, m, _ in rows if m.lower() in fixed), len(proposed)))
    if args.out:
        open(args.out, 'w', encoding='utf-8', newline='').write(text + '\n')
    if args.apply:
        apply(proposed)
        print('written into src/Bloodlines/Core/Garages.cs')


if __name__ == '__main__':
    main()

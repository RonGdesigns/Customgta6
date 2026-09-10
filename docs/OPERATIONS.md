# Operations: which missions run back to back

A chapter that follows the previous one inside the same night, at the same place or
along the same route, with the same people and assets, is one operation. The mission
manager continues it automatically: the next chapter starts when the previous
aftermath ends, with no marker walk and no mission key. Each chapter still commits
its own completion and rewards, a failure retries that chapter alone, and a fresh
session resumes at the next chapter's marker.

The judgment below uses the campaign data (`missions.tsv` time and location) and
the story's own logic. A gap of a few hours at the same site can still be one night;
a gap that needs a different day, a rest, or a new plan is a separate job.

## Declared operations (`MissionManager.Continuations`)

| Operation | Chapters | Clock | Why it is one |
| --- | --- | --- | --- |
| Port Heist | M19 → M20 → M21 → M22 | 03:00, 03:20, 03:45, 04:30 | The breach, the lift, the escort and the drop are one haul on one night; the hand-off ledger already carries the Kraken and the loaded lift between them. |
| Paleto Deep-Sea | M44 → M45 → M46 → M47 → M48 | 02:00, 02:20, 02:40, 03:00, 04:30 | Sub-surface, helipad, vault, collapse, then the run to shore before dawn. M48 is the same crew leaving the same rig in the same boats. |
| Blood Brothers | M63 → M64 → M65 → M66 → M67 → M68 → M69 → M70 | 21:00 through 04:00 | The tower assault, the extraction truck, the drain, the runway and the grounded Titan are one continuous night; the proposal's §5.2 maps the truck, aircraft and waterfront fallback across exactly this span. |

M44 through M70 are not scripted yet. The declarations are inert until a chapter is
playable, and they record the intent so those missions are built as chapters, not
as separately started jobs.

## Staging links, recommended but not declared

| Link | Clock | Treatment |
| --- | --- | --- |
| M18 → M19 | 20:00 → 03:00 | Staging the night of the heist. Continue with a "Later that night" card, but only after the SM01–SM03 gate has been checked and warned about before M18 starts, so the gate cannot refuse the player mid-operation. |
| M43 → M44 | 18:00 → 02:00 | Same shape: staging in the sea cave, then the dive. The SM04–SM06 gate belongs before M43. |

Both wait on the gate warning being moved ahead of the staging mission. Until then
they start from their markers.

## Candidates judged and left separate

| Pair | Clock | Why not |
| --- | --- | --- |
| M04 → M05 | 22:00 → 03:30 | The drive points to Mateo; the crew regroups, loses the pursuit and goes to the coast. A new site and a new plan. |
| M08 → M09 → M10 | 16:45 → 19:30 → 23:00 | The proposal keeps M09 as a separate clearance job with the engines stored between; M10 collects them again. |
| M12 → M13 | 21:30 → 02:00 | Recon, then a different job on a different craft. |
| M14 → M15 | 18:00 → 23:30 | Two setups; each removes its own obstacle. |
| M41 → M42 | 23:00 → 03:00 | The lodge and the Titan drop use different assets and a different pilot situation. |
| M61 → M62 | 23:30 → 05:00 | Plausible as one night, but M62 is the coastal intercept the proposal wants distinguished from the port fighting. Revisit when M61 is scripted. |
| M22 → M23 | 04:30 → 08:00 | The foundry loss ends the operation; M23 is a displacement to a new refuge after it. |
| M47 → M49 | — | M48 ends at the shore; M49 is the return south on a later evening in the Granger. |

## How to add one

Add the pair to `MissionManager.Continuations`. The next chapter must be playable
and its prerequisite is the previous chapter, which the pass just committed. If the
next chapter needs an asset from the previous one, record it through the
`HandoffLedger` in `OnPassed` and consume it in the receiver's `Setup`, as the Port
Heist does with the Kraken and the loaded lift.

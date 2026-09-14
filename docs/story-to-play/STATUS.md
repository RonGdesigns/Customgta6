# Story-to-Play Status and Authority

## Publication status

This planning library is published to the working campaign branch as documentation only. It does not increase the runtime mission count or certify any planned mission as implemented or live-tested.

## Review pin

The planning review was reconciled against `aa50735a29cf407eb28c7ac725590585e3194465` on `claude/gta-v-custom-version-477edi`, which includes PR52's shared guard-placement correction. At that baseline MissionCatalog registers M01-M43 and SM01-SM06.

Any later implementation work must first compare against the current branch head and preserve compatible intervening fixes.

## Status vocabulary

| Label | Meaning |
|---|---|
| `SOURCE_RECORD` | Written in an inspected source; not automatically implemented. |
| `OWNER_LOCKED` | Explicit user requirement; preserve unless the owner changes it. |
| `OPEN_RECOMMENDATION` | Proposed choice awaiting explicit selection where it changes the experience. |
| `PLANNED_NOT_IMPLEMENTED` | Detailed mission treatment exists but gameplay is not registered/built by that planning pass. |
| `OFFLINE_CHECKED` | Named asset/geometry check ran with identified inputs. |
| `AUTOMATED_RUNTIME_CHECKED` | Code ran under the specified automated environment; stand-ins are not GTA. |
| `LIVE_REPORTED_FAILURE` | User reported an in-game failure; source tests do not erase it. |
| `LIVE_ACCEPTED` | A named build/installation/route was successfully checked in GTA. |

Merging a document can only change repository availability. It cannot advance a mission from `PLANNED_NOT_IMPLEMENTED` to `LIVE_ACCEPTED`.

## Locked carry-forward

- M44-M48: one uninterrupted attempt; no intermission or saved midpoint; full restart at M44 after failure/abort/quit/reload; preserve earned preparation.
- Preserve the Port Heist's existing continuous operation model.
- Ron is the dependable connector; Ice, Gohan and Ron are equals.
- Preserve the approved LSIA self-drive/home purpose and do not relocate the home to solve unrelated story/visual problems.
- Preserve the four-seat M01 getaway.
- Preserve the approved M25 bridge/parachute/boat/100-meter departure treatment.
- Preserve `M70_SCENE_OUTRO_10_GUESS` as the final spoken M70 cue with the approved wording: `Told y'all... Guess never misses an exit.` unless the owner explicitly changes it.

## Historical-source cautions

- Novel chapter numbers are not mission IDs. The Personal Reckonings chapter contains later solo stories; subsequent book chapter numbering is shifted relative to mission numbering.
- The novel contains internal continuity conflicts (including Colonel Vance family wording and alternate M70 escape staging). Do not silently import contradictions over current approved canon.
- Old phase-bookmark/checkpoint descriptions for the Paleto operation are superseded by the owner's uninterrupted-operation requirement.
- Older proposal counts stating M31+ was unscripted are historical; the reviewed baseline already implements through M43 and SM06.
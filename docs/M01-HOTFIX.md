# Bloodlines: M01 and controller hotfix

The live log showed M01 starting at 18:00:45 and Guess taking the prototype at
18:03:18. Neither Ice's identification nor Gohan's ledger objective completed,
so the mission stayed in stage 0. Reaching a waypoint could not advance it.
Later debug deployment changes replaced the mission's crew without aborting it.

## Changes

- Camera cleanup rejects an invalid GET_RENDERING_CAM handle and explicitly returns to gameplay rendering before restoring a valid prior scripted camera. Scene start/end is logged.
- M01 uses stock dock exteriors. The proposed crane platform and yacht interior do not exist as installed custom map assets. Bible anchor numbers no longer replace playable location data automatically. Ground navigation is requested and checked before the cast is placed; missing navigation rejects setup within a bound instead of spawning into empty space. This is not a manual coordinate survey.
- Ice watches Mateo, Gohan uses a clipboard at his ledger assignment, and Guess approaches/boards the prototype while inactive. Mission-owned AI prevents ordinary following from taking over. Taking control clears that character's scripted task. Player completion of the three assignments is still required.
- Ice supports free-aim identification. Persistent HUD guidance states the active task and identifies unfinished character assignments. The eight-second data copy, recognition scene, guard wave, Mateo escape and car extraction form a tested stage sequence.
- The final exit requires all three living characters in the actual prototype. Clearing the guard wave can advance Mateo's escape after ten seconds rather than waiting out the full ninety seconds. Missing essential actors fail setup.
- Split-approach Hold takes priority over automatic boarding and catch-up warps. Debug deployment changes are blocked while a mission is active.
- Controller character selection and right-stick/A/B debug navigation are implemented. Stick navigation has a dead zone and repeat delay. The menu consumes gameplay input and cancels pending character selection.
- Dialogue extraction stops at structural boundaries. M70's final line no longer contains the technical appendix. Explicit authored edits in data/dialogue_edits.json align the opening with its gameplay and remove premature/contradictory later-story claims. The PDFs are unchanged.

## Controls

| Action | Controller |
|---|---|
| Select a character | Hold D-pad Down; right stick left = Ice, up = Gohan, right = Guess; release Down to switch |
| Open debug menu | Hold D-pad Down and press B (Dev Enabled must be true) |
| Move / adjust menu value | Right stick up/down / left/right |
| Select / go back | A / B; B at the root closes the menu |
| Start an available nearby mission | D-pad Right at its start marker |
| Skip cutscene | A |

Keyboard controls remain available. Solo mission and mission-specific switch locks still apply.

## Retest M01

1. Start near its yellow dockyard start marker. Confirm grounded cast, appropriate opening activities and a return to the normal player camera after the intro or a skip.
2. As Ice, aim directly at Mateo without shooting. Use the controller to switch to Gohan; stay on foot at the ledger marker for eight seconds. Switch to Guess and take the orange prototype's driver seat.
3. Confirm the recognition scene starts once, returns to gameplay, and the guards fight. Clear the guards and follow the getaway instruction.
4. Stop so everyone can board the prototype. Drive all three to the yellow exit. Confirm MISSION PASSED and the aftermath camera releases correctly.
5. Open the debug menu and test right-stick navigation, A selection and B back/close. Confirm camera movement, melee, sprint and vehicle exit do not happen behind the menu.

## Validation and limits

70 C# sources compile against the pinned SHVDN 3.6 API with the Roslyn fallback.
67 story/runtime checks, 35 companion/death/survey checks, and 3 parser tests pass:
105 automated checks total. Mission lint reports no errors; location district
audit reports 0 of 121 flagged. District validation does not prove a coordinate
has usable geometry. The M01 sequence is exercised with GTA stand-ins; actual
navmesh availability, aim line of sight, boarding, animation and controller
behavior require the above live retest.

The script API's camera getter wraps the native handle even when it is invalid;
assigning a non-null camera enables script rendering. See the upstream
[SHVDN World implementation](https://github.com/scripthookvdotnet/scripthookvdotnet/blob/v3.6.0/source/scripting_v3/GTA/World.cs).

There are still 30 gameplay scripts (M01–M27 and SM01–SM03), with dialogue drafts
for all 79 records. M28–M30 and SM04–SM06 are the agreed next batch, paused for
these live blockers. M29's drafted depot-tanker adaptation is not yet a gameplay
script. KJ remains a supporting character in SM03, with SM09's appearance drafted.
This hotfix does not claim that every later split mission already has bespoke AI.

Story Mode only. No Rockstar archives, savegame, personal INIs or surveyed captures
are modified by this hotfix installer.

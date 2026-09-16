# Grand Senora bunker: M23 and headquarters access

The former radar-array yard did not contain the bunker promised by the mission. M23 now uses the shipped Grand Senora Desert bunker entrance and the Gunrunning bunker interior. The airport gate and claims about inaccessible underground rooms are removed.

## Playable sequence

1. Guess drives all three brothers from the north access road to the bunker approach and stops. Ice then takes point while the others cover. Squatters retaliate when fired on and fight when the entrance is breached; the crew car stays outside.
2. Guess checks the workbench, empty fuel reserve and room for vehicles.
3. Gohan works the visible generator controls, restoring access.
4. Gohan enters on foot using E / D-pad Right. The player remains protected and faded until the interior and collision are ready.
5. Gohan walks along the entry passage and inspects the shelter. His report is radio dialogue; Ice and Guess retain their exterior posts.
6. Gohan returns to the interior entrance marker, uses the same button to leave, and rejoins the crew. Completion grants the existing bunker unlock.

The entrance is a controlled fade transition into a real loaded interior. The external hatch model is static. Vehicles remain outside; this is not a seamless drive-in garage or an Online business simulation.

Follow-up fixes for fleet purchases, the planning menu, the driven approach and the hatch marker are documented in [bunker service and approach repairs](BUNKER-SERVICE-REPAIRS-2026-09-13.md).

## Repeat visits and failure behavior

After M23, the green Senora bunker safehouse marker opens the same room between jobs. Its entry menu provides the existing headquarters planning, crew fleet, wardrobe, rest/save and exit services. No new ammunition purchase rules or weapon progression are introduced.

Entry requires being on foot, outside combat and free of wanted stars. Existing saves retain the `grandSenoraRadarBunker` unlock. Saved player position remains the exterior entry, never an unloaded underground coordinate. Interior streaming is driven frame by frame; a failed load times out after 12 seconds, restores the prior position and controls, and fails M23 rather than awarding completion. Aborting an interior visit also returns outside. Character switching remains blocked inside.

## Locations and continuity

The installed Enhanced archives were read with CodeWalker without modifying any Rockstar archive. `gr_case0_bunkerclosed` contains the real entrance at approximately (848, 2993, 43). The underground `gr_grdlc_int_02` collision has solid floor near (892.6384, -3245.8664, -99.265); the player entry is approximately one meter above it. The nearby inspection point also has verified floor collision. Both documented and shortened interior IPL names are supported for registration; arrival still requires a real ready interior.

Exterior points were checked against local collision. They remain marked `estimate`, because an archive collision check does not certify gameplay pathfinding or live furniture clearance. No point is falsely marked as a personal survey.

M24 and M28 return to the new vehicle yard. M29 fuel delivery, M31 defenses/withdrawal/response roads, M32 equipment delivery, M34 refuge/medical equipment, M35 delivery and M38 delivery are relocated consistently. M30 inherits the new workbench through its M23 location reference. Earlier M09 radar-site gameplay is unchanged.

Relocated sites have new location keys. Old personally surveyed radar-yard keys are retired and ignored, rather than applied to the new base. Save, appearance, configuration and personal survey INI files must remain untouched during installation. M23's entrance, actor spawns, bays and generator are in its exterior survey; underground entry/inspection points are available through the room survey after entering the bunker.

`data/mission_edits.json`, `data/dialogue_edits.json` and `data/story_beats.txt` preserve revised descriptions and dialogue through regeneration. Original design bibles remain intact.

## Required live acceptance

Automated tests cover successful M23 entry/inspection/exit, missing-interior rollback, cancellation, permanent access, save-position protection and relocated delivery coordinates. The production DLL compiles against SHVDN 3.6.0. Live acceptance is still required:

- Start M23, verify the real hatch/approach and clear the squatters.
- Complete the exterior checks; enter as Gohan, walk the passage and explore the loaded room.
- Return outside, finish M23 and enter again from the safehouse marker. Use the menu exit; verify movement and switching outside.
- On another attempt, abort while inside and confirm a safe return outside.
- Check later deliveries for practical truck clearance and M31 for arrival/withdrawal pathfinding.

The Port Heist behavior is unchanged: a debug-menu start runs the selected section by itself. Starting the normal Port Heist marker runs M19–M22 continuously. A reported failure to start the next section is an error, not the expected standalone debug completion.

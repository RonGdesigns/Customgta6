# Homes, vehicles, scenes and wanted progression

September 8, 2026. This update builds on the campaign/crew package and supersedes its deferred Mission One notes.

## Military crash and wanted levels

The latest crash was recorded by Windows at 21:45:07: GTA5_Enhanced.exe 1.0.1158.13, exception 0x80000003. The minidump confirms an engine breakpoint. It does not establish which mod native caused it; the prior build logged no military steps before the crash.

The revised response defers attack assignments until at least a second after vehicle creation, verifies the living driver is actually seated, uses a dedicated tank attack task, and removes the forced turret-fire call and helicopter passenger drive-by task. The armed helicopter's pilot owns its attack. Tank spawn points reject occupied areas. Spawn and initial attack steps now write diagnostic messages to Bloodlines.log. These changes need an in-game retest; this is not a confirmed crash fix.

Normal play progresses from GTA's five stars to the custom sixth tier after 90 seconds continuously at five. Opening the debug menu pauses that countdown. Dropping below five resets it. The World menu's wanted selector now handles 0–6 for the deployed crew in free roam; lowering six to five or zero clears the custom tier properly. Zero stands down the military. Native GTA police remain at five while the custom sixth response is active. At most one tank and one armed helicopter are dispatched, subject to available spawn space.

Retest first: reach five, close the debug menu and survive for 90 seconds. Check whether the first military vehicle appears, then whether the second arrives and attacks. Check both on foot and in a car. If it crashes, the new Military log entries should show the last completed step.

## DLC cars in Story Mode

Open the debug menu → DLC / Online cars. Choose a car to park on an open road nearby; enter normally. The menu includes 24 choices from installed GTA DLC, including Armored Kuruma, Buffalo STX, Champion, Ignus, Jester RR, Calico GTF and Granger 3600LX. Unavailable models are hidden. Car requests are disabled during missions and capped at four retained vehicles. Release parked DLC cars frees unoccupied requests without deleting your occupied vehicle.

This uses the installed assets and SHVDN vehicle creation, not downloaded car models. It does not import an Online character's purchased vehicles, provide Online services, add every DLC model to traffic or save these requested cars across game restarts. Model loading and any engine despawn behavior still need live verification on Enhanced.

References used: [SHVDN vehicle creation](https://github.com/scripthookvdotnet/scripthookvdotnet/wiki/How-Tos), [SHVDN runtime overview](https://scripthookvdotnet.github.io/), [Enhanced runtime releases](https://github.com/Chiheb-Bacha/ScriptHookVDotNetEnhanced/releases).

## Homes and weapons

The design bible names the three starter homes. Each now has an exterior access point and a colored map icon:

| Character | Home | Weapon rewards after M03 / M06 / M15 |
| --- | --- | --- |
| Ice | Canal Logistics Loft | Pump Shotgun / Combat MG / Heavy Sniper |
| Gohan | Little Seoul Studio | Stun Gun / Carbine Rifle / Special Carbine |
| Guess | Burro Heights Chop Shop | Combat Pistol / Assault SMG / Assault Shotgun |

Use Route to my home in the debug menu or follow the home icon. Stand on foot at your character's marker and press E / D-pad Right to rest six hours, restore health, save campaign position and collect unlocked weapons with basic ammunition. Wanted or combat states block resting. These are exterior access points, not furnished interiors. Locations are estimates with nearby ground checks and can be corrected with the survey tool. Independent companions can choose home as one of their travel destinations.

Weapon ownership acquired through pickups, purchases or mission equipment is recorded separately for each hero in the existing campaign save's new weaponLockers field. Ownership is captured every five seconds and on crew dismissal; recreated characters receive their saved weapons. M03, M06 and M15 add role-specific rewards to the home locker. Existing completed missions count. Visit home to collect newly unlocked equipment, or receive it on the next deployment. This first pass persists ownership, not exact ammunition, tints or attachments. Starter loadouts remain available. A campaign reset clears earned weapon ownership too.

Retest: acquire a gun as one hero, switch away and back, then stand down/redeploy and restart the game. Check the gun remains with its owner. Visit each home and test rest, saving, wanted blocking and a mission reward.

## Mission One and scene continuity

- Guess keeps the real approach-car seat at mission start. The shared handover also avoids clearing seated actors' tasks.
- Ice starts west of the target, away from Guess's eastern approach. Ground validation remains active; route visibility needs a live pass.
- Mateo checks shipping records with a dock technician. A visible table and laptop give Gohan a concrete hacking target. Press E / D-pad Right, then remain at the terminal for eight seconds.
- Gunfire during the quiet approach fails with an explanation. Mateo is mortal: killing him fails because the crew needs to trace the buyers. His marker identifies him as a lead to keep alive.
- Recognition no longer moves the crew together or forces them out of vehicles. They retain their positions and seats. The dialogue explains the dock security channel connecting them.
- The final recognition line starts Mateo's real boarding/escape task, and the camera follows him. Skipping still starts the escape in gameplay. A blocked boat route fails clearly after the timeout instead of visibly teleporting him aboard.
- Shared cutscenes use phone/radio framing for speakers separated by more than 18 metres. Nearby actors can be framed in person. No generic scene moves the crew into a conversational lineup. Other mission scripts were searched for cutscene relocation: M27 retains its explicit, mission-specific plane boarding transfer during a fade.

Retest M01 in different role orders, with Guess still seated when recognition begins. Skip once and watch the whole scene once. Verify camera/control recovery, actor positions, the actual laptop prompt, Mateo's movement and the three-person extraction.

## Validation and installation

89 production sources compile. 262 story/runtime checks, 85 behavior/recovery checks and 3 parser tests pass: 350 total. Mission lint reports no errors; 29/30 missions have full legacy cue coverage, with M01's superseded recognition cues intentionally omitted. District validation flags 0/126 locations. These checks use GTA stand-ins and do not prove live terrain, native stability or rendered animations.

Installation backs up replaced files, synchronizes source/prebuilt/deployed DLL hashes, and preserves user saves, appearance settings, configuration and surveyed coordinates. The weapon save field is created by the mod during play, not by the installer. Exact hashes and changed files are in the installation receipts.

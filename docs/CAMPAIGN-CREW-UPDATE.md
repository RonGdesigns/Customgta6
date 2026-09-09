# Campaign and crew update — September 8, 2026

This batch updates the 30 currently playable missions and the shared crew systems. M01 and M02 retain their earlier reworks; M03–M27 and SM01–SM03 receive the new objective and flow work. The other 49 missions remain dialogue drafts, without gameplay scripts.

## First retest

1. In free roam, put the crew in one car. Drive, switch to a passenger, and check that everyone stays seated and the former player continues driving. Set a waypoint and repeat.
2. With the crew aboard, attract police. Switch seats: the wanted level should remain shared, the AI driver should accelerate and evade, and passengers should shoot at hostile pursuers. The NPC driver should concentrate on driving.
3. Select Independent behavior. Let the companions choose trips, acquire transport, and stop for activities. Select Follow again while standing in an open area. They should approach in their current vehicles, stop, get out, and meet you. A helicopter needs an open landing area; it should hold nearby when it cannot find one. Switch to an airborne pilot to check seat retention.
4. Leave Independent behavior running for several minutes with off-duty crimes enabled. A companion can steal a vehicle and draw a local police response. Switch to that character and then to a separated, clean character. Heat is personal outside missions; sharing a vehicle shares heat. Personal heat lasts for the current crew session.
5. In free roam, use the debug World menu's military response option. Expect a custom sixth tier with a tank and armed helicopter where spawning space permits. Normal escalation follows 90 seconds at five stars. GTA's native police level remains five; the mod supplies the sixth display and military units. Units are capped at two.
6. Check survival and recovery: the crew now deploys with 600 health and 100 armor, switching preserves damage, and death should restore movement and the normal camera.
7. Resume mission testing at M03. Read the persistent objective and use E / D-pad Right when prompted. Confirm the yellow GPS target follows the current task, including reaching the required vehicle before delivery. Test mission completion and retry, especially the boat, aircraft, nonlethal, and timed work stages below.

Normal road travel targets 30–35 m/s, with 45 m/s during pursuit. These are AI task speeds; traffic, corners and pathfinding determine actual speed. Driver ability is set high and traffic avoidance stays enabled. Vehicle boarding uses normal entry tasks where practical. Catch-up recovery happens out of view rather than pulling everyone into the player's car.

## Mission flow changes

| Mission | What to check |
| --- | --- |
| M01 | Earlier playable flow retained. New scene requests below are deferred. |
| M02 | Earlier proximity chase/hack and role prompts retained. |
| M03 | Separate rail, crane and depot roles; explicit terminal work; actual truck loading and delivery. |
| M04 | Surface breaker interaction; guards; acquire the chase car before Miller flees; recover the drive. |
| M05 | Shore support and actual dinghy crew; flare, close approach to living Mateo, capture and questioning. |
| M06 | Feeder work, entrance cover, concurrent hacking and waves, crew boarding, escape. |
| M07 | Pickup car and roof work; descend to the pickup; armed pursuit helicopter. |
| M08 | Camera, guard and loading roles; equipment interaction; pursue and deliver the actual Flatbed. |
| M09 | Frogger shadowing, roof support, moving convoy, driver takedown and cab transponder recovery. |
| M10 | Flatbed chase with a 35 mph floor and grace period; Ice's response; required truck delivery. |
| M11 | Required car on the rollers; explicit mount work; throttle control at 22–28 PSI. |
| M12 | Actual submarine descent and timed work; return the submarine to the jetty. |
| M13 | Waterscooter charge planting, shore watchmen and explicit detonator interaction in the Granger. |
| M14 | Separate overwatch, hangar and radio roles; actual Besra low flight and stopped landing. |
| M15 | Stun gun and nonlethal guards; maintenance splice. Killing required captives fails. |
| M16 | Breach triggers hostility; actual Cargobob canyon flight and Terminal landing. |
| M17 | Separate repair stations, explicit welding and grapple work; final briefing. |
| M18 | Assigned submarine, helicopter and truck; distinct transport, landing, delivery and loading stages. |
| M19 | Seated submarine start; depth work, clamp interaction and actual resurfacing. |
| M20 | Cargobob hover hookup; quayside sniper support; actual load climb. |
| M21 | Gohan and Ice in the launch, Guess in the helicopter; water pursuit; exit at a coastal breakwater. |
| M22 | Helicopter load release, stopped shore landing, emergency call. |
| M23 | Split approach, radar yard fight, three work bays and generator interaction. |
| M24 | Dry-bank recovery truck, timed crane work with cover waves, actual truck delivery. |
| M25 | RPG supplied for the tanker objective, waves, descent and actual boat boarding. |
| M26 | Armed Lazer and airborne spotters; destroy targets and land the actual aircraft. |
| M27 | Match the jet; bounded fade into a verified passenger seat; locker interaction, bailout and dinghy pickup. |
| SM01 | Question living Sergei for crate codes, clear guards and inspect crates. |
| SM02 | Nonlethal entry and worm interaction; dialogue describes camera archive access accurately. |
| SM03 | KJ remains a supporting NPC; race requires the actual coupe; explicit finish/ambush resolution. |

Stages now fire dialogue at the corresponding gameplay events. Required assets must exist; a vanished target or destroyed required vehicle should fail clearly instead of silently advancing. Vehicle deliveries require the correct character aboard the actual mission vehicle. Aircraft landing objectives also require low height and speed. Land/water placements are resolved at runtime; the survey remains useful for verifying difficult terrain.

## Next Mission One pass — deliberately not included

- Preserve Guess's car seat when the mission takes over from the opening.
- Keep all three at their actual positions during recognition; remove the scripted regroup teleport.
- Replace the proposed drug deal with a visible activity or device that gives Gohan a reason to hack. Nearby dialogue should be in person; distant dialogue can be over radio or phone.
- Give Mateo's contact a meaningful action and stage Mateo's departure as an animated escape.
- Separate Ice's position from Guess's route and make the don't-shoot condition unambiguous.
- Apply the same spatial and conversational logic across the other scenes. Generic cutscenes already preserve positions, but each mission's explicit staging still needs live review.

No new Online vehicle importer is included in this batch.

## Verification and limits

The 86-source production DLL compiled against the project's pinned ScriptHookVDotNet reference. All 244 story/runtime checks, 80 behavior/recovery checks and 3 parser tests passed. The tests exercise production state transitions with GTA stand-ins; they do not simulate GTA physics, traffic, camera rendering or streaming. All 24 later missions complete in the automated flow harness, alongside the existing early-mission checks.

Mission lint reports no errors. Dialogue coverage is complete for 29 of 30 playable missions; M01's three legacy recognition cues are intentionally superseded. District validation flags 0 of 123 locations. District validation does not prove surface height, underwater clearance or vehicle navigation.

The reported crash left no definitive exception in the inspected mod and ScriptHook logs. This update is not a verified crash fix. Retesting in GTA is required.

Installation uses hash-checked backups and replaces only the mod DLL and changed runtime data. User configuration, appearance, survey coordinates and saves are preserved. Project source, prebuilt DLL and deployed DLL are synchronized. See the installation receipts for exact hashes and changed files.

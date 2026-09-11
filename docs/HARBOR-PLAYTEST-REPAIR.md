# Harbor playtest repair — one-sitting mission, water geometry, police and presentation

Base: `d295306764fa137ceda3e1f3741e2a0d766bdb45`, after the continuous-heist and desert work were combined on main. Work branch: `codex/harbor-playtest-repair`. Do not merge without the live acceptance pass. This report distinguishes source changes from live results; no GTA playthrough was performed in the implementation environment.

## Integration with current main

The comparison now includes main `f8a37b9837b11f6661df081fd59616b7c06e6bb3` through PR #32. M03-M08 and main-only placement/vehicle helpers are byte-identical to that main snapshot. Shared definitions retain both changes; the DLL is rebuilt from combined source. See HARBOR-INTEGRATION.md and its manifests. Main itself is not updated.

## Owner report and scope

Ron reported empty underwater welding/clamping space, a submarine released onto a freeway, unreachable yellow objectives, a missing expected boat, and a police helicopter not recognizing a plainly visible wanted player. He also requested a centered GTA-like passed panel, installed-game mission music while on foot, letter-style map starts, and verification of apartment variety.

The latest creative rule is one entire Port Heist attempt. No phase checkpoints, saved midpoint, chapter re-entry, or per-phase success. Fail, abort, death, quit or script unload means the next attempt starts at M19. Pausing and in-mission character switches do not end the attempt. The old M19–M22 classes remain internal implementation units and explicit developer diagnostics. M18 remains preparation; the required solo gate stays intact.

## 1. Marine geometry and worksite

The previous M18 setup validated a channel point but spawned the Kraken at an unvalidated offset. M19 also treated an unknown seabed as acceptable and spawned the bullion only when floating it. A marker and a narrative description did not guarantee usable physical space.

`MarineSites` now requires actual water height and completed map/object collision probes. A vertical first hit above water (including a pier/freeway deck), missing collision, shallow bottom, or blocked sampled footprint is not an acceptable marine placement. Estimates receive a bounded candidate search, at most 160 m; shore/pickup-sensitive sites use 40 m. A user-surveyed anchor is not silently relocated. An exhausted search fails with the exact key in `Bloodlines.log`, before placing the submarine on land. Setup-only synchronous probes are bounded and not run every frame.

This sampling is a preflight guard, NOT proof that the whole route is navigable. It does not certify every point between samples, stock underwater geography, camera framing, or live loading time. A rejected site needs a proper live survey, not a wider blind search or an unbounded relocation.

Changes:

- M17's three dry fitting/welding spots derive from the actual Kraken position and model dimensions instead of unrelated field points. These are dry-dock work points; the dock and walking clearances still need live confirmation.
- M18 creates the Kraken at the validated channel itself, submerged slightly, not at a new unchecked XY offset. A failed placement prevents setup.
- M19 uses one `M19.Worksite` anchor for its stationary **stock tug stand-in**, submerged bullion, accessible work lane, float points and return-to-surface point. It does not claim to create the novel's full-size freighter or a walkable hold. The `tug` is visible before work; the same bullion prop exists underwater before cutting and clamping, then is raised and attached to floats.
- Work markers sit in a lane beside the hull/cargo, with clearance based on the model bounds. They are not placed inside collision geometry merely to match a prose description. The rendered interaction remains a staged adaptation, not a physical cut through the tug collision mesh.
- `M19.BullionSurface` is a separate cargo point. Gohan's surfaced-sub marker no longer doubles as the helicopter's load position.
- M20 validates the escort launch point before creating it, rather than waiting for M21 to correct its location. M21 validates its marine endpoints; M22 validates the drop footprint.
- Runtime positions can be adjusted coherently, but personal INIs are not edited or shipped. `M19.Worksite` is now the anchor to survey for that group. The derived M19 work/cargo points are rebuilt together, so old independent derived-key overrides are not the authoritative group layout.

## 2. Underwater objective guidance

Submarine interactions use the craft center, not the pilot ped's displaced origin, for the same radius shown by the marker. Marine work draws a translucent yellow 3D sphere and explains horizontal distance plus `dive`/`rise` distance. The map target remains, but a submarine/boat objective does not generate a fictitious road GPS route. Multi-site work retains its remaining-site map blips.

The surface objective now requires the actual sub, the intended driver, horizontal arrival and vertical proximity to the surface. Being directly below the marker at depth no longer counts as surfacing.

Work-animation cleanup also remembers the actor who started it. Switching brothers cannot clean up the wrong person's task.

## 3. Police sighting

The prior correction searched a 70 m sphere only during grey stars. A helicopter 140 m directly overhead was outside that sphere. The revised reacquisition path checks grey stars, stars about to drop and a wanted-but-not-yet-seen state. Ground observation keeps the 70 m limit; a living police helicopter crew can be considered within 240 m in 3D and 170 m horizontally. A valid police model type or COP relationship group is required. Merely being in a helicopter does not make a civilian a police observer.

A real unobstructed entity LOS result is still required before `REPORT_POLICE_SPOTTED_PLAYER` and the wanted-center update. This does not raise the star level, report through roofs/tunnels, or report a nonexistent officer. Breaking sight leaves the game's search behavior alone. Diagnostics identify air/ground and the measured distance or blocked result. This is not a guarantee that every rendered searchlight represents a law-enforcement observer; a private Aegis helicopter can have different ownership. Test both stock police and the mod's actual dispatch.

## 4. Mission passed, soundtrack and map identity

`MissionPresentation` is a non-Script service. It draws a wide translucent panel, amber Pricedown `MISSION PASSED`, and the completed mission title in the center of the screen. It does not freeze the player, move the camera, save progress, or award rewards. The manager emits its presentation event after actual final completion; the Port Heist does not emit one for each internal section. The panel waits while an aftermath or pause owns the screen and expires after approximately 5.5 s of visible display.

On-foot mission gameplay requests installed GTA music via bounded `PREPARE_MUSIC_EVENT`/`TRIGGER_MUSIC_EVENT` calls, not copied music files. Default events are `DHP1_START`/`DHP1_STOP`; these are configurable under `[Audio]`. Entering a vehicle, a blocking scene, pause, death, mission end, or teardown stops the service's own score. It does not force a radio station or globally silence other audio. A missing/rejected event logs and continues. An accepted native request is NOT proof of audible playback; event availability, transitions and the user's music volume need live testing. No Rockstar soundtrack bytes are distributed.

Normal mission map starts use stock B (Bloodlines), S (solo) and H (Port Heist) glyphs with mission names. The ground cylinder remains the usable activation area. This is letter-style map navigation, not a custom font/texture pack or arbitrary letter rendered onto each street marker.

## 5. Apartment audit — preserve existing work

`ApartmentTiers.cs` and `CrewHomes.cs` are already on the base branch:

- Starter: Ice's Little Seoul studio, Gohan's Richards Majestic one-bedroom, and Guess's Forum Drive house each have a separate interior key.
- Luxury after M27: separate Eclipse penthouse floors (`apa_v_mp_h_01_a`, `_b`, `_c`), but the same style family. Different floor coordinates do NOT establish three wholly distinct floor plans or decoration themes.
- Top after M47: one shared Diamond penthouse with requested furnishing entity sets. The source labels it two floors; this inspection does not certify the accessible layout in GTA.
- Interior entry uses loading/collision checks. Wardrobe/bed/locker room spots activate independently only after their keys are surveyed; otherwise the entry service menu is the honest fallback.

This pass does not silently replace those residences, invent verified furnishings, change apartment unlocks, or add custom interiors. The remaining question is live correctness and how much unique luxury-room variety Ron wants beyond floor separation.

## Acceptance on a disposable copy, matching DLL and data

1. Record installed commit/hash, actual `Bloodlines.ini`, `Bloodlines.Locations.ini` and `Bloodlines.Surveyed.ini`. Preserve personal files. Start M17/M18 to verify the dry work and actual channel launch, then start M19 normally with real prerequisites and solos satisfied. Explicit developer phase starts are not the continuous-heist test.
2. Verify the tug, submerged bullion and every work sphere exist before cutting. Pilot to each marker without clipping into hull, container, pier or seabed. Verify distance/depth labels agree with the real craft. Follow the surface marker upward.
3. Lift the SAME surfaced cargo. Confirm actual launch and crew transfers, escort, coast/road pickup, inland flight/drive, valid drop, landing and full regroup. Run watched, skipped and interrupted scenes on separate attempts. No mission phase can succeed merely because a scene was requested.
4. If a marine preflight refuses, record the exact key and position; capture a genuinely open water anchor and correct depth. Do not mark the mission passed, disable the guard or replace a personal survey with a made-up coordinate.
5. Fail late, abort and reload separately. The next normal attempt must start M19, never the escort. No interim checkpoint, rewards or saved cargo history may leak. Old mid-heist saves retain earned ownership/rewards but cannot become a new resume feature.
6. With one/two/three stars test stock foot/car police and an overhead police helicopter at 70/140/220 m. Test visibility, roof/bridge occlusion, entering a tunnel, losing sight, switching brother and reacquisition. Police must be escapable; the HUD must reflect real engine sighting, not a cosmetic forced indicator.
7. Complete an ordinary mission and the complete heist: one final centered banner, no false result on fail/cancel, no frozen controls. Test 16:9 and ultrawide, subtitles, pause and aftermarket HUD mods.
8. Test on-foot mission score, vehicle entry/exit, dialogue/cutscene, pause, failure and unload. Set `[Audio] MissionScoreEnabled=False` to opt out. Report the accepted event and audible result separately.
9. Inspect B/S/H starts on both radar and map. Names remain correct, prerequisites/gates unchanged and ground activation still works.
10. Enter each starter home as its owner; inspect furnishings and exit. Preview luxury/top with QA only without granting ownership. Check actual rooms rather than accepting floor-count metadata as proof.

## Verification status

The implementation report generated after CI records the actual checks and matching DLL checksum. Until that exists, this is changed source awaiting compilation. Unit tests simulate water columns, LOS and tasks; they cannot establish world geometry, animated boarding, real music audibility or frame rate. Do not reuse the old 1,365 checks as evidence for new source.

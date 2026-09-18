# The interface pass, September 17, 2026

Ron asked what would make the interface more engaging and chose four of the six: a mission
HUD that shows state, a results card, the car page with a silhouette and drawn bars, and a
title card at the start of gameplay. This is what was built and what is still a live question.

## 1. The mission HUD shows state

`Core/MissionHud` replaces three lines of yellow text. It is composed every frame from the
running mission - `MissionManager.CurrentObjectives` and `CurrentStageName` are the seam,
read off the live `MissionStage` - and draws:

 * the heading: mission id, title and stage name;
 * **a chip in the owning brother's color and his name**, with "switch" when the beat is not
   the brother being played;
 * the distance to the objective's `AssignmentPosition`, where it has one;
 * the objective, wrapped to three lines, with the stage's passive rules on a line under it
   rather than inside it;
 * a progress bar for a gauge (its value in its unit, green when in the band), an aligner
   (signal strength) or a multi-hold (sites done of total);
 * **a timer bar with the time on it, red under thirty seconds.**

That last one fixes a defect: `TimerObjective` pushed its clock as a subtitle, and
`ComposedMission` pushes the objective text into the same subtitle slot every tick, so the
clock lost every frame. **M55's five minutes never showed.** The timer no longer pushes a
subtitle; the HUD owns it.

`MissionHud.Compose` returns a `Frame` a test can read without a screen. The story suite
drives a mission with a gauge, a timer and a rule through it.

## 2. The results card

`Core/MissionTally` adds up one attempt from what the mission already owns, once a tick,
reading only: elapsed time, hostiles dead among the mission's staged peds (by relationship
group), headshots through `GET_PED_LAST_DAMAGE_BONE`, switches, damage off the man in play,
and the payout from the cash before and after the reward commits. `MissionManager` opens it
when gameplay begins, ticks it while the mission runs, closes it on pass and drops it on
failure. `Passed` now carries the result, and the passed panel draws it under the title for
nine seconds: TIME, KILLS (headshots), CREDITED, SWITCHES, DAMAGE TAKEN, PAYOUT, CAMPAIGN.

**A kill is credited to whoever was in play when it was noticed.** That is an honest
approximation and the panel says "credited", never "scored". A replay says "replay - no
payout" rather than showing a zero. Damage is never charged for the baseline sample after a
switch: the new brother's health is his own.

## 3. The car page: a silhouette and bars

`PhoneEntry` gained `Art` and `LiveBars`. The phone's reading pane draws the art at the top
and the bars under it, in the owner's color, before the text - through the same `sprite` and
`box` callbacks the home screen already uses, so no new drawing path exists. The car page's
text now carries only what a bar cannot (`VehicleSpecs.Facts`: mph, seats, built figures);
the four ratings are `VehicleSpecs.Fractions`, drawn as fills rather than as
`[=====.....]` characters.

**Ten silhouettes ship, one per catalog class, not one per model.** `assets/ui/phone-car-
<class>.png`, 228 by 80, white on transparent, named by `VehicleSpecs.ArtFor(category)`.
They are drawn polygons - placeholders that make the pipeline real end to end; better art
drops in over the same file names. A story test refuses a catalog class with no silhouette.

## 5. The title card

`MissionManager.Started` fires when gameplay begins - after the briefing, never over it -
and `MissionPresentation.QueueTitle` draws five seconds of: the mission id and act, the
title in the game's own display face, the context card's target line, and who is on it.
Blocked frames (a scene, the phone, the death handler) hold it rather than burning it.

## Not built, by Ron's choice

4 (a crew strip) and 6 (dispatch toasts) are in `docs/ACT3-AUDIT-2026-09-17.md`'s proposal
and were not chosen this pass.

## Later the same day

Ron's first look at the HUD: too big, taking up too much of the screen, the information
solid. Every size in it is about two thirds of the first pass now (400 wide, text .18 to
.24); see `docs/CREW-ORDERS-2026-09-17.md`.

## Live questions

Nobody has seen any of this on a screen. The HUD's panel height is computed from its lines;
whether it sits clear of the game's own HUD at every aspect ratio is a look. The card and
panel share the vertical band the old "MISSION PASSED" used. `GET_PED_LAST_DAMAGE_BONE`
answering a head bone for a dead ped is an assumption about the engine; a zero there costs
nothing but the headshot count.

## Checks

Build clean, 4,946 story checks, 250 regression, lint no errors, 0 of 1,091 flagged,
mission map and story freshness verified, American English clean.

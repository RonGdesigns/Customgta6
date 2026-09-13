# Harbor playtest repairs - September 12, 2026

This package follows the installed M06/M08/M09/M10 repair build. It addresses the M12-M18 reports and the empty welding scene in M11. No saves, character appearance, configuration or personal survey files are overwritten. No repository changes have been committed or pushed.

## Changes

### M11

Guess works at a visible engine module resting on the workbench. The opening walks him beside that bench and welding faces the equipment. The player then fabricates the mounts with an interruptible work animation. Ice operates the controller-friendly 20-30 PSI calibration; Gohan brings the harbor findings afterward.

### M12

Gohan begins aboard the Kraken in separately authored, footprint-validated water beside the south quay. Ice watches from the quay and Guess holds his own station. Descend to the sonar mark, perform the hull scan without detection, then surface at the explicit KrakenReturn water marker. KrakenSpawn, return water, both patrol launch spawns and their routes are separately editable in Survey all.

### M13

Ice starts on the water scooter in the open marina basin. Three stationary fuel tugs follow the diagonal channel, separated by more than a hull length; all nine points of each model-sized footprint must have five metres of water. Plant beside each hull, escape the alarm launch to the slipway, board the Granger and detonate. The launch and alarm entry have open-water positions; dock workers remain at explicit posts. Failed water preflight names the exact key and refuses a land spawn.

### M14

Ice clears the apron from the ridge. Shooting alerts the guards and replaces idle guard tasks with combat. When Guess reaches the hangar entrance, three additional enemies emerge from positions inside the real hangar. They are not part of the earlier kill gate: Guess may fight or board the Besra under fire. Fly low to McKenzie and secure the pod. Individual apron and hangar enemies are editable.

### M15

Reach the maintenance access at a visible service office on the port apron. Ice stuns three watchmen at explicit posts clear of the freight containers. Their initial health prevents a normal stun hit killing them; a recorded stun hit remains valid after the brief engine stun flag disappears. They are kept alive, cuffed and down through the rest of the attempt. A dead guard still fails. Gohan works at the cable cabinet on the office wall, with a timed interaction facing the cabinet; the splice shot no longer welds empty air. Leave via Guess at the exit.

### M16

The crew starts together in its car on the Great Ocean Highway bridge outside Zancudo. Ice approaches and clears the hangar; the Cargobob stands at a collision-checked centre position facing the open mouth. After switching to Guess, Ice starts boarding while Guess approaches. Gohan takes the car by road. A stopped empty passenger seat has an eight-second recovery if Ice cannot finish his entry. Projectile clearing around this helicopter covers departure only: it ends at 1.1 km from the hangar, after 90 seconds, or upon reaching the pursuit-loss stage. The vanilla armybase script is not terminated; wanted pursuit continues. Cleanup restores the previous wanted maximum.

### M17

The Kraken starts afloat beside a reachable Elysian quay workshop. Gohan prepares three physical torch modules at workbenches; each completed module is transferred collision-free to the sub. Guess tests the magnetic lock at the third station. Gohan configures its external emergency release at a separate reachable workbench. This is a grapple release, not a crane or submarine launch. Saved work points are no longer replaced by hull-derived coordinates. Complete the radio check to record the ready sub.

### M18

Gohan drives the Kraken from its own water spawn to the channel mark. Guess flies the Cargobob from its separate staging origin to a clear apron, gets out and installs the visible jammer crate. Ice drives the hauler roughly 300 metres, parks, and loads the marked launcher crate using an interruptible animation. Mounts must attach successfully to count. Inactive brothers retain mission ownership and parked vehicles hold their places; selecting them releases their vehicle for control. The final radio check boards Guess and Ice into their own vehicles before the shots. Cleanup releases ownership and all mission-owned vehicle freezes.

## Placement evidence and limits

The read-only CodeWalker passes inspected the installed Enhanced collision archive, water bounds and model bounds. Failed candidates were replaced before packaging: shallow marina edges, a crate under a workbench, and containers inside helicopter footprints. The selected M16 hangar centre has about 29 metres to its closest sampled wall and an open southwest mouth. The final tug checks use their actual 7.2 by 19.1 metre half-footprints, rotated with the channel; every sampled point is deeper than five metres. Marine runtime preflight remains enabled. The reader output stays under build/codewalker-harbor25-* and is not distributed with the mod.

Positions remain marked estimate until an in-game survey. Static collision checks cannot prove streamed navmesh, animations, GTA vehicle handling or AI in a live playthrough. The Survey all menu automatically lists the new Kraken spawns, patrol routes, guard posts, work positions and staging vehicle origins. M11 and M17 benches also have explicit prop controls. Attached parts follow their vehicle; those are attachment offsets rather than free-standing map placements. This pass covers the reported harbor missions, not an assertion that every prop throughout the unfinished campaign is already editable.

M16 uses a bounded, local departure countermeasure rather than changing version-dependent armybase script memory. GTA's armybase script creates dedicated aircraft/missile attackers independently of ordinary wanted suppression. The local projectile-clearing native leaves the vanilla script and normal wanted pursuit intact. References: [armybase implementation](https://github.com/root-cause/v-decompiled-scripts/blob/master/re_armybase.c), [Cfx native definition](https://github.com/citizenfx/natives/blob/master/MISC/ClearAreaOfProjectiles.md).

## Retest in order

1. M12: confirm the sub starts afloat, complete the dive/scan/return, and confirm KrakenSpawn appears in Survey all.
2. M13: confirm all three fuel tugs and the scooter spawn in water; plant beside all three and finish the escape/detonation.
3. M14: shoot from Ice's ridge, observe return fire, approach the hangar as Guess, and board without killing the new defenders.
4. M15: stun each guard once, wait fifteen seconds, switch to Gohan and work at the office cabinet. All guards must remain alive and down until the attempt ends.
5. M16: drive in from the bridge, clear the hangar, switch to Guess and check Ice boards during the approach. Fly out through the hangar mouth and stay low. Countermeasures must end outside the base or after the departure window; abort/retry must restore normal wanted behavior.
6. M17: walk to every bench without using teleport. Confirm three modules appear on the afloat sub, test the lock, configure the release and finish the radio check.
7. M18: none of the three delivery legs should complete at spawn. Switch away and back during each leg; control must return and the others must stay with their assigned assets. Fit the jammer, load the launchers and finish the seated radio check. Retry once to check all state resets.
8. M11: inspect the opening engine/bench scene and the timed work animation before the PSI test.

## Automated validation

2228 story/runtime checks, 189 general regressions and 3 parser tests passed. The production DLL compiled against ScriptHookVDotNet3 3.6.0. Mission lint reported no errors; district validation reported 0 of 295 flagged. These are automated checks, not a live GTA playthrough.

## M13 follow-up

The September 13 live test still failed the first barge preflight. The follow-up adds bounded nearby recovery, full-footprint streaming retries, real-size test hulls and specific rejection diagnostics. See [M13-STARTUP-FOLLOWUP.md](M13-STARTUP-FOLLOWUP.md). Live verification is still required.

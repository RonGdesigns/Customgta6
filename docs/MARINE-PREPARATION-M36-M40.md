# Marine preparation — M36–M40

September 13, 2026. Implemented following the user's request to take the next steps after M31–M35. **46 missions now have runtime scripts: M01–M40 and SM01–SM06.** M41 onward and SM07–SM09 remain plans. The package includes the prior M13 startup repair, whose live acceptance is still pending.

## Play order and controls

Normal progression offers M36 after M35. The debug mission list can start each chapter independently. Work objectives show the responsible brother, target and progress bar. Press **E / D-pad Right** at the target to begin; leaving range or moving a vehicle during stationary work resets that interaction. The usual character wheel handles required role changes. Combat sections permit the available brothers to fight, with mission AI maintaining the others' assignments.

Underwater work stays inside the sub. A three-dimensional sphere and horizontal/depth guidance show where its center needs to be; reaching the correct map X/Y while floating above the target is insufficient. These missions do not require an unprepared breath-holding dive.

| Mission | Actual flow | First-completion cash |
|---|---|---:|
| M36 — Deep Well Recon | Gohan scans three visible seabed sensor housings from the sub. A surface patrol approaches the first pickup. Guess moves the dinghy to the alternate pickup; Gohan surfaces beside it with the completed survey. Ice watches the shore. | $85,000 |
| M37 — The Grapeseed Harvest | Guess steals and lands the first Duster at Sandy Shores. Ice clears the second strip, steals the second Duster and lands it at a separate apron space. Gohan fits both kits and verifies two visible smoke releases. | $140,000 |
| M38 — Blood in the Quarry | Guess brings the Benson into the loading yard under fire as moving cover. Clear four guards, use Gohan at the stock-control cabinet, then carry and stow each of four charge packages. Board all three, escape the response, lose pursuit and deliver/inspect the same loaded truck. | $160,000 |
| M39 — The Paleto Cable | Gohan follows the underwater route, attaches a cutter to a visible cable junction, then runs and verifies the cut. Ice clears the resulting shore response. Gohan returns in the sub while Guess holds the pickup. | $100,000 |
| M40 — The Phantom Rigging | Guess fits two hull kits at the open cove. Ice hits both floating practice targets with a carried gun. Gohan verifies navigation on a shore laptop. Guess and Ice each take a loaded Tropic through its trial course and return it to its own cove position. | $125,000 |

The five jobs pay **$610,000 once**, bringing total first-completion cash across all scripted jobs to **$3,347,000**. Replays remain unpaid. Existing mission payouts and weapon unlocks were preserved.

## Story and scenes

M36 supplies an approach survey; it does not declare the offshore fortress ready to assault. Its three targets are actual sensor housings placed on the seabed, and the patrol is a real boat with a driver. The first pass takes place on accessible coastal geometry. The drilling fortress, its structural columns and depth-charge machinery have not yet been constructed, so neither the objectives nor the dialogue claim that the player visited those unseen structures.

M37 turns the survey into a tested smoke-screen capability. The planes start separately on the open Grapeseed strip and must both land. Four canisters attach to the two aircraft; fitting alone is not success. Each release must actually start its smoke effect. The non-looped effect comes from the game's stock `core` dictionary, verified against the [extracted particle-effect list](https://github.com/DurtyFree/gta-v-data-dumps/blob/master/particleEffectsCompact.json) and compiled against the pinned ScriptHookVDotNet API. Smoke is an optical effect, not radar invisibility or guaranteed AI blindness.

M38 acquires the demolition stock measured by the survey. Guess drives the carrier into the firefight before the loading sequence. The player can clear the yard as Guess or use Ice. Gohan operates the cabinet, takes each package in hand and stows it in the Benson; each package counts only after its attachment succeeds. The truck and all four attachments must survive to inspection. The response retreats after the quarry escape point so the delivery is not an endless scripted tail.

M39 separates attaching the cutter from completing the cut. A clamp attaches to the real junction; the following timed operation verifies that it is still attached before reporting success. Only then do four guards arrive at the return shore. Cutting the link is a recorded story consequence; it does not disable all native police, radio communications or military response globally.

M40 uses an open cove with two floating Tropics, shore kits, a table/laptop and physical practice targets. It does not add a walkable cave, ballistic-glass model or mounted boat turret. Ice tests carried weapons for passenger defense. Each trial boat receives 200 extra maximum/current entity health and 200 body-health points when its kit is fitted; bullet, fire, explosion and collision damage remain enabled. Both loaded boats must complete a course and return. The brothers discuss Bradley's access card only after this result.

Establishing shots show the real props and vehicles at their current locations. Remote brothers use the radio; scenes do not assemble them in one place for conversation. Interactions use existing work animations, attached props and normal boarding. Watching and skipping scenes must reach the same required physical state. Voice recordings are not included.

## Unlocks and persistence

M36 records `offshoreApproachSurvey`. M37 records the smoke aircraft and unlocks **free-roam Duster smoke**: while piloting a Duster, press **E / D-pad Right** to release a cloud. It recharges for 12 seconds after a successful release. The input is blocked in debug menus, the character wheel, missions, the surveyor and the prologue. A failed effect request does not consume the release.

M38 records delivered seismic charges. M39 records the cut mainland cable. M40 records two tested extraction launches. These acquisitions are available for later mission prerequisites and scene logic; they are not passive income businesses or global world-defense switches.

The delivered aircraft, boats and loaded truck are released for continued use, with fitted canisters/kits/packages retained rather than forcibly deleted during mission cleanup. GTA may eventually reclaim released world entities. **Persistent hangar and boat-berth ownership is still planned**; this update does not save every aircraft or boat into an invisible garage. The M40 reinforcement applies to its two prepared trial boats, not every Tropic in the world.

## Failure and retry

All five chapters restart from the beginning after failure; no partial-stage checkpoint is advertised. The shared mission manager discards provisional cargo, evidence and upgrades on failure/abort and commits rewards on first success. Startup rejects missing essential vehicles/equipment and reports the failed location or asset.

| Mission | Failure checks |
|---|---|
| M36 | Required crew, sub, pickup or sensor lost. The surfaced sub remaining within patrol sight/range for eight seconds fails; diving or clearing the patrol resets exposure. No detection timer runs before the patrol exists. |
| M37 | Either aircraft or fitted canister lost, attachment breaks, or either smoke release fails. A flyby does not count as landing; the plane must be low and stopped at its own apron position. |
| M38 | Crew/truck/package lost or any loaded package detaches. Passenger boarding has the existing required-seat and bounded nearby-boarding failure messages. Delivering another truck cannot satisfy the objective. |
| M39 | Required crew, craft, junction or cutter lost. A detached cutter cannot complete the cut. The crew must clear the shore response and return before receiving the final consequence. |
| M40 | Required crew, boat or hull kit lost. One weapon target is insufficient; both must register Ice's hits. One sea trial is insufficient; both loaded boats must return with attached kits. |

## Placement and validation

**77 new named positions** are available through the mission surveyor. Read-only CodeWalker checks used the installed Enhanced collision and water data, with **101 final samples** including 24 aircraft/truck footprint corners. The first estimated aircraft point under a structure was replaced with open-strip positions. Sub work points sit above visible seabed equipment at reachable depths. All 389 pre-existing source location rows and the user's surveys were preserved.

Offline geometry does not establish live navigation, camera framing, boat boarding, traffic, collision streaming or smoke appearance. New positions retain estimate status until played/surveyed. Raw map reports remain under `build/codewalker-marine-28` and are not shipped as Rockstar assets.

Automated walkthroughs cover all five completions, physical cargo/canister attachment, two separate aircraft landings and releases, both weapon targets, two boat trials, exposed-sub failure, detached-cutter failure, missing assets, reward totals and replay protection. Existing story/runtime and regression suites run alongside the release build and data checks. These use GTA stand-ins; live acceptance remains pending.

Recommended first live pass:

1. **M36:** Check sub spawn and depth guidance, each sensor's visibility, the patrol route, alternate pickup and eight-second surface warning.
2. **M37:** Check wing/runway clearance, both landings, Gohan's access to each fitting point and two visible smoke releases. Then try free-roam smoke after completion.
3. **M38:** Drive the truck into the fight, check both allies return fire, carry all four packages, board the rear cargo seat and inspect the delivery.
4. **M39:** Check junction/clamp visibility, both work phases, shore enemies returning fire and the sub's return.
5. **M40:** Check shore-kit access, hitting both practice barrels, normal Tropic boarding, separate trials and both boats still present at the cove.

Watch and skip an opening, then deliberately fail one chapter and retry. Include the mission, objective label and any edited survey key when reporting a problem. The next planned chapter after this package is M41, followed by the aircraft/submarine delivery prototype and the offshore staging/heist work.

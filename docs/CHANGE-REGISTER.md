# Bloodlines — Change Register

**Status:** Living reconciliation table, maintained by the implementation agent.  
**Companion to:** `AGENT-HANDOFF-PROPOSED-CHANGES.md`, `CAMPAIGN-IMPLEMENTATION-AUDIT.md`  
**Branch:** `claude/handoff-implementation-pass` (September 9, 2026)

This is the table the audit asked for (§17): one row per proposal item, what the
code and data actually say, what was decided, and whether the game has confirmed
it. "Live verified" means played in GTA V Enhanced on the installed build. Nothing
in this pass has been live-verified yet; the source-level checks that did run are
listed in `HANDOFF-IMPLEMENTATION-UPDATE.md`.

## Source priority applied

1. Latest explicit user-approved creative decision (the handoff's locked wording).
2. Current working implementation where it does not conflict with that decision.
3. Latest authored mission/dialogue data (`story_beats.txt`, `opening_scene.txt`, `dialogue_edits.json`).
4. Proposal documents.
5. Older bible/PDF descriptions.

## Register

| Mission / system | Current implementation (this branch) | Current authored data | Approved intended change | Old proposal / bible conflict | Live verified? |
| --- | --- | --- | --- | --- | --- |
| Prologue (LSIA → home) | `PrologueSequence`: Guess deploys solo at `Prologue.LSIACurb`, arrival scene with phone/walk/enter-car blocking, player drives to the starter-apartment entrance, homecoming scene (exit car, walk to door, read the job), fade and time-cut to Terminal Island, then M01 starts. Hold Backspace skips and still marks it played. `CampaignState.PrologueComplete`; saves that already finished M01 are exempt. | `M01:prologue` (3 lines) and `M01:arrival` (3 lines) in `scenes.tsv`, all GUESS. | Ron is the first point of view; physical walk and vehicle entry; control returns seated; drive home; exit and job setup; transition into M01. | Older proposal assumed Burro Heights exterior because apartments were thought exterior-only. The audit (§9) reversed that: the implemented starter apartment is the destination. Burro Heights remains a fallback through `CrewHomes.Position`. | No. Needs: LSIA curb/car coordinates, walk path, car entry animation, apartment marker arrival, M01 placement after the time-cut, watch vs skip. |
| M01 vehicle | `schafter3` (four-door) in `CutsceneDirector` and `M01GhostInTheDockyard`. | Bible text says "prototype supercar"; authored lines say "four-door prototype". | At least four seats; no two-seat T20. | Bible's T20 wording. | Resolved in an earlier pass; unchanged here. |
| End of implemented content | `CampaignState.Progress()` distinguishes story available / side content only / story blocked / all implemented content complete. Mission key and menu report the state; nothing falls back to M01. `MarkComplete` logs the end of content. | — | Separate states, never M01 as a fallback. | Audit §4.3 described a fallback that the audited snapshot no longer had; the state reporting was still missing and is now explicit. | No (source test only). |
| Retry after failure | Mission key prefers `LastAttempted` while `RetryAvailable`; a nearby marker explicitly chosen overrides; the player is told which job is retrying. | — | Direct retry of the failed mission before ordinary selection. | — | No. |
| Rewards / replay | All cash, gold, safehouse and fleet flags commit once in `CampaignState.MarkComplete` → `AwardCompletion`. M22's foundry loss and gold reset commit only on first completion. Story-test proves M24 twice pays once. | — | Idempotent, first-completion-only story mutations. | Audit §4.1/4.2 described stage-time awards; those were already moved before this branch. | No. |
| SM01 ammunition | New flag `armorPiercingSupply`; `WeaponProgression.RestockCount` doubles Ice's rifle restock at home lockers. SM01's completion subtitle says exactly that. | SM01 outro unchanged. | Decide whether the AP ammo is real. Decision: a locker-restock supply line, Ice only. | Bible implied a persistent ammo type the engine does not expose. | No. |
| SM03 racing transmission | `FleetGarage.FitChopBay`: vehicles repaired at Guess's chop bay get the race transmission mod once each. Wired through `CrewHomes.ApplyFleetUpgrade`. | — | Trace the flag; implement, describe honestly, or remove. Decision: implement at the chop bay. | Flag existed with no consumer. | No. |
| Mission-issued weapons | `WeaponProgression.Update(crew, captureAllowed)`; ownership capture is off while a mission runs, so mission loans never enter the locker. | — | Classify loans vs permanent; milestones must not be pre-empted. | — | No. |
| Pre-reunion free roam | Deploy key before M01 completes deploys Guess alone (`DeploySolo`); dev builds keep the full sandbox. | — | Story players cannot field the trio before the reunion. | — | No. |
| Required assets | `Mission.RequireAsset(entity, reason)` fails the attempt the tick a required vehicle/prop is lost, independent of which stage is protecting it. Applied to M19 Kraken, M20 Cargobob + container, M21 launch + Cargobob + container, M22 Cargobob, M24 crane. | — | Every required asset gets fail / respawn / alternate / invulnerability. | — | No. |
| Port Heist continuity | `OperationHandoff` / `HandoffLedger` on `MissionContext`. M19 records the surfaced Kraken → M20 puts Gohan in it; M20 records the loaded lift → M21 starts the Cargobob where the climb-out ended **and always attaches a visible container**; M21 records → M22 consumes. Records live in memory only; a restart falls back to default staging with a log line. | — | Preserve or reconstruct the same apparent state; never escort an empty lift. | — | No. Needs: attached container under an AI-flown Cargobob at 45 m, escort behavior. |
| Cutscene motion | `SceneBlocking` steps: WalkTo, EnterVehicle, ExitVehicle, UsePhone, LookAt, Wait, with timeouts and `Finish()`. `CutsceneDirector.Play(..., blocking)` leaves movers unfrozen, tracks the current subject, holds the scene until blocking finishes, and completes remaining steps on skip. | Prologue/arrival scenes use it. | Reusable moving-scene helpers; skip ≡ watch. | — | No. Needs: GoTo/EnterVehicle task behavior at the curb, camera clipping. |
| Gohan decision mechanic | `TechnicalChoiceObjective` (cycle with G / D-pad Left, commit with E / D-pad Right). M28: which relay system to cut first changes response delay and squad size. | M28 gameplay lines unchanged. | A small set of reusable Gohan decisions rather than timers. | — | No. |
| M02–M07, M10, M14, M16, M27, M30, SM02, SM03 dialogue | `dialogue_edits.json` revised; `dialogue.tsv` regenerated. No "switch to", markers, button names, radii or "no ability needed" in speech for those missions (story-test enforced). | Regenerated. | HUD = mechanics, dialogue = people. | Bible's tutorial-style lines. | No. |
| M05 reveal | Mateo: three contracts, "they wanted all three of you there", city money behind it, no full conspiracy. Ice: "Then we find out who." | Outro already said the accusation is not proof. | Allegation and lead only; M46 stays the hard-evidence payoff. | Bible's forty-million-dollar contract reveal. | No. |
| M10 | Locked: "I joke when I'm nervous too. Learn the difference." | `M10_SCENE_OUTRO_02_GUESS` | Locked wording. | "scared". | No. |
| M25 | Locked: "…hear the pressure in my voice. Next time, I'll make the call." | `M25_SCENE_OUTRO_01_ICE` | Locked wording. | "hear me scared". | No. |
| M42 | Locked: "Sub deployed. That one had my nerves up. Nobody put that in the flight log." | `M42_SCENE_OUTRO_01_GUESS` | Locked wording. | "I'm still shaking". | No. |
| M17 | "External release, where either of you can reach it." | `M17_SCENE_OUTRO_01_GOHAN` | Remove "marked yellow" from speech. | — | No. |
| M22 strike limitation | Gohan: the strike came off a contract airframe with a city permit and their address on file; Aegis spent the one shot it can explain, the next needs a target it can prove. | `M22_SCENE_OUTRO_01_GOHAN` | World logic for why the strike cannot simply repeat. | — | No. |
| M26 aircraft | Guess: the Lazer towed out of Zancudo under the M16 clearance, fueled at McKenzie. | `M26_SCENE_INTRO_01_GUESS` | Establish acquisition/storage or change aircraft. | Aircraft appeared only because the mission needed it. | No. Mission code still spawns the Lazer at the airfield; a visible hangar aircraft between M16 and M26 is future work. |
| M45 | "Helipad's secure. Gohan, I need Bradley's card up here. Guess, how's our bird?" | `M45_SCENE_OUTRO_01_ICE` | Conversational, not commander. | — | No. |
| SM05 / SM06 / SM07 / SM08 aftermaths | Rewritten to the proposal's short forms (Ice's "come eat before Ron starts calling"; "You actually slowed down."; "Sterling's dead." / "You good?" / "Nah."; "Files are out… You coming back?"). SM08 reframed to communication, not belonging. | Regenerated. | Quieter, brotherly, no therapy language. | Previous authored versions over-explained. | No. |
| M70 ending | Quiet boat: names in words, Gohan distracted by a pinging relay, the phone joke, breakfast. | `M70_SCENE_OUTRO_*` | No confession scene. | Previous authored confession. | No. |
| M33 trigger | Not changed; M33 has no script yet. Register note: the code acquisition trigger must follow Ramos's stabilization objective. | — | Check the trigger when M33 is scripted. | — | n/a |
| Cypress destruction presentation | Not changed. `M22` commits the safehouse loss; no persistent damage props yet. | — | Decide post-M22 presentation. | — | Open. |
| Checkpoints | Unchanged: `SupportsCheckpointRestore=false` everywhere; retry is a full restart. | — | Do not enable until reconstruction exists. | — | n/a |
| Companion death policy | Unchanged: any hero named by a stage objective is mission-critical; `ComposedMission` fails on their death. Free-roam recovery is suspended while `MissionActive`. | — | Declare per mission. | — | Open: no per-mission "recoverable" tier yet. |

## Economy ledger (audit §13)

One vocabulary for every form of wealth, so no mission can call the same score
three names:

| Asset | Save field | Set by | Consumed by |
| --- | --- | --- | --- |
| Physical bullion taken | narrative: 30 t | M20 lift | — |
| Physical bullion hidden | 30 t minus `alamoGoldDredgedTons` | M22 drop (`AlamoGoldDredgedTons = 0`) | M24 (+5 t) |
| Physical bullion recovered | `alamoGoldDredgedTons` | M24 | future Act II/III conversions |
| Liquid operating cash | `cashOnHand` | first completions (M05 … SM06), shops | shops, customization |
| Bearer bonds / rig loot | not yet a field | M46 (unscripted) | — |
| Electronic / offshore | `offshoreEscrowBalance` | none yet (zero in playable chapters) | — |

The "$3 billion" in M11 is a manifest claim, not spendable money, and stays that way.

## Antagonist and support presence map (audit §16)

| Character | First mention | First appearance | First action that hits the trio | Mid-campaign reminder | Final payoff |
| --- | --- | --- | --- | --- | --- |
| Mateo | M01 intro | M01 (clipboard, dock) | Runs at recognition; his ledger is the lead | M05 capture and allegation | Names the contracts; his word starts the hunt, M46 finishes it |
| Miller | M03 outro | M04 | Sells the dock forensics | — | Drive points to Mateo's cave |
| Vance | M08 outro (shared surname) | M09 convoy (unseen) | Convoy IFF is the crew's way in | M16 burns the clearance; M26 charters | M65 confrontation (unscripted): must attack something specific about the trio |
| Ramos | M32 (unscripted) | M32 | Rescued as a person first | M33 stabilization before codes; M35/M43 cited | Codes open the rig; stays a person |
| Harrison / Bradley | M44–M46 (unscripted) | — | — | — | Bradley's card opens the helipad route (M45) |
| Sterling | SM07 (unscripted) | — | Personal to Ice | — | SM07: "Sterling's dead." / "You good?" / "Nah." |
| KJ | SM03 intro | SM03 | Watches the rivals' line | — | SM09 (unscripted): warns Ron to keep a backup exit |

## Continuity flags carried forward

- M39 rig communications: choose cut-to-backup (slower, noisier) over flatlined; enemy behavior must follow. Not yet scripted.
- M51 blackout timing: choose one of blackout now / test outage / charges staged; the tower sequence must respect it. Not yet scripted.
- M60 civilians: any large explosive event needs evacuation or placement justification. Not yet scripted.
- Evidence chain: M05 allegation → verification → M46 proof → M59 publication → later records deny containment. M05 now conforms.

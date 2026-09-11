# The Port Heist — continuous-operation comparison

Base: `bb073aa9b73d4cb5421c922ad01f0e85d402bb3b`.
Built on `codex/continuous-port-heist`; merged onto main on September 11, 2026 after Ron chose the continuous operation over chapters with checkpoints. The live checks below are still owed; the branch's preview workflow was not carried onto main.

## The approved change

Normal story play starts **The Port Heist** once. Underwater Breach (M19), Sky Hook (M20), Open Water (M21), and Scorched Bay (M22) are internal phases of one operation. The phase scripts and original IDs remain intact for authored dialogue, prerequisites, existing saves and explicit QA starts. M18 remains preparation; the SM01–SM03 gate before M19 is unchanged. Later operations and the weapon-market redesign are outside this branch's scope.

`PortHeistOperation` owns the four phases, `PortHeistWorld` owns their shared live entities, and `MissionManager` owns the one outer mission session. Finishing an internal phase does not call ordinary mission Start/Finish, end its weapon loan, clear pursuit, reset the clock/weather, redeploy the crew, or award a separate mission pass. The final M22 result commits the remaining phase IDs once and produces one Mission Passed and aftermath.

## Live continuity versus retry reconstruction

| Boundary | Normal uninterrupted operation |
| --- | --- |
| M19 → M20 | The original three peds, surfaced Kraken, staged Cargobob, bullion and floats remain live. Gohan is still in the sub. |
| M20 → M21 | The same pilot, aircraft, attached container and crewed escort launch are borrowed by reference. No detach/recreate on the boundary. |
| M21 → M22 | The loaded aircraft remains where Ron actually flew it. Ice/Gohan stay in the same Granger on the coast. Ron flies inland while the road team drives to the Alamo. The short beach-arrival scene starts when their vehicle reaches its road approach. |
| Operation end | The deposited container, landed aircraft and extraction Granger are retained; temporary tasks, holds, blips, unused enemies and incidental props are cleaned up. |

A live named asset that disappears is a failure; another nearby copy of its model is not silently substituted. Original damage and character state persist naturally by retaining the exact objects. The scripted sub/boat/road transfer actions remain in-engine scenes with explicitly verified final seats. Distant navigation, loading, water positions and attached-load physics are **not** proven by source tests.

The inland road team has a bounded ten-minute journey and periodically refreshes its road task. Failure to arrive produces a retryable failure, not an invisible relocation to the lake. The player may wait for the road team before completing the beach regroup.

## Essential scenes and actions

`SceneSpec.RequiresCompletion` distinguishes an essential physical result from optional establishing footage. Scene identities/outcomes are exposed independently so a canceled previous scene cannot be mistaken for a successful new one. Only Completed/Skipped satisfy an essential scene. Required transfers certify their result after the actions, using `VerifySceneStep`, not when their camera starts.

Carry/stow/handover/transfer steps check the actual attachment native. Entry steps check the requested seat, and refuse to evict another occupant to satisfy a skip. Exceptions during action startup, camera callbacks or timeout finalization stop the remaining success actions. Optional shots may keep their existing radio/HUD fallback; an essential fallback must itself satisfy its postconditions.

Normal watched animations are preserved. Existing bounded skip/finalization placement is still used where appropriate. This is not a claim of natural pathfinding or animation accuracy: those require the live pass.

## Progress, rewards and old saves

`campaign.portHeistResumePhase` is an optional, validated additive field. It records **a phase-start reconstruction**, never entity handles or arbitrary stage state. Missing/invalid values are ignored. It is separate from `completedMissions` and cannot grant money or weapon rewards.

During a normal new operation, internal phase completion updates that bookmark but leaves M19–M22 completion flags uncommitted. The operation's cargo changes remain in its in-memory attempt. Final verified success marks all four legacy IDs in a single save batch, applying only rewards that have not already been earned. It then commits the final cargo locations and clears the bookmark. A replay after M22 does not overwrite later salvage locations, reset recovered gold or duplicate the payout. Legitimately owned weapons and existing completion records are retained.

A save already between the old independent chapters resumes at the first incomplete phase. A bookmark from this version takes precedence for a new unfinished operation. A save already past M22 is never forced backward. The normal map presents one operation entry, labeled with the resume phase where applicable; the developer catalog retains individual phases.

**Retry policy:** a failed phase restarts its defined setup, including the essential crew, seats, aircraft/cargo state and objectives. It does not restore the exact injuries, ammunition, enemy positions or elapsed seconds from the moment of failure. This is a coarse phase-start restart, not full checkpoint restoration. `SupportsCheckpointRestore` remains false. A fresh load also reconstructs rather than trusting handles from a previous session. Replays have in-session phase retry, but do not overwrite the completed campaign's persistent bookmark.

## Source verification

See `CONTINUOUS-PORT-HEIST-VERIFICATION.md` for the actual Actions run's results and binary hash. The implementation runner must compile against the pinned SHVDN API, run the story/runtime and regression suites, parser checks, mission lint, location validation and generated-data freshness before publishing the implementation commit. It rebuilds the matching prebuilt DLL. The temporary implementation utility/workflow are removed from the final comparison branch.

The new regression cases cover real phase classes under the parent, exact entity identities across all boundaries, unchanged damage/clock/weather/heat, deferred awards, final commit/replay behavior, failed attachment and occupied-seat outcomes, a refused transfer exit, retry boundary persistence, and explicit standalone QA starts. Tests simulate player/vehicle movement and use GTA stand-ins. They do not simulate physics, police, rendering, asynchronous loading or road navigation.

## Live acceptance route

Use a backup/disposable campaign and matching DLL plus data. Keep the working installation and its save separately for A/B comparison. Developer bypass starts a standalone phase, so it is **not** the test of the combined normal operation.

1. Finish the actual M19 prerequisite and the three required solos. Start normally once, not through a bypassed per-phase QA start.
2. Watch the initial briefing and approach. Finish the underwater cut and both float clamps. Confirm the container physically surfaces and the original Kraken reaches the support mark.
3. Enter Sky Hook without a new Mission Passed, normal intro, clock jump, cleared pursuit or redeployed crew. Note injuries and aircraft damage before/after the boundary.
4. Complete the quay fight and actual hover. Inspect the attached cargo, then watch the crew's submarine/pier/launch transfer. Repeat on a separate run with a deliberate skip.
5. Start the water escort in that launch, with the original loaded helicopter. Check helicopter hold/release and follow behavior while switching.
6. Complete the coastal escort, land at the shore, and watch Ice/Gohan board the Granger. Confirm there is no boat magically appearing on the inland lake.
7. Switch to Ron and fly the same loaded aircraft to the Alamo. The Granger must travel the road. Record a stuck road team and its retry reason rather than marking it passed.
8. Watch or skip the short beach arrival. Release the container, land, exit, and meet both brothers on foot. Confirm the final aftermath finishes and one Mission Passed/reward is recorded.
9. Fail after the hook or during the escort, then retry. Verify the failed phase restarts, earlier gameplay is not unnecessarily repeated, and no early reward was saved. Restart the mod on a disposable save and test the same boundary resume.
10. Abort/cancel a required transfer and destroy its required transport. No later action may be certified as completed. Check player control, visibility, camera and non-frozen occupied transport afterward.
11. Replay after M24 on a copied save. Cash, recovered gold, existing weapon ownership and the later bullion location must not roll backward or duplicate.

## Known limits / separate work

- No live GTA run was performed by this implementation environment. The normal module-level CI and the additional parent tests are evidence of source behavior only.
- The stock tug/container stand-ins, scripted buoyancy, particle/strike presentation and other existing asset substitutions remain. Distant foundry rendering and persistent destruction are still not certified.
- M20's jammer presentation and broader mission balance are not replaced by this lifecycle patch. The existing M13/M15 escort preparation effects remain.
- Cargo/evidence transactions outside this operation, weapon acquisition categories, M04's separate handover issues, multiplayer and M31+ remain separate assignments.
- The combined operation is longer than any one former chapter. Evaluate pacing and recovery with a real uninterrupted playthrough before accepting it as the campaign standard.

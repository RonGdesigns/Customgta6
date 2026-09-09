# Remaining plans and the next additions

Reviewed against `docs/bibles/design_bible_v1.txt`, the omnibus v2 interstitial systems and production roadmap, the solo-mission bible, and the current C# implementation. These documents describe creative proposals; they do not establish implemented features or authorize copying their prototype code into the running mod.

## What remains from the original plans

| Original proposal | Current implementation | Remaining work |
|---|---|---|
| 70 main jobs and 9 solo stories | M01-M30 and SM01-SM06 playable; dialogue/flow plans exist for the rest | 43 gameplay scripts: M31-M70 and SM07-SM09, including setup, role work, visible interactions, failure/retry and earned rewards |
| Mission checkpoints | Full mission restart with failure cleanup and safe player recovery | Reconstruct actors, vehicles, damage, timers, role assignments and private mission state before enabling mid-mission retry |
| Three specialized bases/workbenches | Enterable starter/luxury housing, personal lockers, wardrobe, repairs and story flags | Ice's AP/incendiary ammunition press; Gohan's persistent camera/radar disruption; Guess's earned turbine/conversion workshop and saved fleet builds |
| Trade-specific off-duty life | Independent companion activities, travel, crimes, personal wanted state and ride modes | More authored work animations, visits and consequences that persist between play sessions |
| News and encrypted group-phone reactions | Campaign dispatch/subtitle follow-ups and dialogue | A readable phone inbox/history, branching reactions to optional jobs, recorded radio/news audio |
| Custom character appearance | Three Black freemode heroes, fixed head hair, adjustable clothing | Bespoke body shapes, faces and properly rigged locs/custom ped assets |
| Enterable dock, hangar and tower interiors with cover | Stock interiors/exterior staging where currently supported | Authored MLO spaces and verified connected navigation/cover; game files do not automatically provide every location described in the bible |
| Fully voiced/animated cinematic production | Subtitle-driven scenes, optional WAV playback, local actors/actions and remote conversation framing | Recorded voices, more authored animation/blocking and tested moving-vehicle/air/sea set pieces |
| Offshore operation and three-site finale | Detailed remaining-campaign plan | M42's aircraft/submarine transfer, M44-M48 rig encounter, M55 simultaneous penthouse work, M62 train/boat/helicopter convergence and the M68-M70 finale need physical prototypes |

The friends' relationship remains the story's foundation: trust is rebuilt through sharing information, accepting another brother's decisions and counting people before cargo. Optional solo missions should deepen that arc without becoming required exposition. KJ remains Guess's occasional supporting contact, including the planned SM09 handoff.

## Additions that fit the current systems

These are proposals, not newly installed features.

1. **Owned garages first.** Store vehicle identity, all purchased modifications, damage and location. Add paid recovery/replacement, a workshop build sheet, a marine berth and aircraft storage. This gives shop purchases lasting value and makes acquisition missions useful after completion.
2. **Guess's specialist builds.** Earn engine/transmission packages through M11, SM03 and later jobs. Offer road, pursuit, armored and off-road presets with understandable tradeoffs and a short test route. Support appropriate model conversions only after their actual assets and mod kits are verified.
3. **Ice's weapon bench.** Buy compatible suppressors, sights, extended magazines and Mk II ammo components. Gate AP/incendiary/explosive ammunition behind the intended mission/workbench, show incompatibilities before charging, and preserve each hero's ammo/component state.
4. **Gohan's operational intel.** A map/inbox showing cameras, approach routes and timed disruption opportunities. Let mission preparation open alternative routes rather than simply reducing every enemy's health.
5. **Meaningful crew preparation.** A heist board that reads real saved boats, fuel, aircraft, keys and intel. Show which mission supplies a missing item. Preparation can change available approaches, transport quality or response timing without hiding the main objective.
6. **Clothing ownership and saved outfits.** Separate affordable everyday clothes from premium outfits, add several named presets per hero and reward selected outfits through story/solo jobs. Keep their established hairstyles fixed.
7. **Consequences with limits.** Repair bills for owned transport, small bounded civilian/crew reactions, neighborhood supply discounts after M60, and optional recovery jobs. Avoid an endless wanted-state punishment that undermines the existing ability to escape police.
8. **Post-story rewards.** Preserve homes and equipment after M70; add earned garage choices, visual mementos and replay challenges without repeating the large finale payout.

## Recommended order

Validate this shop/travel build, then implement persistent garages and weapon components alongside **M31-M35**. That block supplies fortification, rescue and armored-transport rewards that can feed the new systems immediately. Build the marine berth and offshore prototypes next, before scripting the whole rig arc. Schedule the major tower/train/aircraft prototypes before the final missions' cinematic production.

There is no XP/skill-point system, simulated business income or third housing tier installed yet. Progress remains mission milestones plus owned equipment and crew cash. See `PROGRESSION-GUIDE.md` for the existing unlock schedule and `CAMPAIGN-REMAINDER.md` for the full remaining mission-by-mission plan.

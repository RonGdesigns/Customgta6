# Remaining plans and the next additions

Reviewed against `docs/bibles/design_bible_v1.txt`, the omnibus v2 interstitial systems and production roadmap, the solo-mission bible, and the current C# implementation. These documents describe creative proposals; they do not establish implemented features or authorize copying their prototype code into the running mod.

Current implementation status refreshed on September 19, 2026. The additions below are proposals unless identified as already implemented. See `code-review-2026-09-19.md` for the integration repair record.

Current external feature research and the recommended delivery order are recorded in
`FEATURE-RESEARCH-2026-09-20.md`.
The selected neighborhood progression and owned-vehicle lifecycle are specified in
`NEIGHBORHOOD-VEHICLE-IMPLEMENTATION-PLAN.md`.

## What remains from the original plans

| Original proposal | Current implementation | Remaining work |
|---|---|---|
| 70 main jobs and 9 solo stories | All 70 main missions and 9 solo missions have gameplay scripts | Live acceptance, placement surveys, balancing, and presentation refinement; automated walkthroughs do not replace playtesting |
| Mission checkpoints | Full mission restart with failure cleanup and safe player recovery | Reconstruct actors, vehicles, damage, timers, role assignments and private mission state before enabling mid-mission retry |
| Three specialized bases/workbenches | Enterable starter/luxury housing, personal lockers, wardrobe, repairs and story flags | Ice's AP/incendiary ammunition press; Gohan's persistent camera/radar disruption; Guess's earned turbine/conversion workshop and saved fleet builds |
| Trade-specific off-duty life | Independent companion activities, travel, crimes, personal wanted state and ride modes | More authored work animations, visits and consequences that persist between play sessions |
| News and encrypted group-phone reactions | Custom campaign phone with persistent completed-job inbox/history, news, crew contacts, live objectives/destinations and wallet; existing notifications and dialogue | Garage/KJ ordering, crew commands, journal, progression, alerts and the planning board are connected; remaining work includes optional voice calls, further branching reactions, recorded radio/news audio and in-world handset screen rendering |
| Custom character appearance | Three Black freemode heroes, fixed head hair, adjustable clothing | Bespoke body shapes, faces and properly rigged locs/custom ped assets |
| Enterable dock, hangar and tower interiors with cover | Stock interiors/exterior staging where currently supported | Authored MLO spaces and verified connected navigation/cover; game files do not automatically provide every location described in the bible |
| Fully voiced/animated cinematic production | Subtitle-driven scenes, optional WAV playback, local actors/actions and remote conversation framing | Recorded voices, more authored animation/blocking and tested moving-vehicle/air/sea set pieces |
| Offshore operation and three-site finale | M42-M70 scripts exist, including the continuous M44-M48 Paleto operation, M55 penthouses, M62 convergence, and the finale | Validate the installed environments, crew transitions, failure cleanup, and mission balance in game |

The friends' relationship remains the story's foundation: trust is rebuilt through sharing information, accepting another brother's decisions and counting people before cargo. Optional solo missions should deepen that arc without becoming required exposition. KJ remains Guess's occasional supporting contact, including the SM09 handoff.

## Additions that fit the current systems

These are proposals, not newly installed features.

1. **Deepen the existing garage system.** Persistent owned vehicles, modifications, paid recovery, KJ delivery, and tuning stages already exist. Refine their status and build-sheet presentation, then validate any expanded physical storage experience against installed assets.
2. **Guess's specialist builds.** Earn engine/transmission packages through M11, SM03 and later jobs. Offer road, pursuit, armored and off-road presets with understandable tradeoffs and a short test route. Support appropriate model conversions only after their actual assets and mod kits are verified.
3. **Ice's weapon bench.** Buy compatible suppressors, sights, extended magazines and Mk II ammo components. Gate AP/incendiary/explosive ammunition behind the intended mission/workbench, show incompatibilities before charging, and preserve each hero's ammo/component state.
4. **Gohan's operational intel.** A map/inbox showing cameras, approach routes and timed disruption opportunities. Let mission preparation open alternative routes rather than simply reducing every enemy's health.
5. **Meaningful crew preparation.** A heist board that reads real saved boats, fuel, aircraft, keys and intel. Show which mission supplies a missing item. Preparation can change available approaches, transport quality or response timing without hiding the main objective.
6. **Clothing ownership and saved outfits.** Separate affordable everyday clothes from premium outfits, add several named presets per hero and reward selected outfits through story/solo jobs. Keep their established hairstyles fixed.
7. **Consequences with limits.** Repair bills for owned transport, small bounded civilian/crew reactions, neighborhood supply discounts after M60, and optional recovery jobs. Avoid an endless wanted-state punishment that undermines the existing ability to escape police.
8. **Post-story rewards.** Preserve homes and equipment after M70; add earned garage choices, visual mementos and replay challenges without repeating the large finale payout.

## Recommended order

Validate the September 19 integration repairs first: crew orders, shared slow motion, getaway driving, and continuous-heist presentation. Survey the next missions being played and resolve live failures before expanding the campaign. Then prioritize race position and saved records, clearer crew-order status, and selected uses of the existing alignment/gauge/choice objectives.

There is no XP/skill-point system, simulated business income or third housing tier installed yet. Progress remains mission milestones plus owned equipment and crew cash. See `PROGRESSION-GUIDE.md` for the existing unlock schedule and `CAMPAIGN-REMAINDER.md` for the full remaining mission-by-mission plan.

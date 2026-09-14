# Bloodlines — Story-to-Play Planning Library

This directory is the organized campaign-planning source for the remaining Bloodlines story-to-play work. It is documentation only: publishing these files does not register a mission, change coordinates, rebuild Bloodlines.dll, alter saves, or convert a planned mission into a live-accepted mission.

## Current review baseline

- Working branch: `claude/gta-v-custom-version-477edi`
- Review baseline before this documentation commit: `aa50735a29cf407eb28c7ac725590585e3194465`
- Built-in gameplay catalog at that baseline: M01-M43 and SM01-SM06 (49 registered mission scripts)
- M44-M70 and SM07-SM09 remain planned unless a newer implementation commit explicitly adds them.

## Reading order

1. [STATUS.md](STATUS.md) — authority, status vocabulary and locked carry-forward.
2. [DECISION-REGISTER.md](DECISION-REGISTER.md) — owner-locked constraints and open recommendations.
3. [Pass 01](pass-01/PLAN.md) — complete campaign alignment plus the first M44-M48 Paleto proposal.
4. [Pass 02](pass-02/PLAN.md) — owner-locked uninterrupted M44-M48 flow plus M49-M53.
5. [Pass 03](pass-03/PLAN.md) — M54-M60, M55 synchronized breach and the M59-M60 emergency-flow option.
6. [Pass 04](pass-04/PLAN.md) — M61-M70, SM07-SM09 and finale continuity options.

## Source priority

Use this order when documents disagree:

1. Latest explicit owner-approved decision.
2. Current compatible working implementation.
3. Current authored story/dialogue data.
4. The latest applicable planning proposal, with recommendations clearly labeled.
5. Older bible/novel material.

The novel is story context, not an asset manifest. A written route, interior, vehicle capability, weapon mount, moving set piece or cargo transfer is not considered implemented until the game/system supports it and the relevant acceptance evidence exists.

## Owner-locked continuity

M44-M48 is one uninterrupted mission attempt, modeled after the existing Port Heist: one normal start, no free-roam intermission, no intermediate Mission Passed screen, no midpoint checkpoint/resume, no fresh crew deployment or loadout reset between internal phases, and one final completion after M48. Failure/abort/quit/reload restarts the operation at M44 without erasing preparations legitimately earned before the operation.

Preserve the existing continuous Port Heist, Ron's approved opening/home, the four-seat M01 getaway, approved M25 parachute/boat/100-meter departure and compatible newer mission/system fixes.

The M59-M60 grouping and M63-M70 full-finale grouping remain recommendations, not owner-approved requirements unless later explicitly accepted.

## Implementation rule

For every mission: context before control, physical result before dialogue claims it, consequence before the next job, and all required people/assets accounted for during transitions. A planning checklist is not a GTA playtest.
# Handling calibration — the road loop and the log sheet

**Direction (Ron, September 9, 2026):** the speed target stays at 2x. Fix steering,
grip, weight, suspension and braking so those speeds are drivable. Baseline
first, without the ability; Guess's ability is a layer on top.

## What changed in code

| Layer | Where | What it does |
| --- | --- | --- |
| Baseline road profile | `Core/RoadHandling.cs`, applied by `WorldTuning` once per model | Per class: traction curve up, traction loss down, suspension damping up, center of mass lowered, brake force up, yaw inertia up. Restored on stand-down where still ours. |
| Guess overlay | `Abilities/SlipstreamReflex.cs` | On his car only, while on all wheels: a bounded press toward the road (a constant 0.25 g of weight plus downforce growing with speed squared to a 0.6 g cap, applied as force because the SDK has no per-vehicle gravity), and a +20% grip lift on the model's shared traction. The press blends in and out over half a second. Never on aircraft or boats. |
| Aircraft | `Core/TravelHandling.cs`, `Core/WorldTuning.cs` | The flight-handling adapter now logs found / unsupported per model. Helicopters fly stock since September 10: no ceiling, no cruise force, no handling edit. |
| Readout | `DevTools.cs` | Top-right: X, Y, Z, heading, and speed in km/h and mph when in a vehicle. |

## Opening values (not final)

| Field | Car | SUV / van | Truck |
| --- | ---: | ---: | ---: |
| TractionCurveMax, TractionCurveMin | ×1.15 | ×1.12 | ×1.08 |
| TractionLossMultiplier | ×0.85 | ×0.85 | ×0.90 |
| SuspensionCompressionDamping, SuspensionReboundDamping | ×1.25 | ×1.30 | ×1.20 |
| CenterOfMassOffset.Z | −0.12 | −0.15 | −0.10 |
| BrakeForce | ×1.40 | ×1.40 | ×1.30 |
| InertiaMultiplier.Z (yaw) | ×1.10 | ×1.10 | ×1.00 |
| SteeringLock, Mass | unchanged | unchanged | unchanged |

Class comes from the vehicle's class type: Compacts, Sedans, Coupes, Muscle, Sports
Classics, Sports and Super are "car"; SUVs, Off-Road, Vans, Utility, Service and
Emergency are "SUV"; Industrial, Commercial and Military are "truck". Motorcycles,
bicycles, boats, aircraft and trains take no road profile.

## The loop

Same route every run: Vespucci Beach parking → Vespucci Blvd east → Del Perro
Freeway north → exit at Rockford Hills → back down West Eclipse Blvd to the start.
Clear weather, midday, no traffic modifiers, no upgrades on the car, dev tools on
for the readout.

Four cars, in this order: **Primo** (sedan), **Schafter** (M01's four-door,
sports sedan), **Granger** (SUV), **Benson** (truck).

Three runs per car, in this order:

1. **Vanilla** — crew stood down (`F10` off), which restores stock handling.
2. **Speed only** — the build before this branch (stand-down and redeploy do not give you this; use the receipt's previous install if you want it, otherwise skip).
3. **Handling first** — this branch, crew deployed, ability off.
4. **Ability** — Guess, same loop, ability on for the two freeway curves.

## Log sheet

Copy one block per run.

```
Car:            Primo | Schafter | Granger | Benson
Run:            vanilla | speed-only | handling-first | ability
Top speed seen: ___ km/h  (readout)
Lap time:       ___ s
Brake 100→0:    ___ m   (start braking at the first freeway gantry, note where you stop)
Lane change at 120 km/h:   recoverable | slide | spin
Curb / crest at speed:     settles | bounces | launches
Collision at speed:        stays down | lifts | flips
Steering at top speed:     usable | heavy | twitchy
Verdict (Ron):  better | same | worse than the previous run, and why
```

For the ability run add: `Ability end mid-corner: smooth | dropped`.

## Buzzard pair

Same altitude (about 80 m), same route (Vespucci Beach to the LSIA control tower
and back), stock versus this branch, both directions:

```
Aircraft:       buzzard | buzzard2
Cruise speed:   ___ km/h at ___ m  (readout)
Route time:     ___ s
Hover:          holds | drifts
Landing:        normal | floaty | hard
Log line:       "flight handling: ..." from Bloodlines.log (found / unsupported)
```

## What the numbers decide

- If "handling first" is better than "speed only" on every car and the collision
  and crest rows stop saying "launches", the profile stands and the speed target
  stays. Then tune per class by a step at a time, one field at a time.
- If SUVs or the truck still lift on impact, the next step is a larger center-of-mass
  drop for that class only, before touching mass.
- If steering is still heavy at top speed after the grip change, the next lever is
  a modest reduction of `TractionLossMultiplier` for that class, not steering lock.
- Speed reduction is not on this list. That is Ron's call after these runs.

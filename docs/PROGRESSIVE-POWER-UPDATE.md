# Progressive power tuning

The updated preference permits extra power, provided cars build speed gradually. The two-times gearing ceiling remains, with progressive torque assistance instead of an immediate launch boost.

- From rest through 25% of the car's original redline speed: ordinary torque.
- Above that: assistance increases smoothly with forward speed, up to 80% extra torque at the original redline speed.
- Increases are also limited over time: full assistance takes at least four seconds to build, even when entering a car already moving quickly.
- Returning to low speed removes the extra torque immediately, so stopping does not leave a boosted relaunch. Reverse, airborne, stalled and disabled cars receive no assistance. Resuming after a scene starts the ramp again.
- Guess's ability shares this tuning; it does not add a separate instant power spike. Stand-down/reload restores ordinary torque and the original live gearing.

This applies to the same loaded road cars as the existing speed profile, including crew, traffic and police cars. No velocity is directly assigned. Shared handling drive force and inertia remain unchanged; the frame-based torque native supplies assistance. Its neutral value is 1.0; the selected maximum of 1.8 is within the range used by game scripts. [Native reference](https://github.com/citizenfx/natives/blob/master/VEHICLE/SetVehicleCheatPowerIncrease.md).

Automated checks cover gradual application, bounded torque, stopping/relaunch, reverse, airborne behavior, scene resumption and cleanup. Actual acceleration feel and attainable top speed still need a road test; two-times gearing is not proof of two-times measured speed.

Test a regular sedan from rest, then continue accelerating along a clear straight. Expect a normal launch with stronger pull as speed builds. Brake fully and repeat to confirm there is no surge on relaunch. Compare Guess's ability on/off and report the car model if it still feels too abrupt or cannot build enough speed.

Later update: see [Market and travel](MARKET-AND-TRAVEL-UPDATE.md) for coverage beyond road cars, including aircraft and boats. The earlier road-only scope above describes the preceding build.

# M13 startup follow-up — September 13, 2026

The installed fix25 build failed twice at M13.BargeOne (-960, -1660), inside SpawnFuelBoats. The intro played; mission setup then aborted. The log confirmed the updated coordinates were loaded. Its generic preflight error did not record whether the failed sample was shallow, obstructed, or still streaming, so the exact native rejection cannot be established from that log.

## Changes

- Estimated barge positions can search within 30 metres in 10-metre rings, checking the complete rotated hull footprint and five metres of water. Personal surveyed positions remain fixed.
- All three hull footprints are reserved and checked before any fuel boats are created. Nearby recovery cannot overlap another barge. Failure at a later site rolls earlier adjustments back so repeated attempts cannot keep moving the first boat away.
- A failed full-footprint check yields for streaming and retries up to three times; seeing the center seabed alone no longer skips that retry.
- Logs report actual model bounds, failed sample position, water/floor elevations, required depth, or the obstructed segment. Those details will identify any remaining live rejection.
- The test tug now uses dimensions read from the installed Enhanced tug fragment instead of the generic small prop used before. Tests plant charges beside the actual-sized hull.

## Verification and next playtest

The read-only map check still finds approximately nine metres of water at the first barge center. Extracted tug drawable bounds are min (-5.141756, -16.78415, -3.719979), max (5.141756, 14.29196, 10.97245). Static map results do not establish live native clearance.

The production DLL compiles; 2,237 story/runtime checks and 189 general checks pass. Regression scenarios cover a blocked estimate that relocates, fixed surveyed positions, blocked water that remains rejected, delayed full-footprint streaming, hull separation, transactional failure, and specific diagnostics. Mission lint reports no errors; location validation reports 0 of 295 flagged.

Start M13 from the mission menu. Let the intro finish or skip it, allow a few seconds for setup, and confirm Ice starts on the scooter with three separate fuel boats. Then plant the three charges and complete the escape/detonation. If it still stops, report it; Bloodlines.log now captures the missing failure details. This build has not been live-tested by the assistant.

Native reference used when reviewing probe results: [Cfx GET_SHAPE_TEST_RESULT](https://github.com/citizenfx/natives/blob/master/SHAPETEST/GetShapeTestResult.md).

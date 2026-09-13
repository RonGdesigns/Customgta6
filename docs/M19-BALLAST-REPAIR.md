# M19 ballast float repair — September 13, 2026

The live log identifies a mission script failure at 13:13:29, after Gohan completed
both ballast clamp interactions. Each interaction requested `prop_buoy_01`, which
is absent from the installed Enhanced archives. Its model request failed silently,
but the objective still counted the clamp. `PlayFloat` then threw because it had
zero floats instead of two. This establishes the script failure; the logs do not
establish a separate native game crash cause.

M19 now uses `prop_dock_bouy_3`, verified by the local CodeWalker asset query. The
mission checks its availability before deploying the operation, and a failed late
model request or prop allocation cannot silently award a clamp. Multi-site work
records completion only after its physical completion callback succeeds. Added
log messages identify each fitted float and the successful surfaced container.

The existing buoy stand-in and staged container rise remain: this is not a custom
inflatable model or simulated buoyancy. Player placements, dialogue, save state,
and the M19–M22 continuous mission structure are unchanged.

Validation: 2,642 story/runtime checks and 206 regression checks passed, including
missing-model startup rejection, late loading/allocation failure without clamp
credit, watching/skipping the float scene, restored submarine controls, and the
existing full M19–M22 object-preserving handoff. The build and mission linter pass;
the location audit flags 0 of 790 placements. These tests use GTA stand-ins, so
the live game retest remains necessary.

Retest M19 from its beginning: cut the hull, complete both eight-second clamps,
confirm two floats and the container-rise scene, then surface the Kraken at the
support marker. The operation should continue to Guess in the Cargobob. If it
fails, preserve `scripts/Bloodlines/Bloodlines.log` before another attempt.

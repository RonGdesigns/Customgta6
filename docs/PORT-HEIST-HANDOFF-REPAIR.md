# Port Heist handoffs — September 13, 2026

## Findings and repairs

The latest live log confirms M19's repaired floats both spawned, the container
surfaced, and the underwater section passed. M20 then rejected M21.LaunchSpawn
as obstructed. A read-only CodeWalker query found the authored center at
(1140, -3380) in water approximately nine meters deep, with clear horizontal
segments across the launch footprint.

The installed ScriptHookVDotNet3 3.9.0.0 DLL was tested directly, without launching
GTA or invoking game natives. A simulated native BOOL output of zero with stale
upper allocation bytes returned true from GetResult<bool>(), but zero from
GetResult<int>(). MarineSites now reads the four-byte BOOL correctly. Real hits
still reject placement; water depth and footprint checks remain in force.
The upstream [OutputArgument and conversion implementation](https://github.com/scripthookvdotnet/scripthookvdotnet/blob/main/source/scripting_v3/GTA.Native/Native.cs)
documents the allocation and conversion path; the reproduction used this user's
installed DLL, rather than assuming the upstream version was identical.

M21's standalone debug run reached its shore-transfer scene, then failed its
seat verification when that scene was skipped. DriveUpStep cleared its driver's
tasks before moving the car, without preserving the seats. Its finish path now
restores the actual occupants to their original seats if cleanup or relocation
unseats them. Tests inject driver ejection to exercise the failure. It does not
replace passengers, displace unrelated occupants, or pretend failed boarding worked.

Failed live boarding/arrival actions in M20, M21 and M22 now report a specific
mission failure rather than leaving an objective waiting indefinitely.

M22's container origin now settles four meters below the waterline. Its verified
model roof is approximately 2.83m above its origin, leaving more than one meter
of water above the roof. The container remains a real frozen underwater prop;
its horizontal recovery location and cargo ledger are unchanged. M24 recreates
or restores that same submerged placement. The drop line now describes hidden
cargo instead of claiming it sits in four feet of water.

## Continuous play and debug testing

Normal story entry runs M19, M20, M21 and M22 as one Port Heist. It preserves
the same characters, vehicles and bullion through each internal transition,
with one final award and no saved checkpoints between sections.

The explicit debug bypass starts an individual section in isolation and stops
when that section completes. It is useful for testing a particular scene, but
does not test the continuous operation's handoffs. An isolated section ending
normally is different from a red failure message.

The automated suite exercises all four production mission classes through one
complete live operation with dirty native BOOL padding injected. It checks the
M19-to-M20 and M20-to-M21 entity identity, live shore boarding, Alamo arrival,
submerged cargo preservation, final payout, aborts/retries, and watched/skipped
scene paths. These stand-ins cannot establish live navigation or native physics.

Retest the normal Port Heist from M19 through the final beach aftermath. Confirm
Gohan surfaces, Guess takes the existing Cargobob, the escort boards, the shore
team enters the Granger, both teams reach the Alamo, and the released container's
roof is underwater. A separate M21 debug run should also survive skipping the
road pickup scene. Keep Bloodlines.log if any section fails again.

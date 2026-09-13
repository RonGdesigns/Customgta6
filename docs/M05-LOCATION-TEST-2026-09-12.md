# M05 startup and location test — September 12

The game log refused M05.GrottoMouth during setup. The ground native in MissionSites.DeepEnough included water, which can return the water surface instead of the seabed. The query now excludes water. Tests model this argument so the original bug produces a failure. The normal 1.2-meter depth requirement remains; this build does not adopt PR43's blanket 0.3-meter surveyed allowance. PR43's useful CoveAir estimate correction is included: the flare point is now beside the dinghy.

## Test mode

The user's installed configuration enables `[Dev] M05LocationTestMode = True`. The repository example defaults to False. This option applies only to M05, regardless of other debug settings.

M05 checks each of its six required locations separately. When a ground or water check fails in test mode, setup continues at that key's configured X/Y. A water fallback uses measured surface height plus 0.2 m if available, otherwise its configured Z. Unverified locations get orange `TEST: M05.…` map blips, a notice after the opening scene, and a `M05 LOCATION TEST` log entry recording the configured and used position. Verified locations are also logged. Successful checks may still resolve estimates through the ordinary location service.

This is a surveying override: an unverified placement may be shallow, on land, floating or inaccessible. It lets the player see that problem instead of being refused at the briefing. It does not certify that the boat can travel through that spot. No survey/configured location files are changed; all temporary location resolutions are restored on cleanup. Normal abort, mission objectives, and retry remain active. Missing essential actors/boats or missing location records still stop setup with an explicit diagnostic instead of creating an impossible mission.

## What to inspect

1. Start M05 and finish or skip the opening. Note any orange TEST markers or location notice.
2. Check Ice's cliff perch and all four generator guards for ground placement and visibility.
3. Switch to Guess. Check the dinghy floats and can move; follow the nearby flare marker.
4. Check Mateo's boat at GrottoMouth and the chase destination at Sandbar. Note boats grounded, buried, or unable to steer out.
5. Use the surveyor to record corrected spots. Report the key, character, objective and visible problem. Abort/retry to rebuild the mission after corrections.

After the locations are confirmed, set M05LocationTestMode=False to restore the normal startup checks. The switch can remain enabled during this playtest.

Validation: 1,898 story/runtime checks, including 30 new native-argument/fallback checks, and production compilation against the installed API reference. Ground/water rejection, all failed-key diagnostics, usable mission setup, abort restoration, ordinary-mode refusal and missing-asset errors are exercised. Live animation and collision still require the playtest above.

Native reference: https://raw.githubusercontent.com/citizenfx/natives/master/MISC/GetGroundZFor_3dCoord.md

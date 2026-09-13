# M05: player-driven chase and remote engine disable

This supersedes the earlier optional mid-chase handoff, following the live test report that the AI did not pursue reliably.

Guess stays at the helm after the flare and drives the entire chase. Gohan works automatically from the dinghy passenger seat using the same ProximityHack progress system as M02. Stay within 45 meters for 24 accumulated connected seconds; losing range pauses progress without erasing it. The HUD shows percentage and range. There is no full-route or one-minute requirement.

At 100%, Mateo's engine is disabled without killing him or ejecting him. His boat slows gradually. Guess still controls the dinghy and must stop within 18 meters of the stopped target. Only then does the objective ask the player to switch to Gohan and take Mateo aboard. No AI pursuit is needed. A lost crew boat, dead witness, dead Gohan or five-minute chase timeout gives a failure reason.

Guess's scripted boat assignment remains protected through the flare and retries, preventing general companion leash recovery from pulling him toward Ice. Surveys and saves remain unchanged. Automated coverage exercises range pauses, passenger requirements, the stopped handoff, full completion, and abort/retry; live water behavior still needs replay.

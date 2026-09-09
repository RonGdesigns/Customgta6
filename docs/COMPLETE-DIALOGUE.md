# Bloodlines: complete dialogue and recording draft

79 written missions; 36 gameplay scripts. Future mission triggers remain design targets.

Generated from dialogue.tsv and authored scene sources. Editorial changes live in data/dialogue_edits.json; original PDFs remain intact.

M01 uses stock dock exteriors while its proposed crane/yacht set is unavailable. Three old recognition cues are superseded by the recognition scene.

## M01 - GHOST IN THE DOCKYARD (scripted)

### Intro

**GUESS** `M01_SCENE_INTRO_01_GUESS`
Terminal Island. Two in the morning. They sent me a gate code, a photograph and half the money. That's usually the part where I should ask more questions.
Delivery: Private channel; no recognition or shared conversation
Trigger: intro

**GUESS** `M01_SCENE_INTRO_02_GUESS`
I'm outside the freight entrance in my own car. One pickup inside the yard. Keep it quiet, get paid, go home.
Delivery: Private channel; no recognition or shared conversation
Trigger: intro

**GOHAN** `M01_SCENE_INTRO_03_GOHAN`
Different client, same dockyard. The laptop at the west service station holds Mateo's payment ledger. It's separate from his meeting point. I'm at the service entrance; I need to reach it before the security shift.
Delivery: Private channel; no recognition or shared conversation
Trigger: intro

**ICE** `M01_SCENE_INTRO_04_ICE`
Mateo Cifuentes. The contract says identify him, keep him alive and trace the shipment. From here I can watch the yard without crossing it.
Delivery: Private channel; no recognition or shared conversation
Trigger: intro

**GUESS** `M01_SCENE_INTRO_05_GUESS`
There it is. Bay two, the four-door prototype. My car stays here when I take that one. Whoever owns it won't be pleased.
Delivery: Private channel; no recognition or shared conversation
Trigger: intro

**GOHAN** `M01_SCENE_INTRO_06_GOHAN`
I used to audit books like these. The numbers tell you who got paid. Missing entries tell you who somebody wants forgotten.
Delivery: Private channel; no recognition or shared conversation
Trigger: intro

**ICE** `M01_SCENE_INTRO_07_ICE`
Mateo's checking shipping records with a dock technician. I don't know who's moving through the yard below. I keep the scope on him until I'm certain.
Delivery: Private channel; no recognition or shared conversation
Trigger: intro

**GUESS** `M01_SCENE_INTRO_08_GUESS`
No names on the job, no backup promised. All right. Easy through the gate. Let's find out what they paid for.
Delivery: Private channel; no recognition or shared conversation
Trigger: intro

### Approaches

**ICE** `M01_S1_01_ICE`
Eyes on Mateo. He is checking a shipment with a dock technician. Keep him alive; the buyers are the real target.
Delivery: Quiet; watching the target from dock cover
Trigger: Ice identifies Mateo through aim

**GOHAN** `M01_S1_02_GOHAN`
Dock terminal is open. Copying the ledger. Just need a few seconds without an alarm.
Delivery: Focused; working the dock terminal
Trigger: Gohan begins the eight-second data copy

**GUESS** `M01_S1_03_GUESS`
Bay two. Keys are in the prototype. Someone expected a driver tonight. I am in.
Delivery: Muffled, tools clinking
Trigger: Guess takes the actual prototype driver seat

### Recognition after all three approaches

**GOHAN** `M01_SCENE_RECOGNITION_01_GOHAN`
Copy finished. The alarm opened their dock security channel. Can anyone hear me? Shooters coming into the yard!
Delivery: Face-to-face reunion; anger interrupted by danger
Trigger: recognition

**ICE** `M01_SCENE_RECOGNITION_02_ICE`
Wait. That voice... Devin? Gohan? Stay in cover. I'm watching the yard.
Delivery: Face-to-face reunion; anger interrupted by danger
Trigger: recognition

**GUESS** `M01_SCENE_RECOGNITION_03_GUESS`
Why am I hearing my high school graduation on a tactical radio? Ice? Both of you?
Delivery: Face-to-face reunion; anger interrupted by danger
Trigger: recognition

**GOHAN** `M01_SCENE_RECOGNITION_04_GOHAN`
Darius. Ron. I thought you were both out of this city.
Delivery: Face-to-face reunion; anger interrupted by danger
Trigger: recognition

**GUESS** `M01_SCENE_RECOGNITION_05_GUESS`
I came back. Tried your old numbers. Figured scholarships bought you two somewhere you didn't need to call from.
Delivery: Face-to-face reunion; anger interrupted by danger
Trigger: recognition

**ICE** `M01_SCENE_RECOGNITION_06_ICE`
Mine bought me a way out. Didn't teach me how to come home. We can't do this here.
Delivery: Face-to-face reunion; anger interrupted by danger
Trigger: recognition

**GOHAN** `M01_SCENE_RECOGNITION_07_GOHAN`
You said you were leaving after graduation. You didn't say we'd have to spend fifteen years guessing if you were alive.
Delivery: Face-to-face reunion; anger interrupted by danger
Trigger: recognition

**GUESS** `M01_SCENE_RECOGNITION_08_GUESS`
We can be angry in a moving car. There's room for three. Tell me you remember how to get in one.
Delivery: Face-to-face reunion; anger interrupted by danger
Trigger: recognition

**ICE** `M01_SCENE_RECOGNITION_09_ICE`
Mateo's running for the slipway. Let him lead us to the buyers. Cover each other and get to Guess's car.
Delivery: Face-to-face reunion; anger interrupted by danger
Trigger: recognition

### Escape

**ICE** `M01_S3_07_ICE`
Mateo reached the launch. We have his ledger; we can follow the money. Get to the car!
Delivery: Frustrated; rallying the others
Trigger: Mateo escapes after the firefight

**GUESS** `M01_S3_08_GUESS`
Get in! The entire port authority is rolling! Fifteen years and this is how we catch up?!
Delivery: Tires screaming, passenger door kicked open
Trigger: Gate smash extraction

**GOHAN** `M01_S3_09_GOHAN`
The copy is here, but half the entries are encrypted. Their security recorded all three of us. We need somewhere to open this.
Delivery: Panting, slamming drive on dash
Trigger: Freeway getaway

### Aftermath

**GOHAN** `M01_SCENE_OUTRO_01_GOHAN`
The dock cameras tagged all three of us. An Aegis van is carrying the upload. We have to catch it.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M01_SCENE_OUTRO_02_GUESS`
Fifteen years without a call. Now we're sharing a wanted poster. Nobody disappears before we talk.
Delivery: Reflective; allow the response to land
Trigger: outro

## M02 - LOOSE STRANDS (scripted)

### Intro

**GOHAN** `M02_SCENE_INTRO_01_GOHAN`
The van has the dock recording. I can hack it from the passenger seat. Stay within thirty-five metres while I cut the upload.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M02_SCENE_INTRO_02_ICE`
Guess drives, Gohan works the connection. If they spot us, I cover the car. Once the van stops, I take the physical drives.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M02_S1_01_GOHAN`
That van has the dock footage. I'll work from back here. Keep us close - less than three minutes before the upload lands!
Delivery: 
Trigger: Pursuit begins; Gohan already seated

**GUESS** `M02_S1_02_GUESS`
Black Rumpo, satellite dome. Gohan, you have a signal? I'll keep us inside thirty-five metres.
Delivery: 
Trigger: Chase car closes to the van

**ICE** `M02_S2_03_ICE`
Halfway. They found the intrusion - guns in the windows! Keep working, Gohan. I'll cover our side.
Delivery: 
Trigger: Remote hack reaches 50 percent

**GOHAN** `M02_S2_04_GOHAN`
I'm in. Upload cut, ignition disabled. The server's intact. Ice, take the drives.
Delivery: 
Trigger: Remote hack reaches 100 percent

**ICE** `M02_S2_05_ICE`
Hands on your neck! Unplug that rack or I ventilate your chest!
Delivery: Kicking rear doors off hinges
Trigger: Hard drive seizure

**GUESS** `M02_S2_06_GUESS`
Aegis chopper overhead! Dive down the storm canal ramp-hold on!
Delivery: 
Trigger: Aqueduct escape

### Aftermath

**GUESS** `M02_SCENE_OUTRO_01_GUESS`
You still give orders like the bell's about to ring. I can find us a shop in Cypress, if you can ask.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M02_SCENE_OUTRO_02_ICE`
Can you get us somewhere safe, Guess? And Gohan... keep that drive. I want to hear what it says.
Delivery: Reflective; allow the response to land
Trigger: outro

## M03 - CYPRESS FOUNDRY (scripted)

### Intro

**GUESS** `M03_SCENE_INTRO_01_GUESS`
Cypress Foundry has space, but no armor or equipment. That depot gives us a place we can actually defend.
Delivery: Briefing; intent before tactics
Trigger: intro

**GOHAN** `M03_SCENE_INTRO_02_GOHAN`
Guess delays the rail response. Ice clears the depot; I load its weapons. Use the yellow work markers with E or D-pad Right. Then Guess brings the Benson home. Shared base, shared information.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M03_S1_01_GUESS`
Rail junction locked. Their backup route is delayed. Ice, the depot approach is yours.
Delivery: 
Trigger: Matching playable objective event in M03

**ICE** `M03_S1_02_ICE`
Switch to me. Walk into the yellow entry marker at the depot. No ability needed. Once we cross it, clear the guards marked red; then Gohan can load the truck.
Delivery: 
Trigger: Matching playable objective event in M03

**ICE** `M03_S2_03_ICE`
Clear the marked guards first. Nobody starts loading while they're shooting at the terminal.
Delivery: Firing Combat MG in bursts
Trigger: Matching playable objective event in M03

**GOHAN** `M03_S2_04_GOHAN`
Weapons loaded. Guess, bring yourself to the depot and take the orange-marked Benson. We need that truck, not another car.
Delivery: 
Trigger: Matching playable objective event in M03

**GUESS** `M03_S2_05_GUESS`
I've got the weapons truck. Taking it to the foundry. Ice and Gohan, keep the depot clear until I'm out.
Delivery: 
Trigger: Matching playable objective event in M03

### Aftermath

**ICE** `M03_SCENE_OUTRO_01_ICE`
Miller's selling the dock forensics. We go after him next. Everybody gets the file before we move.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M03_SCENE_OUTRO_02_GUESS`
Three keys to the shop. Keep yours this time. I got tired of being the only one checking the door.
Delivery: Reflective; allow the response to land
Trigger: outro

## M04 - SEVERED WIRE (scripted)

### Intro

**GOHAN** `M04_SCENE_INTRO_01_GOHAN`
Miller has our forensics and an Aegis buyer. The van's drive gave us the meeting. We need his copy.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M04_SCENE_INTRO_02_GUESS`
Gohan takes the marked breaker in the surface lot. Ice clears the escort; I chase Miller for his drive. His car or him, we stop one. Tell me where this stops after that, Ice.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M04_S1_01_GOHAN`
Miller's meeting is in this surface lot. Follow the yellow breaker marker, then press E or D-pad Right. My special ability is optional.
Delivery: 
Trigger: Matching playable objective event in M04

**ICE** `M04_S1_02_ICE`
Breaker cut. Switch to me and clear the marked escort. Guess is waiting in our chase car.
Delivery: 
Trigger: Matching playable objective event in M04

**GUESS** `M04_S2_03_GUESS`
Escort's down. Switch to me in the orange-marked car. Once we're ready, we go after Miller on the surface streets.
Delivery: 
Trigger: Matching playable objective event in M04

**ICE** `M04_S2_04_ICE`
He's moving. Follow his red marker, disable the car or stop Miller, then get out for the drive. Don't let him pull away.
Delivery: 
Trigger: Matching playable objective event in M04

**ICE** `M04_S2_05_ICE`
Drive secured. We have the meeting records. Let's see where Miller sent the money.
Delivery: 
Trigger: Matching playable objective event in M04

### Aftermath

**ICE** `M04_SCENE_OUTRO_01_ICE`
Miller's drive points to Mateo's sea cave. Mateo can tell us who paid for this. We bring him in breathing.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M04_SCENE_OUTRO_02_GOHAN`
Then let me ask the questions. A corpse can't explain why three separate clients sent us to one dock.
Delivery: Reflective; allow the response to land
Trigger: outro

## M05 - TIDAL LOCK (scripted)

### Intro

**GOHAN** `M05_SCENE_INTRO_01_GOHAN`
Mateo is hiding offshore. If Aegis burns him before we reach him, we lose the only person who knows the setup.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M05_SCENE_INTRO_02_ICE`
I cover the shore. Guess and Gohan take the dinghy: signal the cove, close on Mateo, bring him in breathing. I missed my shot when I heard your voice. Don't make me regret it.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M05_S1_01_ICE`
I'm covering the coast. Clear the marked shore guards, then switch to Guess in the dinghy. Gohan is already riding with him.
Delivery: 
Trigger: Matching playable objective event in M05

**GUESS** `M05_S1_02_GUESS`
Flare away. Gohan, stay aboard. Switch to Gohan for the approach and I'll drive, or take the wheel yourself first.
Delivery: 
Trigger: Matching playable objective event in M05

**GOHAN** `M05_S1_03_GOHAN`
Mateo's running. Stay in the dinghy and close within twenty-five metres for five seconds. We need him alive.
Delivery: 
Trigger: Matching playable objective event in M05

**MATEO** `M05_S2_04_ENEMY`
Aegis bought city council... they needed a three-man ghost squad to justify a forty-million-dollar defense contract!
Delivery: Bleeding against boat dashboard
Trigger: Beach revelation

**GOHAN** `M05_S2_05_GOHAN`
The whole damn state government is funding the private army hunting us.
Delivery: 
Trigger: Trio realization

**ICE** `M05_S2_06_ICE`
Then we don't run. We take this war to Blaine County and bleed them dry.
Delivery: 
Trigger: Act I climax

### Aftermath

**GUESS** `M05_SCENE_OUTRO_01_GUESS`
You said take the war to Blaine. Fine, later. Our faces are still sitting in a police evidence vault here.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M05_SCENE_OUTRO_02_GOHAN`
Mateo's accusation isn't proof yet. First the backup servers. Then we follow the money he was protecting.
Delivery: Reflective; allow the response to land
Trigger: outro

## M06 - CLEAN SWEEP (scripted)

### Intro

**GOHAN** `M06_SCENE_INTRO_01_GOHAN`
The mobile upload was one copy. Vespucci has the backup. Burning it buys us time; it doesn't erase who saw us.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M06_SCENE_INTRO_02_GUESS`
I wait in the Granger. Gohan cuts the marked feeder, Ice takes the entrance, then Gohan burns the racks while Ice holds SWAT. Both of you come back before we leave.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M06_S1_01_GOHAN`
Feeder cut. Ice, take the yellow sally-port marker. Guess stays with the getaway car.
Delivery: 
Trigger: Matching playable objective event in M06

**ICE** `M06_S1_02_ICE`
Switch to Ice and walk into the yellow entrance marker. No ability needed. Gohan handles the racks while I cover the alley.
Delivery: 
Trigger: Matching playable objective event in M06

**ICE** `M06_S2_03_ICE`
Stay on me and take out the red-marked SWAT. Gohan is working on the racks himself. We leave when the burn is done and the waves are clear.
Delivery: 
Trigger: Matching playable objective event in M06

**GOHAN** `M06_S2_04_GOHAN`
Burn complete. The backup is gone. We're ready to leave.
Delivery: Coughing through smoke
Trigger: Matching playable objective event in M06

**GUESS** `M06_S2_05_GUESS`
Granger's waiting at the orange marker. Switch to me in the driver's seat and let both of you board before we leave.
Delivery: 
Trigger: Matching playable objective event in M06

### Aftermath

**ICE** `M06_SCENE_OUTRO_01_ICE`
You called the window and I took it. We made it because you were watching something I couldn't see.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M06_SCENE_OUTRO_02_GOHAN`
Now we need their traffic, not their cameras. The microwave dish should tell us what Aegis is moving.
Delivery: Reflective; allow the response to land
Trigger: outro

## M07 - WIRETAP WALTZ (scripted)

### Intro

**GOHAN** `M07_SCENE_INTRO_01_GOHAN`
Mateo named Aegis. This tap lets us test his story against their own manifests. Don't destroy the dish.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M07_SCENE_INTRO_02_ICE`
I used to trust a uniform to tell me whose side I was on. Give me something I can verify.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M07_S1_01_GOHAN`
Ice, reach the marked antenna platform. Fit the receiver and pull the shipping manifest.
Delivery: 
Trigger: Gameplay stage 1 entry

**ICE** `M07_S1_02_ICE`
Receiver clamped. Pulling manifests... Gohan, what are these engines?
Delivery: 
Trigger: Gameplay stage 2 completion

**GOHAN** `M07_S1_03_GOHAN`
Aegis is shipping two military turbine engines to Paleto. That's our target.
Delivery: 
Trigger: Gameplay stage 2 completion

**ICE** `M07_S2_04_ICE`
Aegis helicopter incoming. I need a way off this roof.
Delivery: 
Trigger: Gameplay stage 3 entry

**GUESS** `M07_S2_05_GUESS`
Your pickup is marked, Ice. Get down safely. I'm holding the car for you.
Delivery: 
Trigger: Gameplay stage 3 entry

### Aftermath

**GUESS** `M07_SCENE_OUTRO_01_GUESS`
Two engines, bound for Paleto. Steal their speed and maybe we stop finishing every job with holes in the doors.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M07_SCENE_OUTRO_02_GOHAN`
Elysian warehouse first. This is a supply route. Each shipment should tell us where the next one goes.
Delivery: Reflective; allow the response to land
Trigger: outro

## M08 - SUPPLY & SEVER (scripted)

### Intro

**GUESS** `M08_SCENE_INTRO_01_GUESS`
Those turbines are the difference between hauling armor and dying inside it. I'll choose what the truck can carry.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M08_SCENE_INTRO_02_ICE`
Take the time you need to secure them. I won't call you slow while I'm standing behind your armor.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M08_S1_01_GUESS`
Seven hundred horsepower, bulletproof casings, all-fuel intake. Nothing in San Andreas catches us.
Delivery: 
Trigger: Gameplay stage 1 entry

**GOHAN** `M08_S1_02_GOHAN`
Camera loop is active. Ice, clear the marked sentries so Guess can load the engines.
Delivery: 
Trigger: Gameplay stage 1 completion

**GUESS** `M08_S2_03_GUESS`
Crate one seated! Ice, armored technical coming up the boat ramp!
Delivery: 
Trigger: Gameplay stage 4 entry

**ICE** `M08_S2_04_ICE`
Technical is out. Guess, secure the second crate.
Delivery: Detonating grenade launcher
Trigger: Gameplay stage 4 completion

**GUESS** `M08_S2_05_GUESS`
Chained down! Hitting the Del Perro connector!
Delivery: 
Trigger: Gameplay stage 7 entry

### Aftermath

**GOHAN** `M08_SCENE_OUTRO_01_GOHAN`
The crates need a quiet transfer before the final run. While they're hidden, we can lift an IFF from Vance's convoy.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M08_SCENE_OUTRO_02_GUESS`
Colonel Vance. Same surname as you, Ice. I won't invent a connection, but don't make me ask twice if there is one.
Delivery: Reflective; allow the response to land
Trigger: outro

## M09 - ROLLING THUNDER (scripted)

### Intro

**ICE** `M09_SCENE_INTRO_01_ICE`
The colonel isn't family. He's Aegis command. We take his escort's clearance unit; chasing him costs us the route.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M09_SCENE_INTRO_02_GUESS`
Good. Tell me the part that might change your judgment before I put the Frogger over that convoy.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M09_S1_01_ICE`
Three Insurgents north past Harmony. Middle vehicle has Vance. Rear vehicle is escort.
Delivery: 
Trigger: Gameplay stage 1 entry

**GUESS** `M09_S1_02_GUESS`
I'm taking the Frogger toward the convoy. Ice, hold the ambush point. Keep the rear escort marked.
Delivery: 
Trigger: Gameplay stage 2 entry

**ICE** `M09_S2_03_ICE`
Escort driver down. The transponder is still in his cab.
Delivery: Suppressed crack
Trigger: Gameplay stage 3 completion

**GUESS** `M09_S2_04_GUESS`
Ice, approach the stopped escort on foot. Pull its IFF unit. I'll keep watch from the Frogger.
Delivery: 
Trigger: Gameplay stage 4 entry

**ICE** `M09_S2_05_ICE`
Transponder secured. Code 7-Echo-Victor. We got military clearance tomorrow.
Delivery: 
Trigger: Gameplay stage 4 completion

### Aftermath

**ICE** `M09_SCENE_OUTRO_01_ICE`
Seven-Echo-Victor gets us past a military checkpoint. Save it for the heavy lift. The engines still need delivering.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M09_SCENE_OUTRO_02_GUESS`
Then we finish the job we parked. No fresh score until those crates are off the road.
Delivery: Reflective; allow the response to land
Trigger: outro

## M10 - OPEN THROTTLE (scripted)

### Intro

**GUESS** `M10_SCENE_INTRO_01_GUESS`
The engines are back on the hauler. Aegis knows they're missing. Keep their bikes off me and I'll keep us moving.
Delivery: Briefing; intent before tactics
Trigger: intro

**GOHAN** `M10_SCENE_INTRO_02_GOHAN`
I'm watching the route. Call the danger you see; I'll keep the way out on your map.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M10_S1_01_GUESS`
Keep the loaded flatbed moving above thirty-five. We have a little room to accelerate, but no room to get boxed in.
Delivery: 
Trigger: Gameplay stage 2 entry

**ICE** `M10_S1_02_ICE`
Bikes closing on us. Guess, use your drive-by weapon. I have the passenger side.
Delivery: 
Trigger: Gameplay stage 2 entry

**GOHAN** `M10_S1_03_GOHAN`
I'm watching the route from here. Keep the truck intact; those engines are our way forward.
Delivery: 
Trigger: Gameplay stage 2 entry

**ICE** `M10_S2_04_ICE`
Buzzard is out. Guess, get us moving again.
Delivery: 
Trigger: Gameplay stage 3 completion

**GUESS** `M10_S2_05_GUESS`
Back to the flatbed. Next stop is the tunnel marker.
Delivery: 
Trigger: Gameplay stage 4 entry

**GOHAN** `M10_S2_06_GOHAN`
Clear! Tunnel mouth ahead! Engines delivered!
Delivery: 
Trigger: Gameplay stage 4 completion

### Aftermath

**ICE** `M10_SCENE_OUTRO_01_ICE`
You stopped joking on that bridge. I should've noticed how close we were before you had to tell me.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M10_SCENE_OUTRO_02_GUESS`
I joke when I'm scared too. Learn the difference. Tomorrow we bolt these things into something that brings us home.
Delivery: Reflective; allow the response to land
Trigger: outro

## M11 - IRONCLAD DYNO (scripted)

### Intro

**GUESS** `M11_SCENE_INTRO_01_GUESS`
Nobody fires a shot today unless this engine starts shooting first. Ice, hold the throttle where I put it.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M11_SCENE_INTRO_02_ICE`
Your shop, your call. I owe you a day where keeping us alive doesn't mean driving through a roadblock.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M11_S1_01_GUESS`
Torque converter locked. Roll on dyno! Let's see if this military turbine tears our gearbox in half!
Delivery: Wrench ratcheting, air compressor hum
Trigger: Gameplay stage 1 completion

**ICE** `M11_S1_02_ICE`
Manifold pressure passing 25 PSI! RPM at 6,200! Exhaust temps holding green!
Delivery: Hands on steering wheel, throttle revving
Trigger: Gameplay stage 3 completion

**GUESS** `M11_S1_03_GUESS`
Remember when we used to hotwire clunkers behind the Davis car wash? Now we tuning aerospace turbine engines in Burro Heights.
Delivery: Leaning into driver window, grinning
Trigger: Gameplay stage 4 entry

**ICE** `M11_S1_04_ICE`
Fifteen years ago you couldn't change an alternator without stripping the threads, Guess. You came a long way.
Delivery: Chuckling softly
Trigger: Gameplay stage 4 entry

**GOHAN** `M11_S1_05_GOHAN`
Enjoy the nostalgia while you can, boys. Aegis just docked three billion in cartel gold bullion into Berth 44. We are taking it.
Delivery: Walking into shop with rugged laptop
Trigger: Gameplay stage 4 entry

### Aftermath

**GOHAN** `M11_SCENE_OUTRO_01_GOHAN`
Berth 44 has gold and bonds on the manifest. Three billion is the shipment's claim, not money we can spend.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M11_SCENE_OUTRO_02_GUESS`
We survey it before we dream about it. And if we get rich, nobody buys the right to vanish without a goodbye.
Delivery: Reflective; allow the response to land
Trigger: outro

## M12 - BLACK TIDE RECON (scripted)

### Intro

**GOHAN** `M12_SCENE_INTRO_01_GOHAN`
The manifest gives us a hold number. The ROV tells us whether a container can actually come out through the hull.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M12_SCENE_INTRO_02_GUESS`
You're quiet when you're worried. Used to mean an exam. Now it means I should check my fuel twice.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M12_S1_01_GOHAN`
I'm launching the sub from the jetty. First the sonar marker, then the hull scan.
Delivery: 
Trigger: Gameplay stage 1 entry

**ICE** `M12_S1_02_ICE`
Patrols are near the hull. Keep the sub at the marked depth and break their sight line.
Delivery: 
Trigger: Gameplay stage 2 entry

**GOHAN** `M12_S1_03_GOHAN`
Breach coordinates tagged. Hull thickness eight inches. We need acoustic plasma torches.
Delivery: 
Trigger: Gameplay stage 3 completion

### Aftermath

**GOHAN** `M12_SCENE_OUTRO_01_GOHAN`
The hull can be breached, but surface patrols will box in the extraction. We need their boats short of fuel.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M12_SCENE_OUTRO_02_ICE`
You showed us the part that wouldn't work. Keep doing that. I can plan around a problem you tell me about.
Delivery: Reflective; allow the response to land
Trigger: outro

## M13 - SMUGGLER'S CUT (scripted)

### Intro

**ICE** `M13_SCENE_INTRO_01_ICE`
The fuel barges supply harbor pursuit. Take them out and the heist's slowest vehicle gets a chance to leave.
Delivery: Briefing; intent before tactics
Trigger: intro

**GOHAN** `M13_SCENE_INTRO_02_GOHAN`
Check the waterline before you plant. A fuel worker isn't Aegis command just because he's working their slipway.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M13_S1_01_ICE`
Charge three armed on barge hull. Guess, be ready on western canal slipway.
Delivery: 
Trigger: Gameplay stage 2 completion

**GUESS** `M13_S1_02_GUESS`
I'm holding the Granger at the slipway. Get aboard, then trigger the charges.
Delivery: 
Trigger: Gameplay stage 3 entry

**ICE** `M13_S1_03_ICE`
Charges blown! Basin is an inferno! Patrol boats burning at moorings!
Delivery: 
Trigger: Gameplay stage 4 completion

### Aftermath

**GUESS** `M13_SCENE_OUTRO_01_GUESS`
Fewer boats behind us. That still leaves radar above us. Sandy Shores has the jammer pod we need.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M13_SCENE_OUTRO_02_ICE`
Copy. We're making an exit, not collecting explosions. Keep me honest about that.
Delivery: Reflective; allow the response to land
Trigger: outro

## M14 - AIRSPACE BLACKOUT (scripted)

### Intro

**GUESS** `M14_SCENE_INTRO_01_GUESS`
The stolen pod has to survive the flight. Blinding their radar is worth more than winning a dogfight tonight.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M14_SCENE_INTRO_02_ICE`
If the approach closes, call it off. I'd rather change the heist than put your name on a memorial.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M14_S1_01_GUESS`
The marked aircraft carries the jammer hardware. Getting in, then taking it to McKenzie.
Delivery: 
Trigger: Gameplay stage 3 entry

**ICE** `M14_S1_02_ICE`
Radar coverage ahead. Stay under fifty metres above terrain and land at the marked airfield.
Delivery: 
Trigger: Gameplay stage 4 entry

**GUESS** `M14_S1_03_GUESS`
Cleared the pass! Pod jammer secured in McKenzie hangar.
Delivery: 
Trigger: Gameplay stage 4 completion

### Aftermath

**GUESS** `M14_SCENE_OUTRO_01_GUESS`
Pod's ours. Hearing you say I could turn back made it easier to make the run. Funny how that works.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M14_SCENE_OUTRO_02_GOHAN`
Air cover sorted. I still need the harbor gate network before we move anything through the channel.
Delivery: Reflective; allow the response to land
Trigger: outro

## M15 - CRAWLSPACE (scripted)

### Intro

**GOHAN** `M15_SCENE_INTRO_01_GOHAN`
This fiber trunk runs the lock gates. Without access, our exit becomes a basin with the doors shut.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M15_SCENE_INTRO_02_ICE`
Talk me through the tap. I don't need every circuit. I need to know when you're committed and can't move.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M15_S1_01_GOHAN`
I'm at the fiber point. Starting the optical bypass; cover the approach.
Delivery: 
Trigger: Gameplay stage 3 entry

**ICE** `M15_S1_02_ICE`
All three watchmen stunned. Still breathing. Gohan, finish the connection.
Delivery: 
Trigger: Gameplay stage 2 completion

**GOHAN** `M15_S1_03_GOHAN`
Lock gate controls mapped to my laptop. We control port access.
Delivery: 
Trigger: Gameplay stage 3 completion

### Aftermath

**GOHAN** `M15_SCENE_OUTRO_01_GOHAN`
Gate access is ready. Keeping a copy on each of your devices. If I go down, the plan still belongs to all three.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M15_SCENE_OUTRO_02_GUESS`
You aren't a spare part, Gohan. But thank you for trusting us with your work.
Delivery: Reflective; allow the response to land
Trigger: outro

## M16 - THE HEAVY LIFT (scripted)

### Intro

**ICE** `M16_SCENE_INTRO_01_ICE`
The convoy's IFF buys an approach to Zancudo. It won't make us invisible once Guess starts the Cargobob.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M16_SCENE_INTRO_02_GUESS`
I'll call when the aircraft is ready. You leave the fight then, even if there's somebody left to be angry at.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M16_S1_01_ICE`
Helipad perimeter clear! Cargobob twin turbines spooled! Take the controls, Guess!
Delivery: 
Trigger: Gameplay stage 2 completion

**GUESS** `M16_S1_02_GUESS`
Taking the Cargobob through the canyon. Keeping below sixty metres above terrain.
Delivery: 
Trigger: Gameplay stage 4 entry

**ICE** `M16_S1_03_ICE`
Canyon segment clear. Bring the lift to Terminal Island and set it down.
Delivery: 
Trigger: Gameplay stage 5 entry

### Aftermath

**ICE** `M16_SCENE_OUTRO_01_ICE`
I heard you. You got the lift out. We'll meet at Terminal. The helicopter is ours and the clearance trick is burned.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M16_SCENE_OUTRO_02_GOHAN`
Now the submerged load needs a way to float. Bring that lifting capacity to the dry dock.
Delivery: Reflective; allow the response to land
Trigger: outro

## M17 - SUB-ZERO PAYLOAD (scripted)

### Intro

**GOHAN** `M17_SCENE_INTRO_01_GOHAN`
The Kraken needs cutters and ballast grapples. A hole in a ship means nothing if the cargo stays on the bottom.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M17_SCENE_INTRO_02_GUESS`
You build like you expect nobody to come get you. Put a release on this thing that another person can reach.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M17_S1_01_GOHAN`
Plasma-arc torches calibrated. Can slice through eight-inch naval bulkhead in four minutes.
Delivery: 
Trigger: Gameplay stage 1 completion

**GUESS** `M17_S1_02_GUESS`
Hydraulic ballast grapples tested. Fifty-ton magnetic lock verified.
Delivery: 
Trigger: Gameplay stage 2 completion

**ICE** `M17_S1_03_ICE`
The sub is ready. Tomorrow night we hit Berth 44.
Delivery: 
Trigger: Gameplay stage 3 entry

### Aftermath

**GOHAN** `M17_SCENE_OUTRO_01_GOHAN`
All right. External release, marked yellow. I learned to work alone. That doesn't mean I want to die that way.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M17_SCENE_OUTRO_02_ICE`
We rehearse the pickups at staging. Everybody knows how to bring the other two back.
Delivery: Reflective; allow the response to land
Trigger: outro

## M18 - THE STAGING LINE (scripted)

### Intro

**ICE** `M18_SCENE_INTRO_01_ICE`
Sub, Cargobob, hauler. Check the fuel and the exits now. Once Gohan cuts the hull, this stops being preparation.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M18_SCENE_INTRO_02_GUESS`
One rule from me: if somebody calls abort, we answer before we argue. A container isn't a fourth brother.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M18_S1_01_ICE`
Tonight we hit Berth 44, take thirty tons of their gold, and finish what started.
Delivery: 
Trigger: Gameplay stage 5 entry

**GUESS** `M18_S1_02_GUESS`
Cargobob fueled. Just cut that container loose and I will hoist it.
Delivery: 
Trigger: Gameplay stage 5 entry

**GOHAN** `M18_S1_03_GOHAN`
Kraken sub submerged in position. Moving to Berth 44.
Delivery: 
Trigger: Gameplay stage 5 entry

### Aftermath

**GOHAN** `M18_SCENE_OUTRO_01_GOHAN`
I used to think being useful was how I earned a place here. Tonight, if I need help, I'll say it.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M18_SCENE_OUTRO_02_ICE`
Then we go in with that understood. Hold three first. No one starts a different fight.
Delivery: Reflective; allow the response to land
Trigger: outro

## M19 - THE PORT HEIST: UNDERWATER BREACH (scripted)

### Intro

**GOHAN** `M19_SCENE_INTRO_01_GOHAN`
I'm taking the sub under Berth 44. Listen for my breathing. If the radio gets too quiet, ask me a question.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M19_SCENE_INTRO_02_ICE`
What did Guess break at the car wash? I've got enough answers to keep you talking all night.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M19_S1_01_GOHAN`
Breach is open. Next are the two ballast clamps. Staying in the Kraken to work them.
Delivery: 
Trigger: Gameplay stage 2 completion

**ICE** `M19_S1_02_ICE`
Container surfacing! Guess, bring the Cargobob in now!
Delivery: 
Trigger: Gameplay stage 3 completion

**GOHAN** `M19_S1_03_GOHAN`
Depth charges dropping! Surfacing to the boarding launch!
Delivery: 
Trigger: Gameplay stage 4 entry

### Aftermath

**GOHAN** `M19_SCENE_OUTRO_01_GOHAN`
Ballast attached. Cargo is coming up. Guess, your turn. And Ice... it was the owner's new radio.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M19_SCENE_OUTRO_02_GUESS`
It was already broken. Hook line coming down. Don't settle that argument under thirty tons of metal.
Delivery: Reflective; allow the response to land
Trigger: outro

## M20 - THE PORT HEIST: SKY HOOK (scripted)

### Intro

**GUESS** `M20_SCENE_INTRO_01_GUESS`
Gohan floated the load. I hook it; Ice keeps the quayside gunners off the rotor. Nobody mistakes altitude for safety.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M20_SCENE_INTRO_02_ICE`
Call the strain before the engine does. We can lose weight. We can't lose you.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M20_S1_01_GUESS`
Cable locked! Hoisting thirty tons of bullion! KEEP RPGS OFF MY ROTORS!
Delivery: 
Trigger: Gameplay stage 3 completion

**ICE** `M20_S1_02_ICE`
Quayside gunners clear. Guess, bring the lift over the container and lock it on.
Delivery: 
Trigger: Gameplay stage 2 completion

**GUESS** `M20_S1_03_GUESS`
Full cyclic forward! We are airborne and clearing the cranes!
Delivery: 
Trigger: Gameplay stage 4 completion

### Aftermath

**GUESS** `M20_SCENE_OUTRO_01_GUESS`
She's airborne, barely. I can't dodge anything with this hanging under us. Gohan, I need that water route clear.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M20_SCENE_OUTRO_02_GOHAN`
I'm on your flank. Follow my boat. You don't have to invent an exit while you're holding the whole score.
Delivery: Reflective; allow the response to land
Trigger: outro

## M21 - THE PORT HEIST: OPEN WATER (scripted)

### Intro

**GOHAN** `M21_SCENE_INTRO_01_GOHAN`
The Cargobob can't outrun a missile with the container attached. I'll draw the surface boats away from its path.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M21_SCENE_INTRO_02_GUESS`
Stay where I can see you. Protecting me doesn't mean you're allowed to disappear under the water.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M21_S1_01_GOHAN`
Running the launch alongside the lift. Ice, watch the boats. Guess, keep your route over the water.
Delivery: 
Trigger: Gameplay stage 2 entry

**ICE** `M21_S1_02_ICE`
Pursuit boats clear. Gohan, take the launch through the breakwater.
Delivery: 
Trigger: Gameplay stage 3 completion

**GUESS** `M21_S1_03_GUESS`
Water exit clear. I'm taking the bullion north. Meet me at the Alamo.
Delivery: 
Trigger: Gameplay stage 4 completion

### Aftermath

**ICE** `M21_SCENE_OUTRO_01_ICE`
We have a route to the Alamo. Drop the load shallow enough to recover. Everybody lands before we count it.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M21_SCENE_OUTRO_02_GUESS`
I want one minute on the beach where nobody asks me to keep moving.
Delivery: Reflective; allow the response to land
Trigger: outro

## M22 - THE PORT HEIST: SCORCHED BAY (scripted)

### Intro

**ICE** `M22_SCENE_INTRO_01_ICE`
The container goes into the shallows. Nobody takes the whole shipment home in one night. We recover it in pieces.
Delivery: Briefing; intent before tactics
Trigger: intro

**GOHAN** `M22_SCENE_INTRO_02_GOHAN`
I want to check the foundry link before we head back. Aegis still knows there were three faces at the docks.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M22_S1_01_GUESS`
Container detached! Thirty tons of bullion sitting safe in four feet of Alamo water.
Delivery: Turboprops cutting out, container splashing into mud
Trigger: Gameplay stage 2 completion

**ICE** `M22_S1_02_ICE`
Our Cypress Foundry is gone. Aegis just leveled the entire city block with precision cruise missiles.
Delivery: Looking south at black smoke column over mountains
Trigger: Gameplay stage 4 entry

**GOHAN** `M22_S1_03_GOHAN`
Servers, tools, clothes... everything we set up in Los Santos is ash. They declared us military combatants.
Delivery: Staring at laptop screen, jaw clenched
Trigger: Gameplay stage 4 entry

**GUESS** `M22_S1_04_GUESS`
They burned our city shop? Fine. They don't know who they dealing with out here in the dirt. Blaine County belongs to us now.
Delivery: Kicking sand, determined glare
Trigger: Gameplay stage 4 entry

**ICE** `M22_S1_05_ICE`
We need shelter before another strike. That radar site in the desert is our next move.
Delivery: Racking bolt of assault rifle
Trigger: Gameplay stage 4 entry

**GOHAN** `M22_S1_06_GOHAN`
They know our names now. We take care of each other, and we take back the initiative.
Delivery: 
Trigger: Gameplay stage 4 entry

### Aftermath

**GUESS** `M22_SCENE_OUTRO_01_GUESS`
That shop was going to be ours. Not a hideout. A place you two might actually stay.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M22_SCENE_OUTRO_02_ICE`
I said I could defend it. I was wrong. We get shelter in Senora, then we count people before equipment.
Delivery: Reflective; allow the response to land
Trigger: outro

## M23 - GHOST IN THE SAGE (scripted)

### Intro

**ICE** `M23_SCENE_INTRO_01_ICE`
The radar bunker gives us cover after Cypress. We clear the outpost, but nobody calls it home because I say so.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M23_SCENE_INTRO_02_GUESS`
A door that locks is a start. A way out is better. I'll check the second one while you take the first.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M23_S1_01_ICE`
Breaching upper radar dome. Four cartel riflemen on walkway.
Delivery: 
Trigger: Gameplay stage 1 entry

**GUESS** `M23_S1_02_GUESS`
The three storage bays are secure. We can stage the heavy rigs in this yard.
Delivery: 
Trigger: Gameplay stage 3 completion

**GOHAN** `M23_S1_03_GOHAN`
Generator room online. We have our command center in the desert.
Delivery: 
Trigger: Gameplay stage 4 completion

### Aftermath

**GOHAN** `M23_SCENE_OUTRO_01_GOHAN`
The bunker holds. I can work here. We still need fuel and money to keep three people alive out here.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M23_SCENE_OUTRO_02_GUESS`
Then we recover only what we can guard from the Alamo. No more betting the house on the whole pile.
Delivery: Reflective; allow the response to land
Trigger: outro

## M24 - LIQUID GOLD (scripted)

### Intro

**GOHAN** `M24_SCENE_INTRO_01_GOHAN`
Five tons first. The rest stays hidden. We need operating money, not a convoy that advertises the entire haul.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M24_SCENE_INTRO_02_ICE`
If deputies arrive, call them out. Keep that cable steady; I've got the ridge.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M24_S1_01_GUESS`
First five tons recovered. Secure the haul; it still has to reach the radar base.
Delivery: 
Trigger: Gameplay stage 2 completion

**ICE** `M24_S1_02_ICE`
Sheriff cruisers coming over the ridge! They aren't taking this haul!
Delivery: 
Trigger: Gameplay stage 2 entry

**GOHAN** `M24_S1_03_GOHAN`
Crates loaded! Cash reserves replenished for Blaine operations.
Delivery: 
Trigger: Gameplay stage 3 completion

### Aftermath

**GUESS** `M24_SCENE_OUTRO_01_GUESS`
Enough recovered to keep the lights on. The rest can wait. I'm keeping track of what surviving actually costs.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M24_SCENE_OUTRO_02_ICE`
Those deputies knew too much. Assume every isolated road can turn into a bounty trap.
Delivery: Reflective; allow the response to land
Trigger: outro

## M25 - BOUNTY HUNTERS' CANYON (scripted)

### Intro

**ICE** `M25_SCENE_INTRO_01_ICE`
Aegis put five million on us. The deputies in Raton aren't making an arrest. I'm taking the high ground.
Delivery: Briefing; intent before tactics
Trigger: intro

**GOHAN** `M25_SCENE_INTRO_02_GOHAN`
Send the route before you move. You don't have to keep the danger to yourself to keep us out of it.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M25_S1_01_ICE`
Deputies blocked the southern pass. Detonating fuel tanker on bridge.
Delivery: 
Trigger: Gameplay stage 2 entry

**GUESS** `M25_S1_02_GUESS`
The extraction boat is waiting below, Ice. Use the parachute; I'll talk you down over the radio.
Delivery: 
Trigger: Gameplay stage 4 entry

**ICE** `M25_S1_03_ICE`
In the boat. Getting out of this gorge before another wave arrives.
Delivery: 
Trigger: Gameplay stage 5 completion

### Aftermath

**ICE** `M25_SCENE_OUTRO_01_ICE`
I nearly did it again. Went quiet so neither of you would hear me scared. Next time, I'll make the call.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M25_SCENE_OUTRO_02_GUESS`
Make it early. Their spotters are over the Alamo now. We protect the water together.
Delivery: Reflective; allow the response to land
Trigger: outro

## M26 - THE ALAMO SCRAMBLE (scripted)

### Intro

**GUESS** `M26_SCENE_INTRO_01_GUESS`
Those planes are searching for the submerged gold. I'll take the Lazer. Gohan, keep them from calling in a fix.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M26_SCENE_INTRO_02_ICE`
I'm listening to your channel. If you need me, you get an answer. That's the part I can promise.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M26_S1_01_GUESS`
Two spotters over the Alamo. Taking the Lazer up to stop them finding our stash.
Delivery: 
Trigger: Gameplay stage 2 entry

**GOHAN** `M26_S1_02_GOHAN`
Spotter one splashed! Spotter two is diving for Grapeseed-kill him!
Delivery: 
Trigger: Gameplay stage 3 entry

**GUESS** `M26_S1_03_GUESS`
Both spotters down in the lake! Our Alamo stash remains a ghost.
Delivery: 
Trigger: Gameplay stage 4 completion

### Aftermath

**GOHAN** `M26_SCENE_OUTRO_01_GOHAN`
The spotters' traffic points to an Aegis charter. Its flight ledger may show where their arms pipeline goes.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M26_SCENE_OUTRO_02_GUESS`
Then we intercept it with an actual pickup plan. Ice isn't jumping into a promise with no boat underneath.
Delivery: Reflective; allow the response to land
Trigger: outro

## M27 - FLIGHT RISK (scripted)

### Intro

**GUESS** `M27_SCENE_INTRO_01_GUESS`
I put you over the Shamal. Gohan has the sea pickup. Check your parachute twice; I want an argument about this later.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M27_SCENE_INTRO_02_ICE`
You both get a vote. If either of you says the approach is wrong, I stay in the plane.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M27_S1_01_GUESS`
Taking the stunt plane up. I need to hold between twelve and sixty metres from the Shamal for the transfer.
Delivery: Shouting over roaring radial biplane engine and 160-knot gale
Trigger: Gameplay stage 2 entry

**ICE** `M27_S1_02_ICE`
Transfer complete. In the cabin. Going for the flight locker.
Delivery: Breathing through oxygen mask
Trigger: Gameplay stage 2 completion

**ICE** `M27_S2_03_ICE`
The ledger is in this locker. Hold the jet steady while I secure it.
Delivery: Plasma torch hissing, hull metal ripping
Trigger: Gameplay stage 3 entry

**GOHAN** `M27_S2_04_GOHAN`
The jet is losing its nose. Ice, bail out now and deploy your parachute. The boat is marked below.
Delivery: Monitoring black box telemetry on ground
Trigger: Gameplay stage 4 entry

**ICE** `M27_S2_05_ICE`
Ledger secured. Clear of the jet. Gohan, keep the boat where I can see it.
Delivery: Kicking emergency door out into open sky
Trigger: Gameplay stage 4 completion

**GUESS** `M27_S2_06_GUESS`
You made the pickup. That ledger tells us who keeps Aegis in the air.
Delivery: Diving plane alongside chute
Trigger: Gameplay stage 5 completion

### Aftermath

**GOHAN** `M27_SCENE_OUTRO_01_GOHAN`
The flight ledger connects the northern relays to an offshore installation. First we cut the network tracking us.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M27_SCENE_OUTRO_02_ICE`
I heard your boat before I saw it. For once I knew somebody was waiting. Don't make a joke yet, Guess.
Delivery: Reflective; allow the response to land
Trigger: outro

## M28 - OFF THE GRID (scripted)

### Intro

**GOHAN** `M28_SCENE_INTRO_01_GOHAN`
The charter's ledger identifies this repeater. Cut it and the northern drone teams lose their coordinated picture.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M28_SCENE_INTRO_02_ICE`
I cover your descent. If the splice takes longer, tell me. Silence isn't the same as having it handled.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M28_S1_01_GOHAN`
Surge unit connected. Hold them off while I bring the relay down.
Delivery: 
Trigger: Gohan begins the timed relay splice

**ICE** `M28_S1_02_ICE`
Aegis patrol responding. Gohan, finish the splice. I've got the approach.
Delivery: 
Trigger: Response squad engages after initial yard clearance

**GOHAN** `M28_S1_03_GOHAN`
Dish offline. Their northern teams just lost the shared feed. Let's get clear.
Delivery: 
Trigger: Relay splice completed

### Aftermath

**GUESS** `M28_SCENE_OUTRO_01_GUESS`
That buys the bunker breathing room, not permanent invisibility. We still need fuel before its generator gives out.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M28_SCENE_OUTRO_02_GOHAN`
Then we steal power we can store. I'm done making plans that depend on nobody noticing us forever.
Delivery: Reflective; allow the response to land
Trigger: outro

## M29 - DUST & DIESEL (scripted)

### Intro

**GUESS** `M29_SCENE_INTRO_01_GUESS`
The bunker needs fuel. We take a road tanker from the rail depot and leave the rest. Enough to keep the lights on.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M29_SCENE_INTRO_02_ICE`
I'll handle the transfer valve. Wait for my call before you pull out. No guessing what the other man has finished.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M29_S1_01_GUESS`
Depot's clear. I'll take the road tanker. Ice, open the transfer valve.
Delivery: 
Trigger: Rail-depot guards cleared

**ICE** `M29_S1_02_ICE`
Fuel transferred. Valve closed. Guess, get it to the bunker before another patrol arrives.
Delivery: 
Trigger: Ice completes the transfer at the depot

**GUESS** `M29_S1_03_GUESS`
Tanker parked at the bunker. Keep the generator running; I'll keep track of the gauge.
Delivery: 
Trigger: Specified tanker delivered and unloaded

### Aftermath

**GOHAN** `M29_SCENE_OUTRO_01_GOHAN`
Generator reserves secured. The satellite gear still needs parts before I can read the offshore traffic cleanly.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M29_SCENE_OUTRO_02_GUESS`
I can run the ridge with those parts. I'll tell you when it stops feeling like a delivery.
Delivery: Reflective; allow the response to land
Trigger: outro

## M30 - REDLINE RIDGE (scripted)

### Intro

**GUESS** `M30_SCENE_INTRO_01_GUESS`
The satellite parts have to reach the bunker. Mount Josiah avoids the highway checks, but it gives me less room to recover.
Delivery: Briefing; intent before tactics
Trigger: intro

**GOHAN** `M30_SCENE_INTRO_02_GOHAN`
I chose the route from a map. You can reject it from the driver's seat. I won't call that failing the plan.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M30_S1_01_GUESS`
Loose shale and a gunship behind me. Gohan, I need that sheltered route now.
Delivery: 
Trigger: Armed helicopter begins pursuit

**GOHAN** `M30_S1_02_GOHAN`
Take the marked bend into the canyon. The rock walls will give you cover.
Delivery: 
Trigger: Truck reaches ridge descent checkpoint

**GUESS** `M30_S1_03_GUESS`
Parts are at the bunker, intact. Next delivery needs a bigger travel allowance.
Delivery: 
Trigger: Specified parts truck delivered and unloaded

### Aftermath

**GUESS** `M30_SCENE_OUTRO_01_GUESS`
Parts delivered. Next time we walk through the drop-offs before you call a line on a map a road.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M30_SCENE_OUTRO_02_ICE`
Agreed. While Gohan fits them, we reinforce the perimeter. We can't afford another Cypress.
Delivery: Reflective; allow the response to land
Trigger: outro

## M31 - THE IRON PERIMETER (future gameplay)

### Intro

**ICE** `M31_SCENE_INTRO_01_ICE`
The bunker survived because they haven't committed a full assault. These defenses buy warning and a route to withdraw.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M31_SCENE_INTRO_02_GUESS`
Good. Build an exit into the defense. I don't want another place we have to love until it kills us.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M31_S1_01_ICE`
Claymores live on access gulley. Let them walk into the kill zone.
Delivery: 
Trigger: Minefield set

**GOHAN** `M31_S1_02_GOHAN`
CIWS tracking four incoming technicals. Engaging auto-fire!
Delivery: 
Trigger: Turret fire

**ICE** `M31_S1_03_ICE`
Ambush stopped. Perimeter held this time. Restock it, and keep the withdrawal route open.
Delivery: 
Trigger: Perimeter held

### Aftermath

**GOHAN** `M31_SCENE_OUTRO_01_GOHAN`
Perimeter is ready. Offshore defenses still outclass us; Zancudo has the EMP warheads that could change that.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M31_SCENE_OUTRO_02_ICE`
Then we take the smallest team and the clearest route. No one earns trust by taking every risk himself.
Delivery: Reflective; allow the response to land
Trigger: outro

## M32 - BLACK SITE ZANCUDO (future gameplay)

### Intro

**GOHAN** `M32_SCENE_INTRO_01_GOHAN`
The rig's defenses need more than my portable pulse. Zancudo's warheads give us a way to shut them down.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M32_SCENE_INTRO_02_ICE`
We'll get the hardware. We still need someone who knows what it protects. Weapons aren't an intelligence plan.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M32_S1_01_GOHAN`
Intake grates cut. Warhead storage is behind door four.
Delivery: 
Trigger: Sub-level dive

**ICE** `M32_S1_02_ICE`
Warheads secured in pelican cases. Guess, bring boat to estuary!
Delivery: 
Trigger: Zancudo escape

**GUESS** `M32_S1_03_GUESS`
Estuary pickup clean! We got military EMP warheads in our hands.
Delivery: 
Trigger: Base egress

### Aftermath

**GOHAN** `M32_SCENE_OUTRO_01_GOHAN`
Warheads secured. We still lack access codes. An Aegis engineer named Ramos is marked for execution on the flats.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M32_SCENE_OUTRO_02_GUESS`
Then we go for Ramos as a person first. If he can't give us anything, we still bring him back.
Delivery: Reflective; allow the response to land
Trigger: outro

## M33 - THE INFORMANT'S GRAVE (future gameplay)

### Intro

**ICE** `M33_SCENE_INTRO_01_ICE`
Ramos is being taken to an execution site. We interrupt it now. Questions can wait until he's out of their hands.
Delivery: Briefing; intent before tactics
Trigger: intro

**GOHAN** `M33_SCENE_INTRO_02_GOHAN`
I know what it's like when a powerful employer decides your name should disappear. Don't let them write his ending.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M33_S1_01_ICE`
Firing smoke canisters! Blind their firing line!
Delivery: 
Trigger: Smoke ambush

**GUESS** `M33_S1_02_GUESS`
Ramos is in the trunk! Floor it before the gunships arrive!
Delivery: 
Trigger: Salt flat rally

**GOHAN** `M33_S1_03_GOHAN`
Ramos is breathing, but he's hurt. Get him through the wind farm. The codes can wait.
Delivery: 
Trigger: Code acquired

### Aftermath

**GUESS** `M33_SCENE_OUTRO_01_GUESS`
He's alive, but he's hurt. The half-track gets him through the wind farm. Nobody asks him for codes on a stretcher.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M33_SCENE_OUTRO_02_ICE`
You heard him. We guard the man before we spend what he knows.
Delivery: Reflective; allow the response to land
Trigger: outro

## M34 - MUD & IRON (future gameplay)

### Intro

**GUESS** `M34_SCENE_INTRO_01_GUESS`
Ramos needs a way through the storm. The half-track can take hits; he can't. Call targets without shouting over him.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M34_SCENE_INTRO_02_ICE`
I'll cover your side. Gohan, stay with Ramos. He ought to hear one calm voice in this thing.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M34_S1_01_GUESS`
Visibility is twenty feet! The wind turbines are spinning like guillotines! Ice, snipers on tower four!
Delivery: Wipers scraping red desert sand, twin turbochargers spooling
Trigger: Sandstorm driving

**ICE** `M34_S1_02_ICE`
Turbine catwalk suppressed! Keep this half-track moving down the service gully, Guess!
Delivery: Heavy twin .50-cal hammering rhythmically
Trigger: Turret barrage

**GOHAN** `M34_S1_03_GOHAN`
Seismic charges primed on the canyon walls! Detonating trail in three... two... one!
Delivery: Operating rear detonator console
Trigger: Seismic prime

**GOHAN** `M34_S1_04_GOHAN`
Rockslide triggered! The entire rear ridgeline just buried six pursuing Insurgents!
Delivery: 
Trigger: Rockslide triggered

**GUESS** `M34_S1_05_GUESS`
Bunker tunnel mouth dead ahead! Half-track pulled inside! Defector is alive!
Delivery: 
Trigger: Safe arrival

### Aftermath

**GOHAN** `M34_SCENE_OUTRO_01_GOHAN`
He's safe enough to talk when he's ready. His rig codes help, but an extraction still needs protection from aircraft.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M34_SCENE_OUTRO_02_GUESS`
Chianski convoy has a technical we can use. We get it because this rescue has to have a way home.
Delivery: Reflective; allow the response to land
Trigger: outro

## M35 - THE CHIANSKI AMBUSH (future gameplay)

### Intro

**ICE** `M35_SCENE_INTRO_01_ICE`
The convoy carries our anti-air cover for Paleto. Disable the escort without destroying what we came to take.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M35_SCENE_INTRO_02_GUESS`
And if the gun truck burns, we change the extraction. We don't pretend a missing piece will appear under fire.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M35_S1_01_ICE`
Lead truck hit the mine! Box them in the canyon!
Delivery: 
Trigger: Mountain trap

**GUESS** `M35_S1_02_GUESS`
Anti-aircraft technical captured! Driving it into the bunker!
Delivery: 
Trigger: Technical hijack

**GOHAN** `M35_S1_03_GOHAN`
Munitions secured. Heavy firepower ready for the rig.
Delivery: 
Trigger: Munitions stored

### Aftermath

**GOHAN** `M35_SCENE_OUTRO_01_GOHAN`
Surface cover acquired. I'll survey the rig underwater. Ramos can explain a system; I still have to see the approach.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M35_SCENE_OUTRO_02_ICE`
Send us the footage too. This time the risk assessment belongs to everybody.
Delivery: Reflective; allow the response to land
Trigger: outro

## M36 - DEEP WELL RECON (future gameplay)

### Intro

**GOHAN** `M36_SCENE_INTRO_01_GOHAN`
Ramos's codes get us access, not a clear sea. I'm mapping the sonar and depth-charge tubes before we commit.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M36_SCENE_INTRO_02_GUESS`
Call what worries you while you're looking at it. Don't save the bad news for the finished diagram.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M36_S1_01_GOHAN`
Ballast columns are reinforced concrete. Seismic charges must go on column 2.
Delivery: 
Trigger: Rig mapping

**ICE** `M36_S1_02_ICE`
Depth charge dropped north! Evade, Gohan!
Delivery: 
Trigger: Underwater evasion

**GOHAN** `M36_S1_03_GOHAN`
Sonar map completed. All demolition points marked.
Delivery: 
Trigger: Recon complete

### Aftermath

**GOHAN** `M36_SCENE_OUTRO_01_GOHAN`
We need a screen over the surface approach and charges below it. Start with smoke aircraft at McKenzie.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M36_SCENE_OUTRO_02_ICE`
You came back with reasons to wait. That's a successful recon. We build what the plan actually needs.
Delivery: Reflective; allow the response to land
Trigger: outro

## M37 - THE GRAPESEED HARVEST (future gameplay)

### Intro

**GUESS** `M37_SCENE_INTRO_01_GUESS`
The crop dusters lay smoke across the rig approach. They won't make us invisible; they shorten the gunners' view.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M37_SCENE_INTRO_02_ICE`
Mark the gaps too. I want the extraction route to make sense when the wind doesn't cooperate.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M37_S1_01_GUESS`
Drop tanks welded. One pass will blanket the entire rig in whiteout.
Delivery: 
Trigger: Plane retrofit

**ICE** `M37_S1_02_ICE`
Hangar secured. Flying both birds back to Sandy Shores.
Delivery: 
Trigger: Biplane delivery

**GOHAN** `M37_S1_03_GOHAN`
Smoke tanks are ready. They'll break visual contact, but radar and thermal crews can still find us.
Delivery: 
Trigger: Smoke ready

### Aftermath

**GUESS** `M37_SCENE_OUTRO_01_GUESS`
Aircraft fitted. The screen gives Gohan a chance to place charges, if we can get the right explosives.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M37_SCENE_OUTRO_02_GOHAN`
Davis Quartz has seismic stock. We take enough for the surveyed columns, not enough to impress each other.
Delivery: Reflective; allow the response to land
Trigger: outro

## M38 - BLOOD IN THE QUARRY (future gameplay)

### Intro

**ICE** `M38_SCENE_INTRO_01_ICE`
The quarry charges fit the rig's stabilizers. Gohan measured the placement. We take the crates he specified.
Delivery: Briefing; intent before tactics
Trigger: intro

**GOHAN** `M38_SCENE_INTRO_02_GOHAN`
And we confirm everybody is clear before detonation. I'm not calling people acceptable losses because a drawing looks neat.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M38_S1_01_ICE`
Commercial charges in pit 3. Keep haulers rolling as cover.
Delivery: 
Trigger: Quarry shootout

**GOHAN** `M38_S1_02_GOHAN`
Charges loaded! Four hundred pounds of high-grade explosive!
Delivery: 
Trigger: Explosive secure

**GUESS** `M38_S1_03_GUESS`
Hauler clear of the quarry! Demolition charges locked down.
Delivery: 
Trigger: Quarry escape

### Aftermath

**GUESS** `M38_SCENE_OUTRO_01_GUESS`
Charges secured. We still have to stop the rig calling the mainland the second it feels a tremor.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M38_SCENE_OUTRO_02_GOHAN`
Its undersea cable is next. One system at a time, with a reason for each one.
Delivery: Reflective; allow the response to land
Trigger: outro

## M39 - THE PALETO CABLE (future gameplay)

### Intro

**GOHAN** `M39_SCENE_INTRO_01_GOHAN`
This cable carries the rig's mainland traffic. Sever it and their response has to use a slower, noisier channel.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M39_SCENE_INTRO_02_ICE`
You keep a return line to us. Cutting their contact doesn't mean cutting yours.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M39_S1_01_GOHAN`
Thermite clamp ignited under thirty fathoms. Rig is cut off from mainland.
Delivery: 
Trigger: Cable severed

**GUESS** `M39_S1_02_GUESS`
Radar confirms rig's external communications flatlined. It's time.
Delivery: 
Trigger: Surface beacon

**ICE** `M39_S1_03_ICE`
Final countdown initiated. Moving all strike teams to the sea cave.
Delivery: 
Trigger: Sea cave muster

### Aftermath

**GOHAN** `M39_SCENE_OUTRO_01_GOHAN`
Mainland link severed. I can do the underwater work. I still need to know the boats can take us off that platform.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M39_SCENE_OUTRO_02_GUESS`
Bring your drawings to the sea cave. We'll put the armor where your exit actually reaches the water.
Delivery: Reflective; allow the response to land
Trigger: outro

## M40 - THE PHANTOM RIGGING (future gameplay)

### Intro

**GUESS** `M40_SCENE_INTRO_01_GUESS`
These boats are the last part we touch after the rig. Plates, glass, engines. Test them loaded, not empty.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M40_SCENE_INTRO_02_ICE`
Put my ammunition on the scale. If I ask for armor and speed, I have to admit what my gear costs.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M40_S1_01_GUESS`
Reinforced steel prows locked. These boats can ram straight through pontoons.
Delivery: 
Trigger: Boat armoring

**ICE** `M40_S1_02_ICE`
Mounted machine gun brackets fitted on both sterns.
Delivery: 
Trigger: Gun mounts fitted

**GOHAN** `M40_S1_03_GOHAN`
Armor fitted and both boats water-tested. Keep the engines covered; this isn't a guarantee.
Delivery: 
Trigger: Fleet ready

### Aftermath

**GOHAN** `M40_SCENE_OUTRO_01_GOHAN`
Boats are ready. Bradley can still authorize mainland air strikes. We need his authority and his access card removed.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M40_SCENE_OUTRO_02_ICE`
I'll go to the lodge. You'll get the route and the return time before I leave.
Delivery: Reflective; allow the response to land
Trigger: outro

## M41 - THE GENERAL'S WIRE (future gameplay)

### Intro

**ICE** `M41_SCENE_INTRO_01_ICE`
Bradley controls military support for the rig. His card opens the command level. Both are reasons this cannot wait.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M41_SCENE_INTRO_02_GUESS`
Then come back when the job is finished. You don't owe us another mountain full of bodies to prove you're useful.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M41_S1_01_ICE`
General Bradley neutralized. Recovered his biometric security card.
Delivery: 
Trigger: Silenced hit

**GOHAN** `M41_S1_02_GOHAN`
Card grants root access to rig vault. Get off the mountain.
Delivery: 
Trigger: Lodge escape

**GUESS** `M41_S1_03_GUESS`
Snowmobile pickup waiting at the trail head. We are clear.
Delivery: 
Trigger: Snowmobile egress

### Aftermath

**ICE** `M41_SCENE_OUTRO_01_ICE`
Bradley can't authorize the strike. Gohan gets the card. I'm coming back with what we agreed to take.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M41_SCENE_OUTRO_02_GOHAN`
We deploy the sub next. Thanks for saying you're coming back instead of just going silent.
Delivery: Reflective; allow the response to land
Trigger: outro

## M42 - SKYFALL DELIVERY (future gameplay)

### Intro

**GUESS** `M42_SCENE_INTRO_01_GUESS`
The Titan gets the Kraken beyond the watched coast. Gohan, talk through each latch before I open the cargo ramp.
Delivery: Briefing; intent before tactics
Trigger: intro

**GOHAN** `M42_SCENE_INTRO_02_GOHAN`
I'm trusting you with the part I can't check from inside the hull. If I'm quiet, it's concentration, not permission.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M42_S1_01_GUESS`
Cargo ramp open! Sub is airborne! Parachutes deployed!
Delivery: 
Trigger: Aerial airdrop

**GOHAN** `M42_S1_02_GOHAN`
Splashdown confirmed! Ballast tanks flooding-diving for the rig.
Delivery: 
Trigger: Ocean splashdown

**ICE** `M42_S1_03_ICE`
Gohan is in the water. Moving into flight position with the chopper.
Delivery: 
Trigger: Chopper ready

### Aftermath

**GUESS** `M42_SCENE_OUTRO_01_GUESS`
Sub deployed. I'm still shaking. Nobody put that in the flight log.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M42_SCENE_OUTRO_02_ICE`
Put it in. Then somebody reading the plan knows what it asks of a person. We regroup at staging.
Delivery: Reflective; allow the response to land
Trigger: outro

## M43 - STAGING PALETO (future gameplay)

### Intro

**ICE** `M43_SCENE_INTRO_01_ICE`
Sub below, gunboats on the surface, helicopter on extraction. Confirm all three before anybody crosses the perimeter.
Delivery: Briefing; intent before tactics
Trigger: intro

**GOHAN** `M43_SCENE_INTRO_02_GOHAN`
Ramos gave us codes because we brought him out alive. We keep proving that was the right decision.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M43_S1_01_ICE`
Tonight we break their fortress and take our five hundred million.
Delivery: 
Trigger: Final muster

**GUESS** `M43_S1_02_GUESS`
Annihilator ready. When that rig burns, I'll be over the derrick.
Delivery: 
Trigger: Chopper spool

**GOHAN** `M43_S1_03_GOHAN`
Submersible holding at forty fathoms. Awaiting green light.
Delivery: 
Trigger: Sub ready

### Aftermath

**GUESS** `M43_SCENE_OUTRO_01_GUESS`
Final check: the exit stays open even if the vault doesn't. We came north as three people. We leave with three.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M43_SCENE_OUTRO_02_ICE`
Agreed. Gohan starts beneath the platform. The rest of us move on his call.
Delivery: Reflective; allow the response to land
Trigger: outro

## M44 - PALETO DEEP-SEA: SUB-SURFACE (future gameplay)

### Intro

**GOHAN** `M44_SCENE_INTRO_01_GOHAN`
I'm approaching the stabilizers. I disable the sea defenses before Ice lands. Nobody jumps ahead because I'm underwater.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M44_SCENE_INTRO_02_ICE`
Your clock. Your call. I'll wait where Guess can still pull away.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M44_S1_01_GOHAN`
Charges clamped on pylon two! Torpedo tubes disabled! Clear for air assault!
Delivery: 
Trigger: Pylon charges

**ICE** `M44_S1_02_ICE`
Copy that, Gohan. Guess and I are inbound on the chopper!
Delivery: 
Trigger: Inbound air

**GUESS** `M44_S1_03_GUESS`
Sixty-knot typhoon winds over the helipad! Coming in hot!
Delivery: 
Trigger: Helipad approach

### Aftermath

**GOHAN** `M44_SCENE_OUTRO_01_GOHAN`
Charges placed, sea defenses disabled. Keep the structure standing until everyone has a route off it.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M44_SCENE_OUTRO_02_GUESS`
Now I bring Ice to the helipad. Hold a channel open. We finally learned to wait for each other.
Delivery: Reflective; allow the response to land
Trigger: outro

## M45 - PALETO DEEP-SEA: HELIPAD BREACH (future gameplay)

### Intro

**GUESS** `M45_SCENE_INTRO_01_GUESS`
Sea defenses are down, but the helipad guns still see us. Ice, you get one landing. Say if it isn't yours.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M45_SCENE_INTRO_02_ICE`
Bring me inside the rail. I'll clear the route to Gohan. No hero jump to save five seconds.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M45_S1_01_ICE`
Touchdown on helipad! RPG bunker neutralized! Breaching elevator!
Delivery: 
Trigger: Helipad storm

**GUESS** `M45_S1_02_GUESS`
Holding hover above the crane! Watch the sniper catwalks!
Delivery: 
Trigger: Air cover

**GOHAN** `M45_S1_03_GOHAN`
Surfaced at lower moonpool! Moving to meet you on the command deck!
Delivery: 
Trigger: Moonpool breach

### Aftermath

**ICE** `M45_SCENE_OUTRO_01_ICE`
Helipad secure. Command deck next. Gohan, bring Bradley's card. Guess, keep that extraction aircraft breathing.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M45_SCENE_OUTRO_02_GOHAN`
I'm on my way. When this door opens, we copy the evidence before we count the bonds.
Delivery: Reflective; allow the response to land
Trigger: outro

## M46 - PALETO DEEP-SEA: VAULT CRACK (future gameplay)

### Intro

**GOHAN** `M46_SCENE_INTRO_01_GOHAN`
The command vault has the political escrow ledger. Bradley's card gets us in. The files explain who paid for all this.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M46_SCENE_INTRO_02_ICE`
Take a copy we can prove is theirs. Mateo's word started us moving. It won't finish the fight.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M46_S1_01_GOHAN`
Vault open! Half a billion in bearer bonds and all their bribery ledgers!
Delivery: 
Trigger: Vault plunder

**ICE** `M46_S1_02_ICE`
Pack the bags! Gohan, set the seismic detonator timer!
Delivery: 
Trigger: Timer armed

**GUESS** `M46_S1_03_GUESS`
Derrick structure is failing! Get to the edge for the jump!
Delivery: 
Trigger: Jump alert

### Aftermath

**GOHAN** `M46_SCENE_OUTRO_01_GOHAN`
Five hundred million in bonds, separate from the port gold. The ledger names officials. That's what makes them afraid.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M46_SCENE_OUTRO_02_GUESS`
Carry the evidence and whatever weight leaves room for a person. We have to get off this rig before we spend anything.
Delivery: Reflective; allow the response to land
Trigger: outro

## M47 - PALETO DEEP-SEA: COLLAPSE (future gameplay)

### Intro

**GOHAN** `M47_SCENE_INTRO_01_GOHAN`
The charges are live. Once I trigger them, the platform stops being a place we can search. Confirm your exit.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M47_SCENE_INTRO_02_GUESS`
Confirmed. Ice, answer in words. I can't hear a nod through a radio.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M47_S1_01_GOHAN`
Pylons snapping! Superstructure going under! JUMP!
Delivery: 
Trigger: Rig collapse

**ICE** `M47_S1_02_ICE`
Chutes away! Hit the water and swim for the gunboats!
Delivery: 
Trigger: Ocean jump

**GUESS** `M47_S1_03_GUESS`
Picked up Ice and Gohan! Speedboats throttling up!
Delivery: 
Trigger: Boats mounted

### Aftermath

**ICE** `M47_SCENE_OUTRO_01_ICE`
All three clear. We have the ledger. Now we cross the blockade before Aegis seals the coast.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M47_SCENE_OUTRO_02_GOHAN`
We're taking proof back south, not just bringing the same war to another hiding place.
Delivery: Reflective; allow the response to land
Trigger: outro

## M48 - THE ROAD BACK SOUTH (future gameplay)

### Intro

**GUESS** `M48_SCENE_INTRO_01_GUESS`
The boats brought us ashore. The technicals take us through the outer blockade. Save fuel for the county line.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M48_SCENE_INTRO_02_ICE`
We go back because their political ledger gives us a chance to end the contract. I won't call revenge a route home.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M48_S1_01_GUESS`
We got their money, we got their ledgers, and their rig is sunk! HEAD SOUTH!
Delivery: 
Trigger: Highway breach

**ICE** `M48_S1_02_ICE`
Los Santos is right ahead. Time to end this war where it started.
Delivery: 
Trigger: Act II finale

**GOHAN** `M48_S1_03_GOHAN`
The ledgers name their people in Los Santos. We go home with evidence this time.
Delivery: 
Trigger: Act II close

### Aftermath

**GOHAN** `M48_SCENE_OUTRO_01_GOHAN`
Outer cordon cleared. Chumash is a second line, not an open road. Aegis knows where the coast narrows.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M48_SCENE_OUTRO_02_GUESS`
Then nobody celebrates early. Get the turbine Granger ready for the next crossing.
Delivery: Reflective; allow the response to land
Trigger: outro

## M49 - RETURN TO THE CONCRETE (future gameplay)

### Intro

**GUESS** `M49_SCENE_INTRO_01_GUESS`
Chumash is the county-line barricade. This is where the Granger we built together has to earn its way back into town.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M49_SCENE_INTRO_02_ICE`
And if it doesn't, we leave it. Cypress taught me a place can matter without being worth more than a person.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M49_S1_01_GUESS`
Chumash roadblock in sight! Concrete barriers and two armored APCs! Hold your breath, I'm hitting the seam!
Delivery: Tires screaming on wet asphalt, engine roaring
Trigger: Roadblock approach

**ICE** `M49_S1_02_ICE`
Spotlight towers one and two neutralized! Seam is clear, Guess-HIT IT!
Delivery: Leaning out window with heavy thermal sniper
Trigger: Tower explosion

**GOHAN** `M49_S1_03_GOHAN`
Local cell towers jammed! Police dispatch won't know the checkpoint fell for ninety seconds!
Delivery: Laptop typing rapidly
Trigger: County line breach

### Aftermath

**GOHAN** `M49_SCENE_OUTRO_01_GOHAN`
We're inside Los Santos. Emergency warrants still make every patrol a threat. Municipal records is the next stop.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M49_SCENE_OUTRO_02_GUESS`
I'll drive past Davis when we can do it without bringing a gunship behind us. I want to see what's still there.
Delivery: Reflective; allow the response to land
Trigger: outro

## M50 - THE REDACTED VAULT (future gameplay)

### Intro

**GOHAN** `M50_SCENE_INTRO_01_GOHAN`
The city is distributing emergency warrants with our faces. I can stop the municipal feed. Existing patrol copies won't vanish.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M50_SCENE_INTRO_02_ICE`
Then we plan for people who still recognize us. I won't mistake a cleared screen for forgiveness.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M50_S1_01_GOHAN`
Optical splice online. Vault sub-level has six automated turrets and four guards. Ice, take the left corridor.
Delivery: Whispering through comms
Trigger: Archive infiltration

**ICE** `M50_S1_02_ICE`
Corridor secured. Plant the wipe sequence, Gohan. We have three minutes before physical backup arrives.
Delivery: Suppressed pistol double-tap
Trigger: Sentries silenced

**GOHAN** `M50_S1_03_GOHAN`
The local alerts are down. That buys us time, not an acquittal. Their backups still exist.
Delivery: 
Trigger: Warrant purge

### Aftermath

**GUESS** `M50_SCENE_OUTRO_01_GUESS`
Less coordination on the streets. Now we prepare Downtown before their contractors settle into another siege.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M50_SCENE_OUTRO_02_GOHAN`
Palmer-Taylor's grid feeds the approach. We place the blackout charges and control when that darkness starts.
Delivery: Reflective; allow the response to land
Trigger: outro

## M51 - BLACKOUT PROTOCOL (future gameplay)

### Intro

**GOHAN** `M51_SCENE_INTRO_01_GOHAN`
The transformer yard gives us a timed blackout for the downtown offensive. We need the failovers too, or it lasts seconds.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M51_SCENE_INTRO_02_GUESS`
People live under those lights. Keep the outage to the plan. I'm not celebrating a dark hospital.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M51_S1_01_ICE`
Limpet three seated on transformer bank north. Guess, watch that patrol crane!
Delivery: 
Trigger: Transformer climb

**GUESS** `M51_S1_02_GUESS`
Crane guard dropped with the silenced carbine. Last charge is armed! Move to the riverbed!
Delivery: 
Trigger: Charge plant

**GOHAN** `M51_S1_03_GOHAN`
Detonating in three... two... one... blackout.
Delivery: 
Trigger: Downtown skyline goes dark

### Aftermath

**ICE** `M51_SCENE_OUTRO_01_ICE`
Charges ready. Harrison signed the authority that keeps Aegis operating. We remove him before the tower assault.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M51_SCENE_OUTRO_02_GOHAN`
Killing a signature doesn't cancel the paperwork. Keep the ledger intact. The public has to see what he approved.
Delivery: Reflective; allow the response to land
Trigger: outro

## M52 - JUDICIAL STRIKE (future gameplay)

### Intro

**ICE** `M52_SCENE_INTRO_01_ICE`
Harrison gave Aegis immunity. His protection keeps the contract alive. This closes one route they use to renew it.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M52_SCENE_INTRO_02_GUESS`
You can call it necessary. Don't call it justice for everybody in this city. We haven't earned that word.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M52_S1_01_ICE`
Harrison is stepping past the courthouse pillars. Wind speed 4 knots west. Lining up crosshairs.
Delivery: Controlled slow breathing
Trigger: Sniper zoom

**ICE** `M52_S1_02_ICE`
Target eliminated. Escort detail in disarray! Guess, pull up to the east stairs!
Delivery: Suppressed crack
Trigger: Target killed

**GUESS** `M52_S1_03_GUESS`
Hop on! We got three Aegis cruisers closing from Olympic!
Delivery: Motorcycle sliding onto marble steps
Trigger: Superbike escape

### Aftermath

**GOHAN** `M52_SCENE_OUTRO_01_GOHAN`
Harrison is gone. Their sweep teams are moving below Pillbox. Clear the tunnels before they cut our route to Downtown.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M52_SCENE_OUTRO_02_ICE`
And if either of you has something personal left to settle, say it. Secrets make a lousy fourth passenger.
Delivery: Reflective; allow the response to land
Trigger: outro

## M53 - SUBTERRANEAN SWEEP (future gameplay)

### Intro

**GOHAN** `M53_SCENE_INTRO_01_GOHAN`
Aegis is sweeping the subway approaches. If they hold these tunnels, our tower plan has no sheltered way in or out.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M53_SCENE_INTRO_02_ICE`
Stay close enough to hear the actual answer when I ask if you're hurt. No more automatic 'I'm fine.'
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M53_S1_01_GOHAN`
Night vision active. Sweep team of eight advancing down track two. They have thermal scopes.
Delivery: 
Trigger: Night vision on

**ICE** `M53_S1_02_ICE`
Tripwires set at car three. Let them stack up at the doors... NOW!
Delivery: 
Trigger: Mine detonation

**ICE** `M53_S1_03_ICE`
Sweep team down. This corridor is clear. Keep somebody watching the next junction.
Delivery: 
Trigger: Tunnel cleared

### Aftermath

**GUESS** `M53_SCENE_OUTRO_01_GUESS`
Routes open. We need an observation point above this mess before we split across three buildings.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M53_SCENE_OUTRO_02_GOHAN`
The foreclosed Pillbox penthouse has sightlines and a transmitter. Temporary operations room. No promises about home.
Delivery: Reflective; allow the response to land
Trigger: outro

## M54 - THE PILLBOX REDOUBT (future gameplay)

### Intro

**ICE** `M54_SCENE_INTRO_01_ICE`
The penthouse overlooks Maze Bank. We use it to watch the target and coordinate the escrow breach, then we leave.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M54_SCENE_INTRO_02_GUESS`
Don't put three keys on the table this time. I know what this place is. We can talk about a home afterward.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M54_S1_01_GUESS`
Express elevator keycard cloned. We got direct access from the parking garage straight to floor forty.
Delivery: 
Trigger: Elevator hack

**ICE** `M54_S1_02_ICE`
Balcony gives uninterrupted line-of-sight on Maze Bank Tower and City Hall. This is our forward combat post.
Delivery: 
Trigger: Perch overview

**GOHAN** `M54_S1_03_GOHAN`
Satellite link live. I'm marking the patrols transmitting nearby. Quiet teams won't show up.
Delivery: 
Trigger: Radar array active

### Aftermath

**GOHAN** `M54_SCENE_OUTRO_01_GOHAN`
The transmitter is up. Three escrow terminals need overlapping access windows. Each of us will hold one.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M54_SCENE_OUTRO_02_ICE`
Then each of us gets to call a delay. Trust has to work when I can't see either of you.
Delivery: Reflective; allow the response to land
Trigger: outro

## M55 - SKYLINE DESCENT (future gameplay)

### Intro

**GOHAN** `M55_SCENE_INTRO_01_GOHAN`
Three terminals, one five-minute window. The breach locks their escrow routes; the final authorization is still at Maze Bank.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M55_SCENE_INTRO_02_ICE`
Nobody changes the timing alone. I spent too long thinking I could keep you safe by keeping you uninformed.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M55_S1_01_ICE`
Breaching penthouse one! Suppressing four executive guards!
Delivery: 
Trigger: Breach 1

**GOHAN** `M55_S1_02_GOHAN`
Terminal bypassed at Arcadius! Sixty million identified. Copying the transfer authorizations!
Delivery: 
Trigger: Breach 2

**GUESS** `M55_S1_03_GUESS`
Vault cracked at Schlongberg! Downloading secondary ledger! 40 seconds left!
Delivery: 
Trigger: Breach 3

**ICE** `M55_S1_04_ICE`
All three nodes compromised! Parachute off the roofs to the canal!
Delivery: 
Trigger: Simultaneous BASE-jump

### Aftermath

**GUESS** `M55_SCENE_OUTRO_01_GUESS`
Three terminals hit. Their ground units are coming down the river to cut our escape corridors.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M55_SCENE_OUTRO_02_GOHAN`
Escrow constrained, not cashed out. We still need the master authorization. First we keep a road out of town open.
Delivery: Reflective; allow the response to land
Trigger: outro

## M56 - IRON IN THE DRAIN (future gameplay)

### Intro

**GUESS** `M56_SCENE_INTRO_01_GUESS`
The canal is part of the final escape route. Aegis armor is trying to turn it into a sealed trench.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M56_SCENE_INTRO_02_ICE`
I'll clear the guns facing your cab. You tell me when we have enough room to leave. We don't have to own the river.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M56_S1_01_GUESS`
Two armored APCs coming north under the 4th Street bridge! Ramming speed!
Delivery: 
Trigger: Aqueduct collision

**ICE** `M56_S1_02_ICE`
Pounding the lead APC engine block! Target detonated! Watch the overpass gunners!
Delivery: 
Trigger: Turret fire

**GOHAN** `M56_S1_03_GOHAN`
Chopper down! Flood channel is clear from Olympic to the harbor!
Delivery: 
Trigger: Channel cleared

### Aftermath

**GOHAN** `M56_SCENE_OUTRO_01_GOHAN`
Canal route survives. Their coastal gunships are hunting the boats next. If they close the water, the road ends nowhere.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M56_SCENE_OUTRO_02_GUESS`
Then we defend the water exit too. I'm planning for a broken aircraft, not just a beautiful takeoff.
Delivery: Reflective; allow the response to land
Trigger: outro

## M57 - VESPUCCI FLAK (future gameplay)

### Intro

**GUESS** `M57_SCENE_INTRO_01_GUESS`
Those gunships are searching our extraction waters. Clear their patrol pattern and we keep a fallback off the coast.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M57_SCENE_INTRO_02_ICE`
You started planning exits for us before either of us learned to ask. I see that now.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M57_S1_01_GUESS`
Jumping the surf break! Lock your Stinger on that lead Maverick!
Delivery: 
Trigger: Wave ramp jump

**ICE** `M57_S1_02_ICE`
Missile away! Direct hit on tail rotor! Bird is in the water!
Delivery: 
Trigger: Heli splashdown

**GUESS** `M57_S1_03_GUESS`
Beachfront clear! Heading for the Marina storm gates!
Delivery: 
Trigger: Pier escape

### Aftermath

**GOHAN** `M57_SCENE_OUTRO_01_GOHAN`
The water route is breathing again. Cifuentes remnants are regrouping in Mirror Park. They can still hunt our families.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M57_SCENE_OUTRO_02_GUESS`
We break their local command. We don't start calling everybody on that street cartel.
Delivery: Reflective; allow the response to land
Trigger: outro

## M58 - CARTEL DECAPITATION (future gameplay)

### Intro

**ICE** `M58_SCENE_INTRO_01_ICE`
Mirror Park holds the cartel's remaining local command. Leaving it intact means leaving someone paid to keep finding us.
Delivery: Briefing; intent before tactics
Trigger: intro

**GOHAN** `M58_SCENE_INTRO_02_GOHAN`
Separate the house from the neighborhood. Davis taught all of us what happens when people stop making that distinction.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M58_S1_01_ICE`
Perimeter breached! Heavy fire on the main villa!
Delivery: 
Trigger: Cul-de-sac breach

**GOHAN** `M58_S1_02_GOHAN`
Tear gas dispersed into HVAC! Cartel capos fleeing through rear patio!
Delivery: 
Trigger: Gas deployment

**GUESS** `M58_S1_03_GUESS`
Their council's down. That breaks this command chain. Doesn't mean every crew in the city quits.
Delivery: 
Trigger: Compound cleared

### Aftermath

**GOHAN** `M58_SCENE_OUTRO_01_GOHAN`
Their command is broken. Now the rig recordings can go public without that crew intercepting the broadcast route.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M58_SCENE_OUTRO_02_GUESS`
Send the proof, including the parts that don't flatter us. If we choose what people can know, we're doing their job.
Delivery: Reflective; allow the response to land
Trigger: outro

## M59 - THE WIRE CUTTERS (future gameplay)

### Intro

**GOHAN** `M59_SCENE_INTRO_01_GOHAN`
The rig ledger and bribery recordings can show how Aegis bought this war. I'm sending copies beyond a single station.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M59_SCENE_INTRO_02_ICE`
Publish what we can substantiate. Let people see the difference between evidence and something I shouted after a gunfight.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M59_S1_01_GOHAN`
Overriding state relay! Broadcasting the offshore ledgers on all public frequencies!
Delivery: 
Trigger: Broadcast override

**ICE** `M59_S1_02_ICE`
Aegis helicopter attempting to deploy shooters on the 'W'! Sniper round away-pilot down!
Delivery: 
Trigger: Sign shootout

**GOHAN** `M59_S1_03_GOHAN`
Broadcast is repeating. Independent copies are spreading. They can't silence this by taking one tower.
Delivery: 
Trigger: Media leak live

### Aftermath

**GUESS** `M59_SCENE_OUTRO_01_GUESS`
They're moving on Davis. They heard the broadcast and they're going after the people who knew us before it.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M59_SCENE_OUTRO_02_ICE`
Then the escape waits. We left those streets once without explaining ourselves. We don't abandon them under fire.
Delivery: Reflective; allow the response to land
Trigger: outro

## M60 - SIEGE OF DAVIS (future gameplay)

### Intro

**ICE** `M60_SCENE_INTRO_01_ICE`
Aegis is attacking Davis because of us. We hold their squads out long enough for neighbors to reach cover.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M60_SCENE_INTRO_02_GUESS`
This isn't our old playground with enemies painted on it. Follow the people who live here. They know who needs help.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M60_S1_01_ICE`
They came to our neighborhood. They think they can burn Davis? Not while we breathe.
Delivery: Deep, protective fury
Trigger: Street defense start

**GUESS** `M60_S1_02_GUESS`
Roadblock set with burning muscle cars on Brouge Avenue! Bring them into the pocket! We played football right on this corner!
Delivery: 
Trigger: Brouge ambush

**GOHAN** `M60_S1_03_GOHAN`
Setting remote EMP charges under the streetlamps! When their APCs roll in, we cut their engines dead!
Delivery: 
Trigger: EMP grid set

**ICE** `M60_S1_04_ICE`
Aegis heavy infantry breaking left! Families homies covering the alley! Hold the line!
Delivery: LMG firing from church roof
Trigger: Rooftop defense

**GOHAN** `M60_S1_05_GOHAN`
Detonating underground gas main! Lead APC destroyed! They are retreating back to the freeway!
Delivery: 
Trigger: Gas explosion

**GUESS** `M60_S1_06_GUESS`
Davis stands! Nobody burns our home block!
Delivery: 
Trigger: Victory shout

### Aftermath

**GOHAN** `M60_SCENE_OUTRO_01_GOHAN`
The neighborhood held. That doesn't erase what we brought here. Keep the names of the people who helped us.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M60_SCENE_OUTRO_02_ICE`
We finish their command network, then get their guns away from these streets. The wreck at Terminal may have the next link.
Delivery: Reflective; allow the response to land
Trigger: outro

## M61 - THE BLACK BOX (future gameplay)

### Intro

**GOHAN** `M61_SCENE_INTRO_01_GOHAN`
The downed command gunship has a tactical server. Its orders can corroborate the broadcast and expose their evacuation route.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M61_SCENE_INTRO_02_ICE`
Save the records before their demolition team destroys them. We need something that survives after we're gone.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M61_S1_01_GOHAN`
Inside flooded bridge. Cutting server chassis with acoustic torch.
Delivery: 
Trigger: Flooded bridge dive

**ICE** `M61_S1_02_ICE`
Aegis divers inbound on underwater scooters! Dropping depth grenades!
Delivery: 
Trigger: Underwater defense

**GOHAN** `M61_S1_03_GOHAN`
Command server extracted! We have the exact access codes for Maze Bank Tower!
Delivery: 
Trigger: Server secured

### Aftermath

**GUESS** `M61_SCENE_OUTRO_01_GUESS`
Aegis is moving mainframes and cash on a coastal freight. Intercept that and their tower loses its last movable backup.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M61_SCENE_OUTRO_02_GOHAN`
Copies go out before the next job. We aren't carrying the only proof into another firefight.
Delivery: Reflective; allow the response to land
Trigger: outro

## M62 - STEEL HORIZON (future gameplay)

### Intro

**GUESS** `M62_SCENE_INTRO_01_GUESS`
The train is their evacuation route. Ice takes the server cars, Gohan covers the water, I handle the air pickup.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M62_SCENE_INTRO_02_ICE`
We extract the evidence and each other. I don't stay on a train to win an argument with a turret.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M62_S1_01_ICE`
On top of train car three! Flatbed has automated twin miniguns- taking them out with EMP grenades!
Delivery: 
Trigger: Train rooftop combat

**GOHAN** `M62_S1_02_GOHAN`
Speedboat holding parallel sixty yards out! Jamming missile relay drones!
Delivery: 
Trigger: Coastal boat match

**GUESS** `M62_S1_03_GUESS`
Dropping chopper sling directly between tunnel arches! Hook the cargo container, Ice!
Delivery: 
Trigger: Aerial hook in tunnel

### Aftermath

**GOHAN** `M62_SCENE_OUTRO_01_GOHAN`
Their mobile backup is secured. The tower still holds the master access. This time we know why we have to go inside.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M62_SCENE_OUTRO_02_GUESS`
And everybody knows the way back out. Say it before we touch that lobby.
Delivery: Reflective; allow the response to land
Trigger: outro

## M63 - TOWER OF GLASS (future gameplay)

### Intro

**ICE** `M63_SCENE_INTRO_01_ICE`
Maze Bank holds Aegis command and the master authorization. We break the ground barricade together before splitting inside.
Delivery: Briefing; intent before tactics
Trigger: intro

**GOHAN** `M63_SCENE_INTRO_02_GOHAN`
The broadcast is already beyond this building. Burning a server here can't take the truth back from everybody else.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M63_S1_01_GUESS`
Lobby barricade breached! Technical parked right at the security elevators!
Delivery: 
Trigger: Glass lobby breach

**ICE** `M63_S1_02_ICE`
Contractor heavy gunner on mezzanine! Neutralized with grenade launcher!
Delivery: 
Trigger: Lobby shootout

**GOHAN** `M63_S1_03_GOHAN`
Security core bypassed! Main freight elevator unlocked for the ascent!
Delivery: 
Trigger: Elevator override

### Aftermath

**GUESS** `M63_SCENE_OUTRO_01_GUESS`
Lobby breached. Elevators are unreliable and they're sealing the upper floors. We climb with a route behind us.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M63_SCENE_OUTRO_02_ICE`
You two stop me if I start chasing Vance ahead of the plan. I mean it this time.
Delivery: Reflective; allow the response to land
Trigger: outro

## M64 - THE 80TH FLOOR (future gameplay)

### Intro

**GOHAN** `M64_SCENE_INTRO_01_GOHAN`
Power is cut above us. The shafts and stairs are the route. Check the next landing before leaving the last one.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M64_SCENE_INTRO_02_GUESS`
We survived fifteen years apart. I'd still rather climb one miserable staircase with you than disappear again.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M64_S1_01_ICE`
Elevator cable severed above! Car falling down shaft-JUMP TO THE LANDING!
Delivery: 
Trigger: Falling car dodge

**GOHAN** `M64_S1_02_GOHAN`
Floors 70 to 80 cleared! Only executive security left on the penthouse level!
Delivery: 
Trigger: Stairwell push

**GUESS** `M64_S1_03_GUESS`
Door blown open! We are on the executive floor!
Delivery: 
Trigger: Executive breach

### Aftermath

**ICE** `M64_SCENE_OUTRO_01_ICE`
Boardroom ahead. If Vance offers one of us a way out, the others hear the offer too.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M64_SCENE_OUTRO_02_GOHAN`
And the answer. No more making a decision for someone because we're afraid of what he'll choose.
Delivery: Reflective; allow the response to land
Trigger: outro

## M65 - EXECUTIVE PRIVILEGE (future gameplay)

### Intro

**ICE** `M65_SCENE_INTRO_01_ICE`
Vance is behind that glass. We need his master authorization to finish the financial shutdown, not another speech about winning.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M65_SCENE_INTRO_02_GUESS`
Then get what we need and come back through this door. Whatever he knows about your past, he doesn't get your future.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**VANCE** `M65_S1_01_ENEMY`
You boys think you won? You're street thugs from Davis! You can't beat the system!
Delivery: 
Trigger: Boardroom standoff

**ICE** `M65_S1_02_ICE`
The system didn't grow up on our streets, Vance. It's over.
Delivery: 
Trigger: Vance executed

**GOHAN** `M65_S1_03_GOHAN`
Biometrics accepted! Escrow control is open. I've got the authorizations; we finish the transfer outside.
Delivery: 
Trigger: Financial collapse

### Aftermath

**GOHAN** `M65_SCENE_OUTRO_01_GOHAN`
Master access secured. The escrow transfer still needs a live route outside their tower. We move before the gunships pin us.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M65_SCENE_OUTRO_02_ICE`
I thought seeing him fall would make it quiet. It didn't. Let's get out. I want to hear you two arguing in the truck.
Delivery: Reflective; allow the response to land
Trigger: outro

## M66 - THE SPIRE EVACUATION (future gameplay)

### Intro

**GUESS** `M66_SCENE_INTRO_01_GUESS`
The roof is burning and the gunships own the landing pads. Check your chutes. Del Perro is the pickup.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M66_SCENE_INTRO_02_ICE`
Nobody jumps until all three answer. No one earns an ending by staying on a roof alone.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M66_S1_01_GUESS`
Choppers circling the spire! Only way off this tower is into the sky! JUMP!
Delivery: 
Trigger: Spire jump

**ICE** `M66_S1_02_ICE`
Pulling chute at eight hundred feet! Gliding between the Arcadius towers!
Delivery: 
Trigger: Canyon parachute

**GOHAN** `M66_S1_03_GOHAN`
Touchdown on the Del Perro connector! Extraction semi-truck is waiting!
Delivery: 
Trigger: Highway landing

### Aftermath

**GOHAN** `M66_SCENE_OUTRO_01_GOHAN`
We're off the tower. I still need a clean connection in the extraction truck to route the escrow beyond their reach.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M66_SCENE_OUTRO_02_GUESS`
Then I keep the truck moving. You've carried the file this far. You don't have to carry the road too.
Delivery: Reflective; allow the response to land
Trigger: outro

## M67 - SCORCHED GRID (future gameplay)

### Intro

**GOHAN** `M67_SCENE_INTRO_01_GOHAN`
Maze Bank gave us authorization. From this truck I can route the rig's escrow into the accounts prepared for disappearance.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M67_SCENE_INTRO_02_ICE`
Keep the evidence separate from the money. A payoff mustn't be the reason the record disappears.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `M67_S1_01_GOHAN`
Transfer confirmed. The money's accessible. It isn't invisible, so we still need to get out.
Delivery: 
Trigger: Fund transfer

**GUESS** `M67_S1_02_GUESS`
Five hundred million dollars... we actually did it.
Delivery: 
Trigger: Truck cab relief

**ICE** `M67_S1_03_ICE`
Not yet. Aegis launched their final surviving battalion to intercept us at the airport.
Delivery: 
Trigger: Airport alert

### Aftermath

**GUESS** `M67_SCENE_OUTRO_01_GUESS`
Funds routed. Families first on the escape arrangements. The rest of us take the canal convoy to LSIA.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M67_SCENE_OUTRO_02_GOHAN`
If this connection dies now, the copies still exist. For the first time, nobody has to save the only copy of me.
Delivery: Reflective; allow the response to land
Trigger: outro

## M68 - BLOOD BROTHERS: THE DRAIN (future gameplay)

### Intro

**GUESS** `M68_SCENE_INTRO_01_GUESS`
The canal takes the loaded semi past their main roadblocks. We reach the airport, but the sea fallback stays ready.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M68_SCENE_INTRO_02_ICE`
If the cargo stops us, we leave it. I'm saying it before I can see what it's worth.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M68_S1_01_GUESS`
Eighteen wheels of steel rolling through the flood channel! Speed is eighty!
Delivery: 
Trigger: Convoy start

**ICE** `M68_S1_02_ICE`
Aegis armored buggies dropping in from bridge ramps! Turret engaged!
Delivery: 
Trigger: Turret combat

**GOHAN** `M68_S1_03_GOHAN`
Deploying magnetic trail mines! Bridge support blown-roadblock crushed!
Delivery: 
Trigger: Channel breakout

### Aftermath

**GOHAN** `M68_SCENE_OUTRO_01_GOHAN`
Airport perimeter next. We can abandon equipment, not people. Keep counting three when the noise gets bad.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M68_SCENE_OUTRO_02_GUESS`
Three. I have been counting three since the dockyard. You finally started saying it back.
Delivery: Reflective; allow the response to land
Trigger: outro

## M69 - BLOOD BROTHERS: RUNWAY 30L (future gameplay)

### Intro

**ICE** `M69_SCENE_INTRO_01_ICE`
The cargo plane is beyond the runway barricades. Clear the route without chasing anything that isn't between us and the ramp.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `M69_SCENE_INTRO_02_GUESS`
The ocean launch and sub are the fallback. If the plane can't fly, we still have somewhere to go.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `M69_S1_01_GUESS`
Runway 30L! Commercial 747 touching down right in front of us! SWERVING!
Delivery: 
Trigger: Jetliner dodge

**ICE** `M69_S1_02_ICE`
Aegis barricade at the taxiway! Guess, ram the fuel bowser into their line!
Delivery: 
Trigger: Barricade smash

**GOHAN** `M69_S1_03_GOHAN`
C-130 cargo ramp is open! Drive the rig straight up into the cargo hold!
Delivery: 
Trigger: Ramp boarding

### Aftermath

**GOHAN** `M69_SCENE_OUTRO_01_GOHAN`
Nose gear's hit. Takeoff is gone. We hold the fuselage long enough to reach the water escape.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M69_SCENE_OUTRO_02_ICE`
No last stand for a pile of gold. Guess, find us a path. Gohan, tell the boats we're coming.
Delivery: Reflective; allow the response to land
Trigger: outro

## M70 - BLOOD BROTHERS: GROUNDED TITAN (future gameplay)

### Intro

**GUESS** `M70_SCENE_INTRO_01_GUESS`
The engines still turn. We use the aircraft to reach the seawall, then abandon it for the boats and sub. No flying miracle.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `M70_SCENE_INTRO_02_ICE`
I hold the ramp until you call. Then I leave with you. Nobody has to drag me away from this one.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `M70_S1_01_ICE`
Fifteen years we walked our own roads, but tonight we hold this ground TOGETHER!
Delivery: Roaring over minigun fire, brass raining down tarmac
Trigger: Final siege holdout

**GOHAN** `M70_S1_02_GOHAN`
Third wave! Ice, we're still here! I'm not losing either of you after finding you again!
Delivery: Firing combat rifle, laughing wildly through smoke
Trigger: Wave three repelled

**GUESS** `M70_S1_03_GUESS`
All four turboprops redlining! We ain't dying on this runway! SEAWALL RAMP IN THREE SECONDS!
Delivery: Throttle quad-levers jammed forward
Trigger: Seawall taxi

**ICE** `M70_S1_04_ICE`
HOLD ONTO SOMETHING! HIT THE WATER!
Delivery: 
Trigger: Plane launches off seawall

**GOHAN** `M70_S1_05_GOHAN`
We survived it. All three of us.
Delivery: Floating in the ocean launch under golden sunrise
Trigger: Sunrise aftermath

**GUESS** `M70_S1_06_GUESS`
Told y'all... Guess never misses an exit.
Delivery: Smiling, looking back at the smoking city horizon
Trigger: Brothers laugh

**ICE** `M70_S1_07_ICE`
Let's go home.
Delivery: Looking at his brothers, loading fresh magazine, smiling
Trigger: Final cut to black: CAMPAIGN COMPLETE

### Aftermath

**GOHAN** `M70_SCENE_OUTRO_01_GOHAN`
The copies are out. The money is routed. For once there's nothing on that burning machine we have to go back for.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M70_SCENE_OUTRO_02_GUESS`
Darius. Devin. Answer with your names. I want to know who made it out, not which job you were doing.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M70_SCENE_OUTRO_03_ICE`
Darius. Still here. I should have called when I came home. I let shame decide you didn't want to hear from me.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M70_SCENE_OUTRO_04_GOHAN`
Devin. I thought if you saw what happened to me, you'd only see the failure. I should have let you decide.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M70_SCENE_OUTRO_05_GUESS`
Ron. I kept acting like leaving didn't hurt. Then every time somebody stayed, I made a joke instead of saying thanks.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `M70_SCENE_OUTRO_06_ICE`
We're not fixing fifteen years on one boat. But tomorrow, when you call, I'm answering.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `M70_SCENE_OUTRO_07_GOHAN`
Tomorrow sounds good. Tell me where we're going before Ron invents another shortcut.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `M70_SCENE_OUTRO_08_GUESS`
Breakfast. Three seats. After that, we figure it out together.
Delivery: Reflective; allow the response to land
Trigger: outro

## SM01 - LEAD & KEVLAR (scripted)

### Intro

**ICE** `SM01_SCENE_INTRO_01_ICE`
Sergei sold me bad ammunition. I'll recover the proper crates alone. The crew needs a supply line we can trust.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `SM01_SCENE_INTRO_02_GUESS`
You can work alone without pretending you don't have anyone to call. Tell us when you're leaving the warehouse.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `SM01_S1_01_ICE`
Sergei thought he could pocket twenty grand and hand me surplus civilian brass. Time to remind him who taught him logistics.
Delivery: Low, dangerous growl
Trigger: Gameplay stage 1 entry

**ICE** `SM01_S1_02_ICE`
Six guards between me and Sergei. I'm clearing the floor before I ask for the codes.
Delivery: Kicking side door open, racking heavy shotgun
Trigger: Gameplay stage 2 entry

**SERGEI** `SM01_S2_03_ENEMY`
Vance! Put the gun down! We can work something out, brother!
Delivery: 
Trigger: Gameplay stage 3 entry

**ICE** `SM01_S2_04_ICE`
We aren't brothers, Sergei. And your credit just expired.
Delivery: 
Trigger: Gameplay stage 3 entry

**ICE** `SM01_S2_05_ICE`
Crates loaded in the trunk. Armor-piercing tungsten-core 7.62. Gohan and Guess have what they need.
Delivery: 
Trigger: Gameplay stage 4 completion

### Aftermath

**ICE** `SM01_SCENE_OUTRO_01_ICE`
Crates secured. I'm clear. I almost switched the radio off out of habit. Thought you ought to know.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `SM01_SCENE_OUTRO_02_GOHAN`
I heard you. That's enough for tonight. We'll check the ammunition together when you get back.
Delivery: Reflective; allow the response to land
Trigger: outro

## SM02 - ZERO-DAY INJECTION (scripted)

### Intro

**GOHAN** `SM02_SCENE_INTRO_01_GOHAN`
The annex gives me municipal optical access for the drone work. One person can reach it quietly. That's why I'm going alone.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `SM02_SCENE_INTRO_02_ICE`
Send the check-in time. You aren't asking permission. You're giving us a chance to notice if you need help.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `SM02_S1_01_GOHAN`
Lifeinvader annex. Reach the roof marker, then stun both guards. Nobody needs to die for a camera archive.
Delivery: Whispering through comm headset
Trigger: Gameplay stage 1 entry

**GOHAN** `SM02_S1_02_GOHAN`
Two marked guards. Stun gun first; the thermal pulse can help me track them.
Delivery: 
Trigger: Gameplay stage 2 entry

**GOHAN** `SM02_S2_03_GOHAN`
Worm installed. Camera archive access is ours. Getting out before IT traces the connection.
Delivery: Keystrokes clicking rapidly
Trigger: Gameplay stage 3 completion

**GOHAN** `SM02_S2_04_GOHAN`
Slip down the fire escape before internal IT notices the packet spike. Clean and invisible.
Delivery: 
Trigger: Gameplay stage 4 entry

### Aftermath

**GOHAN** `SM02_SCENE_OUTRO_01_GOHAN`
The tap is live. The camera archive is open; that doesn't wipe the dock recording or a human witness.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `SM02_SCENE_OUTRO_02_GUESS`
And you're back on the radio. I like that part of the upgrade best.
Delivery: Reflective; allow the response to land
Trigger: outro

## SM03 - MIDNIGHT DRIFT (scripted)

### Intro

**KJ** `SM03_SCENE_INTRO_01_KJ`
Guess, I checked the prize. Transmission and clutch are real. Marabunta's promise to race clean? I wouldn't put money on that.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `SM03_SCENE_INTRO_02_GUESS`
You got me a place on the grid, KJ. That's enough. Stay clear when it starts; I need my friend around after the finish.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `SM03_S1_01_GUESS`
Marabunta thinks their tuned Elegy has the top end. They don't know who is behind this wheel.
Delivery: Twin-turbo revving at 7,000 RPM, two-step popping
Trigger: Gameplay stage 1 entry

**GUESS** `SM03_S1_02_GUESS`
Three laps through the yellow checkpoints. KJ, keep an eye on their line.
Delivery: Slipstream Reflex activated, slow-mo engine roar
Trigger: Gameplay stage 2 entry

**RIVAL RACER** `SM03_S2_03_ENEMY`
We lost the race! Shoot his tires before he leaves!
Delivery: 
Trigger: Gameplay stage 3 entry

**GUESS** `SM03_S2_04_GUESS`
They pulled guns after losing. I can stop them or get the coupe back to the finish.
Delivery: Laughing wildly, ducking below dashboard
Trigger: Gameplay stage 3 entry

**GUESS** `SM03_S2_05_GUESS`
Checkered flag! Title is in my pocket and the racing transmission is loaded on the truck.
Delivery: 
Trigger: Gameplay stage 3 completion

### Aftermath

**KJ** `SM03_SCENE_OUTRO_01_KJ`
I saw the guns. Prize is secured, but don't come back here tonight. Winning doesn't make those people good losers.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `SM03_SCENE_OUTRO_02_GUESS`
Thanks, KJ. For checking the deal, and for telling me when it's bad. I'll call when the parts get back to the crew.
Delivery: Reflective; allow the response to land
Trigger: outro

## SM04 - DEAD DROP QUARRY (scripted)

### Intro

**ICE** `SM04_SCENE_INTRO_01_ICE`
The quarry snipers are watching our desert routes. I'll work the ridge alone so they don't see a convoy coming.
Delivery: Briefing; intent before tactics
Trigger: intro

**GOHAN** `SM04_SCENE_INTRO_02_GOHAN`
I can monitor your check-ins. Being quiet around them doesn't mean being silent with us.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `SM04_S1_01_ICE`
Two Aegis marksmen watching the quarry road. I'll take the overlook and identify both positions.
Delivery: Wind howling through scope, breathing slow
Trigger: Mission begins

**ICE** `SM04_S1_02_ICE`
One down. The other still has a sightline on the road. Can't leave him there.
Delivery: Suppressed crack
Trigger: First marksman eliminated

**ICE** `SM04_S2_03_ICE`
Both down. Radios recovered. Gohan can listen for patrol calls; we'll still have to watch the road.
Delivery: 
Trigger: Both marksmen eliminated and both radios collected

### Aftermath

**ICE** `SM04_SCENE_OUTRO_01_ICE`
Both nests cleared. Their radios give us a warning channel. I sent the frequencies to both of you.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `SM04_SCENE_OUTRO_02_GUESS`
Good. No single point of failure, including the man carrying the rifle.
Delivery: Reflective; allow the response to land
Trigger: outro

## SM05 - BLACK BOX ESTUARY (scripted)

### Intro

**GOHAN** `SM05_SCENE_INTRO_01_GOHAN`
The swamp buoy can feed us military flight telemetry. I'll make the splice alone while its patrol passes.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `SM05_SCENE_INTRO_02_GUESS`
I'll keep a return window open. You don't have to use it to prove it was worth setting up.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `SM05_S1_01_GOHAN`
Swamp's dark enough to hide a boat. Going in quiet. I'll call when I find the buoy.
Delivery: Reeds rustling, water rippling
Trigger: Gohan reaches the boat

**GOHAN** `SM05_S1_02_GOHAN`
Buoy located. Fitting the interceptor at the service harness. Keep this channel open.
Delivery: Diving underwater, rebreather hiss
Trigger: Gohan begins installing the interceptor

**GOHAN** `SM05_S2_03_GOHAN`
Signal locked. We can read the flight traffic this buoy relays. Coming back to the pickup.
Delivery: 
Trigger: Interceptor installed

### Aftermath

**GOHAN** `SM05_SCENE_OUTRO_01_GOHAN`
Telemetry received. It supplements our recon; it doesn't tell us every pilot's next thought. I'm heading back.
Delivery: Reflective; allow the response to land
Trigger: outro

**ICE** `SM05_SCENE_OUTRO_02_ICE`
Copy. Work's done for tonight. You can stop being useful long enough to eat with us.
Delivery: Reflective; allow the response to land
Trigger: outro

## SM06 - CANYON RUNNER (scripted)

### Intro

**GUESS** `SM06_SCENE_INTRO_01_GUESS`
McKenzie's aircraft need fuel. I'll take the tanker through Raton. Heavy truck, narrow route; nobody calls this a race.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `SM06_SCENE_INTRO_02_ICE`
I won't. Call the location if the route closes. We can recover you even if we can't recover the fuel.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `SM06_S1_01_GUESS`
Aviation fuel, full load. Easy on the throttle. This thing only has to arrive in one piece.
Delivery: V8 diesel roaring, chassis shuddering
Trigger: Guess boards the tanker

**GUESS** `SM06_S1_02_GUESS`
Cartel bikes behind me! Gohan, keep the airfield gate clear.
Delivery: 
Trigger: Armed bike pursuit starts

**GUESS** `SM06_S2_03_GUESS`
Through the canyon. Still got the load. I'm taking the next bend slow, whether they like it or not.
Delivery: Flooring pedal, swerving into lead bike
Trigger: Tanker clears the canyon checkpoint

**GUESS** `SM06_S2_04_GUESS`
Tanker at McKenzie, intact. Set this fuel aside for the rig operation.
Delivery: 
Trigger: Specified tanker delivered and unloaded

### Aftermath

**GUESS** `SM06_SCENE_OUTRO_01_GUESS`
Tanker delivered. I had to choose between a clean run and backing off a blind corner. Backing off worked.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `SM06_SCENE_OUTRO_02_GOHAN`
Remember that when you tell the story. Let the younger version of you hear that slowing down can be skill too.
Delivery: Reflective; allow the response to land
Trigger: outro

## SM07 - BLOOD DEBT (future gameplay)

### Intro

**ICE** `SM07_SCENE_INTRO_01_ICE`
Sterling sold out my unit. He's at Aegis now. I'm going after him for myself, and I won't hide that behind the campaign.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `SM07_SCENE_INTRO_02_GUESS`
Then come back for yourself too. We can't make this decision for you, but we're here after it.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**ICE** `SM07_S1_01_ICE`
Fifteen years ago Sterling got my whole squad ambushed in Kandahar for a five-hundred-thousand-dollar kickback. Tonight I settle the ledger.
Delivery: Cold, venomous tone
Trigger: Elevator ascent

**ICE** `SM07_S1_02_ICE`
Four executive bodyguards in the foyer-neutralized.
Delivery: Breaching suite doors with suppressed assault rifle
Trigger: Suite shootout

**STERLING** `SM07_S2_03_ENEMY`
Vance! You're a ghost! That was fifteen years ago in the desert!
Delivery: Cowering behind marble desk
Trigger: Sterling cornered

**ICE** `SM07_S2_04_ICE`
I didn't forget a single name on those dog tags, Sterling. Look at me.
Delivery: 
Trigger: Execution moment

**ICE** `SM07_S2_05_ICE`
Debt settled. The past is buried. Now we finish Aegis in Los Santos.
Delivery: Walking out into rain
Trigger: Hotel exit

### Aftermath

**ICE** `SM07_SCENE_OUTRO_01_ICE`
He's dead. The men I lost are still dead. I said the debt was settled because I wanted it to feel finished.
Delivery: Reflective; allow the response to land
Trigger: outro

**GOHAN** `SM07_SCENE_OUTRO_02_GOHAN`
You don't have to arrive healed. Come sit down. We can hear their names if you want to say them.
Delivery: Reflective; allow the response to land
Trigger: outro

## SM08 - BURNER PROTOCOL (future gameplay)

### Intro

**GOHAN** `SM08_SCENE_INTRO_01_GOHAN`
Vanderbilt & Cole forged the fraud case against me. I'm taking their files before I destroy the archive they used to control me.
Delivery: Briefing; intent before tactics
Trigger: intro

**ICE** `SM08_SCENE_INTRO_02_ICE`
Keep a copy outside the building. Your name is more than what an employer wrote beside it.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GOHAN** `SM08_S1_01_GOHAN`
Vanderbilt & Cole. The suits who doctored the ledgers and handed my name to the feds fifteen years ago. Payback time.
Delivery: Looking up at glass tower
Trigger: Office infiltration

**GOHAN** `SM08_S1_02_GOHAN`
Bypassing their biometric vault lock. Downloading sixty gigabytes of blackmail files.
Delivery: 
Trigger: Vault download

**GOHAN** `SM08_S2_03_GOHAN`
Chemical thermite strips armed along every filing cabinet. In sixty seconds, their entire corrupt history is ash.
Delivery: 
Trigger: Igniting vault

**GOHAN** `SM08_S2_04_GOHAN`
The evidence proving they framed me is out. Clearing my name comes next. First, Ice and Guess need me.
Delivery: Watching flames reflect in glass window
Trigger: Elevator escape

### Aftermath

**GOHAN** `SM08_SCENE_OUTRO_01_GOHAN`
Their archive burned. The files prove what they did; they don't make fifteen years disappear. I overstated it when I said I was clear.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `SM08_SCENE_OUTRO_02_GUESS`
You don't owe us a clean record to belong here. Bring what happened to you, not a version you think we'll accept.
Delivery: Reflective; allow the response to land
Trigger: outro

## SM09 - THE LONG EXIT (future gameplay)

### Intro

**KJ** `SM09_SCENE_INTRO_01_KJ`
Guess, that captain will trade documents and passage for the impounded hypercar. I checked his ship, not his soul. Confirm the passenger count yourself.
Delivery: Briefing; intent before tactics
Trigger: intro

**GUESS** `SM09_SCENE_INTRO_02_GUESS`
I will. You make the introduction, then step back, KJ. This is my run. You don't owe my enemies a look at your face.
Delivery: Briefing; intent before tactics
Trigger: intro

### Gameplay

**GUESS** `SM09_S1_01_GUESS`
The foreign captain wants this hypercar on his cargo deck before dawn, or the escape passports don't get stamped. Guess never fails a delivery.
Delivery: 
Trigger: Hotwiring hypercar

**GUESS** `SM09_S1_02_GUESS`
LSPD SWAT cruisers setting up spiked roadblocks on the bridge! Slipstream Reflex engaged!
Delivery: Sirens wailing, five-star police pursuit
Trigger: Highway drift

**GUESS** `SM09_S2_03_GUESS`
Flying over the blockade! Eat my exhaust!
Delivery: Launching car off construction ramp over police cruiser
Trigger: Ramp jump

**GUESS** `SM09_S2_04_GUESS`
Car driven straight into the shipping container! Container door locked! Passports and sea passage secured for the whole crew!
Delivery: 
Trigger: Mission passed

### Aftermath

**KJ** `SM09_SCENE_OUTRO_01_KJ`
He confirmed the documents and passage. Keep your other exit too. One captain's schedule isn't something I'd stake a family on.
Delivery: Reflective; allow the response to land
Trigger: outro

**GUESS** `SM09_SCENE_OUTRO_02_GUESS`
I kept it. When this is over, I'm buying you breakfast, KJ. Somewhere without a starting grid or a security gate.
Delivery: Reflective; allow the response to land
Trigger: outro


# Playable mission flow and retry map

36 scripted missions. Generated from current production stage declarations. These are code-checked flows, not live playthrough results.

The quoted prompts below are the authored base instructions. Runtime text adds the required character, button, remaining work time, enemy count and passive rules. Yellow marks travel/work; red marks hostiles. E / D-pad Right starts timed work. Leaving its radius resets that work. Vehicle delivery requires the assigned hero aboard the actual vehicle. Aircraft landing also requires low height and speed.

## Failure and retry contract

Start unlocked job → skippable briefing → fresh actors/vehicles → objectives → final dialogue → commit completion and one-time reward → skippable aftermath.

A required hero going down, a required asset disappearing, or a failed objective ends the attempt. Active objective cleanup runs even if another cleanup throws. Markers, cameras and mission AI ownership are released. Player death uses the existing movement-verified recovery; it does not regroup surviving teammates. Retry creates a NEW mission at stage zero with new vehicles, actors, timers and work progress. It never restores a position-only checkpoint. The mission key retries the failed job; the debug mission page also offers Retry last attempt and retains the reason. Explicitly selecting another unlocked job changes the selection.

Abort or failure pays nothing. Committed first completion pays once; replay cannot duplicate cash, gold, or unlocks. The debug Force pass/Mark complete tools deliberately bypass gameplay and grant completion rewards.

Mandatory role handoffs block movement, attacks and interactions until switching, while the camera and character wheel remain usable. World simulation and mission timers continue. Parallel stages permit any character with unfinished work. The gate uses frame inputs, never a persistent player freeze.

## Reading the checks

| Objective | Completion / failure rule |
|---|---|
| ReachZone | Required hero inside the configured marker radius; a vehicle only when explicitly required. |
| MissionInteraction | Required hero close enough, on foot or aboard the required vehicle; press E / D-pad Right and stay. Unloading also requires stopping. Missing work vehicle fails. |
| AssignedWork | Named NPC travels to the actual work site and accumulates work time there while you control the other role. Worker death fails. |
| Kill / Subdue / Waves | All required targets resolved; wave spawns must succeed. Subdue requires stun/cuffs and fails if a target is killed. Missing unresolved actors fail. |
| Enter / Deliver | Actual vehicle and required hero; extraction can additionally require all three aboard. Destroyed vehicles fail. Delivery checks the destination, not a replacement vehicle. |
| TrailerDelivery | The specific tanker is coupled to the tractor, both stopped, and the tanker is within 35m of unloading. |
| Shadow | Approach within the acquisition window, then hold the specified distance band. After acquisition, eight seconds outside the band fails. |
| MultiHold | Visit every marked site, press the interaction button and finish each timed operation. Nearest unfinished site receives the route. |
| Passive rules | Protect, detection, speed and altitude constrain the active stage. They never count as the action needed to finish it. |

## M01 — GHOST IN THE DOCKYARD

Prerequisite: none. Retry: full mission restart.

Guess begins in his approach car; Ice and Gohan have separate exterior approaches. Complete each opening role: Guess reaches the prototype, Gohan copies the ledger at the terminal, Ice identifies Mateo from overwatch. Shooting before recognition blows cover. The recognition call keeps everyone at their actual position. Mateo runs to a launch while the crew defeats the guards. Extract in the actual four-seat prototype and bring all three clear of the exit. Wrecking the required vehicle, losing a brother, missing escape assets or a blocked Mateo escape fails clearly.

Survey references: M01.CapoSpawn, M01.CraneNest, M01.ExitPoint, M01.GohanApproach, M01.GuessApproach, M01.IceApproach, M01.LaunchEscape, M01.PrototypeCar, M01.RegroupPoint, M01.ServiceTerminal.

## M02 — LOOSE STRANDS

Prerequisite: M01. Retry: full mission restart.

Guess drives the occupied Granger to the moving van. Gohan hacks automatically while alive aboard the chase car and within the proximity band; you can drive as Guess or switch to Gohan. Leaving range pauses progress. Van occupants begin shooting halfway through. On completion the van stops: Ice exits and spends three seconds at the rear doors to collect the drives. Killing a driver or stealing the van is not the pickup trigger. Rejoin the Granger with all three and drive to the canal escape marker. Losing the van before the hack, the Granger, or a required brother fails.

Survey references: M02.CanalEscape, M02.InterceptStart.

## M03 — CYPRESS FOUNDRY

Prerequisite: M02. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Drive to the junction** — TravelObjective: Guess: drive to the Davis rail junction. |
| 2 | Guess | **Seal the response routes** — HoldZoneObjective: Guess: get out and hold the junction marker.<br>ConditionObjective: Guess: put the street crew down. |
| 3 | Ice | **Breach the depot** — ReachZoneObjective: Ice: walk to the yellow entry marker at the depot gate.<br>QuietRuleObjective: Quiet until the entry marker. |
| 4 | Ice | **Clear the yard** — KillTargetsObjective: Ice: eliminate the guards marked RED in the container yard. Gohan waits until it is clear. |
| 5 | Gohan | **Load the Benson** — MissionInteraction: Gohan: open the Benson and load the crates |
| 6 | Guess | **Run it home** — EnterVehicleObjective: Guess: travel to the depot and take the orange-marked Benson truck (driver seat). |
| 7 | Guess | **Cypress Flats** — LoseWantedObjective: Lose the police before the foundry. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: Base.CypressFlats, M03.CraneControls, M03.DepotGate, M03.HaulerSpawn, M03.RailJunction.

## M04 — SEVERED WIRE

Prerequisite: M03. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Drive to the lot** — TravelObjective: Guess: drive the crew to the Pillbox Hill lot. |
| 2 | Guess | **In position** — WaitForRolesObjective: Hold at the exit. Ice and Gohan are in position. |
| 3 | Gohan | **Kill the lights** — MissionInteraction: Gohan: cut the marked surface-lot breaker |
| 4 | Gohan | **Miller runs** — SwitchWindowObjective: Miller is running. Take Guess to intercept; Ron is already on him |
| 5 | Guess | **Run him down** — PursueTargetObjective: Guess: chase the red marker. Disable Miller's car or stop Miller, then collect his drive. |
| 6 | Guess | **Recover the drive** — MissionInteraction: Guess: collect Miller's drive |
| 7 | Guess | **Lose them and regroup** — LoseWantedObjective: Lose the police.<br>EnterVehicleObjective: Pick up Ice and Gohan in the van. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: Base.CypressFlats, M04.Breaker, M04.ChaseCar, M04.GarageEntry, M04.RampGuards.

## M05 — TIDAL LOCK

Prerequisite: M04. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Cliff overwatch** — KillTargetsObjective: Ice — take the generator crew off the cave mouth. |
| 2 | Guess | **Light the cove** — MissionInteraction: Guess: launch the signal flare from the dinghy |
| 3 | Gohan | **Breach the grotto** — Follow the current objective; detailed rule is defined by this stage’s objective type. |
| 4 | Gohan | **Run him to the sandbar** — Follow the current objective; detailed rule is defined by this stage’s objective type. |
| 5 | Gohan | **Take him aboard** — MissionInteraction: Gohan: bring the dinghy alongside and take Mateo aboard |
| 6 | Gohan | **Mateo's account** — DialogueFinishedObjective: Hold the dinghy. Mateo is aboard. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M05.CliffPerch, M05.CoveAir, M05.DinghySpawn, M05.GrottoMouth, M05.Sandbar.

## M06 — CLEAN SWEEP

Prerequisite: M05. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Cut the power** — MissionInteraction: Gohan: cut the marked power feeder |
| 2 | Ice | **Sally port** — ReachZoneObjective: Ice: walk into the yellow depot entrance marker. |
| 3 | Ice | **Burn the racks** — AssignedWorkObjective: Gohan is preparing the thermite. Ice: hold the alley while he works.<br>SurviveWavesObjective: Ice: defeat the RED-marked SWAT waves while Gohan finishes the burn. Stay on Ice. |
| 4 | Guess | **The pickup** — ReachZoneObjective: Guess: bring the Granger to the alley mouth for Ice and Gohan. |
| 5 | Guess | **Everyone aboard** — EnterVehicleObjective: Guess: hold at the alley mouth until Ice and Gohan are in the Granger. |
| 6 | Guess | **Out of Vespucci** — LoseWantedObjective: Lose the police. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M06.AlleyHold, M06.Culvert, M06.Feeder, M06.GrangerSpawn, M06.SallyPort, M06.ServerRacks.

## M07 — WIRETAP WALTZ

Prerequisite: M06. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **The mast** — ReachZoneObjective: Ice — get up to the antenna platform. |
| 2 | Ice | **Clamp the receiver** — MissionInteraction: Clamp the packet sniffer to the dish. |
| 3 | Ice | **Off the roof** — ReachZoneObjective: Descend from the roof, then reach Guess's marked pickup. Use the parachute only if there is clearance. |
| 4 | Ice | **Moving pickup** — EnterVehicleObjective: Get in behind Guess. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M07.GarageRoof, M07.LandingZone, M07.MastTop.

## M08 — SUPPLY & SEVER

Prerequisite: M07. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Loop the cameras** — MissionInteraction: Gohan — loop the CCTV feed. |
| 2 | Ice | **Drop the sentries** — KillTargetsObjective: Ice — drop the sentries at the marked warehouse posts. |
| 3 | Guess | **Take the forklift** — EnterVehicleObjective: Guess — take the forklift. |
| 4 | Guess | **Crate one** — MissionInteraction: Guess — bring the forks under the first turbine crate. |
| 5 | Guess | **Crate two, the technical** — DestroyVehicleObjective: Ice — put the Aegis technical down.<br>MissionInteraction: Guess — bring the forks under the second crate. |
| 6 | Guess | **Take the hauler** — EnterVehicleObjective: Guess — take the flatbed. Ice rides with you; Gohan brings the Granger. |
| 7 | Guess | **Lose the police** — LoseWantedObjective: Lose the police before the stash. |
| 8 | Guess | **The stash** — DeliverVehicleObjective: Guess: bring the loaded flatbed to the connector stash. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M08.CameraRoom, M08.Connector, M08.CratePadOne, M08.CratePadTwo, M08.HaulerSpawn, M08.WarehouseGate.

## M09 — ROLLING THUNDER

Prerequisite: M08. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Get airborne** — EnterVehicleObjective: Guess — take the Frogger up. |
| 2 | Guess | **Shadow the convoy** — ShadowTargetObjective: Hold the ridgeline behind the convoy. |
| 3 | Ice | **Take the driver** — KillTargetsObjective: Ice: wait at the ambush point and shoot the marked escort driver. |
| 4 | Ice | **Rip the transponder** — MissionInteraction: Ice: get out, approach the stopped escort cab, and take its IFF transponder. |
| 5 | Guess | **The pickup** — DeliverVehicleObjective: Guess: land the Frogger on the marked flat past the culvert. |
| 6 | Ice | **Ice aboard** — EnterVehicleObjective: Ice — board the Frogger. |
| 7 | Ice | **Transponder extraction** — ReachZoneObjective: Get the transponder to the temporary drop point. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M09.AmbushPoint, M09.Bunker, M09.ConvoyStart, M09.HeliSpawn, M09.Pickup.

## M10 — OPEN THROTTLE

Prerequisite: M09. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Check the load** — MissionInteraction: Guess: check the crates on the flatbed |
| 2 | Guess | **Roll out** — EnterVehicleObjective: Guess — take the flatbed. Ice rides beside you. |
| 3 | Guess | **The run** — SpeedFloorObjective: Keep the flatbed above 35 mph; use drive-by weapons on the bikes.<br>KillTargetsObjective: Clear the cartel bikes. |
| 4 | Guess | **The firing window** — DeliverVehicleObjective: Guess: get the flatbed to the tunnel mouth. That is Ice's firing window. |
| 5 | Ice | **Gunship** — DestroyVehicleObjective: Ice: out of the cab, launcher on the marked Buzzard from the tunnel mouth. |
| 6 | Guess | **Burro Heights** — DeliverVehicleObjective: Guess: get back in the flatbed and deliver the engines to the shop.<br>LoseWantedObjective: Lose the police before the shop. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M08.Connector, M10.TunnelMouth, M11.ChopShop.

## M11 — IRONCLAD DYNO

Prerequisite: M10. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Mount the turbine** — MissionInteraction: Guess — fabricate the motor mounts. |
| 2 | Ice | **On the dyno** — EnterVehicleObjective: Ice — get in and hold it on the dyno. |
| 3 | Ice | **Manifold pressure** — DynoObjective: Ice: use partial RT or tap W to hold 22-28 PSI. Stay in the driver seat. |
| 4 | Ice | **Berth 44** — DialogueFinishedObjective: Gohan has the manifest. Hear him out. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M11.ChopShop, M11.DynoPad.

## M12 — BLACK TIDE RECON

Prerequisite: M11. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Launch the ROV** — EnterVehicleObjective: Gohan — take the ROV out from the south jetty. |
| 2 | Gohan | **Under the sonar** — DeliverVehicleObjective: Gohan: descend in the sub to the underwater yellow marker. |
| 3 | Gohan | **Map the hull** — MissionInteraction: Acoustic-scan hold 3's bulkhead. |
| 4 | Gohan | **Back to the jetty** — DeliverVehicleObjective: Gohan: surface in the sub beside the jetty. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M12.FreighterHull, M12.PierWatch, M12.SonarBuoy, M12.SouthJetty.

## M13 — SMUGGLER'S CUT

Prerequisite: M12. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Into the basin** — EnterVehicleObjective: Ice — take the water scooter into the basin. |
| 2 | Ice | **Limpets** — MultiHoldObjective: Plant limpet charges on all three barges. |
| 3 | Ice | **Clear the water** — ReachZoneObjective: Get to the western slipway. |
| 4 | Ice | **Blow the basin** — MissionInteraction: Ice: board the Granger, then trigger the planted charges |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M13.BargeOne, M13.BargeThree, M13.BargeTwo, M13.CanalSlipway, M13.KayakLaunch.

## M14 — AIRSPACE BLACKOUT

Prerequisite: M13. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Overwatch** — KillTargetsObjective: Ice — clear the apron from the ridge. |
| 2 | Guess | **The hangar** — ReachZoneObjective: Guess — get to the hangar door. |
| 3 | Guess | **Hotwire** — EnterVehicleObjective: Guess: take the marked jammer aircraft. |
| 4 | Guess | **Under the radar** — AltitudeCeilingObjective: Hug the terrain — stay under 50 meters above the terrain.<br>DeliverVehicleObjective: Guess: land the jammer aircraft at McKenzie and stop. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M14.HangarDoor, M14.McKenzieHangar, M14.OverwatchRidge, M14.PlaneSpawn.

## M15 — CRAWLSPACE

Prerequisite: M14. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Maintenance level** — ReachZoneObjective: Gohan: reach the marked maintenance access outside the administration building. |
| 2 | Ice | **Clear the rounds** — SubdueTargetsObjective: Ice — put the watchmen down without killing them. |
| 3 | Gohan | **Splice the trunk** — MissionInteraction: Gohan — splice the optical bypass. |
| 4 | Gohan | **Out clean** — ReachZoneObjective: Leave the way you came in. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M15.AdminEntry, M15.Exit, M15.FiberSplice, M15.MaintenanceVault.

## M16 — THE HEAVY LIFT

Prerequisite: M15. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Walk in** — ReachZoneObjective: Ice — cross the outer depot on the transponder. |
| 2 | Ice | **Take the helipad** — KillTargetsObjective: Clear the military police off the pad. |
| 3 | Guess | **Spool the twins** — EnterVehicleObjective: Guess — take the Cargobob. |
| 4 | Guess | **Raton Canyon** — AltitudeCeilingObjective: Hug the canyon — stay under 60 meters above terrain.<br>DeliverVehicleObjective: Guess: fly the Cargobob through the marked canyon route. |
| 5 | Guess | **Terminal Island** — DeliverVehicleObjective: Put the Cargobob down at Terminal Island. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M16.CanyonRun, M16.CargobobSpawn, M16.DepotFence, M16.Helipad, M16.TerminalDrop.

## M17 — SUB-ZERO PAYLOAD

Prerequisite: M16. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Calibrate the torches** — MultiHoldObjective: Weld the plasma-arc torches to the hull. |
| 2 | Guess | **Grapple test** — MissionInteraction: Guess — test the fifty-ton magnetic lock. |
| 3 | Guess | **Ready** — DialogueFinishedObjective: Finish the radio check before staging the heist. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M17.DrySlip, M17.WeldOne, M17.WeldThree, M17.WeldTwo.

## M18 — THE STAGING LINE

Prerequisite: M17. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Sub into the channel** — EnterVehicleObjective: Gohan — take the Kraken out.<br>DeliverVehicleObjective: Hold her in the channel. |
| 2 | Guess | **Bird in the hangar** — DeliverVehicleObjective: Guess — put the Cargobob in the salt hangar. |
| 3 | Ice | **Load the launchers** — DeliverVehicleObjective: Ice — bring the hauler onto the line. |
| 4 | Ice | **Load the parked hauler** — MissionInteraction: Load the anti-air launchers. |
| 5 | Ice | **Countdown** — DialogueFinishedObjective: Keep your assigned vehicle in place. Listen to the final radio check. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M18.ChannelMark, M18.HaulerMark, M18.SaltHangar.

## M19 — THE PORT HEIST: UNDERWATER BREACH

Prerequisite: M18. Story gate: SM01, SM02, SM03 must be complete first (QA may bypass). Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Dive** — EnterVehicleObjective: Take the Kraken down. |
| 2 | Gohan | **Cut the bulkhead** — MissionInteraction: Burn the breach into hold 3. |
| 3 | Gohan | **Clamp the floats** — MultiHoldObjective: Clamp the ballast floats to the container. |
| 4 | Gohan | **Surface** — DeliverVehicleObjective: Gohan: surface in the Kraken at the yellow marker. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M19.DiveStart, M19.HullBreach, M19.Surface.

## M20 — THE PORT HEIST: SKY HOOK

Prerequisite: M19. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Get on the water** — EnterVehicleObjective: Guess — take the Cargobob over the basin. |
| 2 | Ice | **Suppress the deck** — KillTargetsObjective: Ice — clear the marked quayside gunners from the pier. |
| 3 | Guess | **Lock the cable** — MissionInteraction: Guess — hold the hover over the container. |
| 4 | Guess | **Climb out** — DeliverVehicleObjective: Guess: climb in the Cargobob to the elevated yellow marker. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M12.PierWatch, M18.SaltHangar, M19.Surface, M20.ClimbOut, M20.DeckGunners, M20.HoverPoint.

## M21 — THE PORT HEIST: OPEN WATER

Prerequisite: M20. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Get on the water** — EnterVehicleObjective: Gohan — take the armed launch. |
| 2 | Gohan | **Draw the locks** — ShadowTargetObjective: Stay on the Cargobob's wing. |
| 3 | Gohan | **Kill the speedboats** — KillTargetsObjective: Clear the Aegis boats before they close. |
| 4 | Gohan | **Over the ridge** — DeliverVehicleObjective: Gohan: take the launch through the yellow breakwater exit. Guess will continue inland by air. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M12.PierWatch, M21.Breakwater, M21.LaunchSpawn, M21.RidgeCross.

## M22 — THE PORT HEIST: SCORCHED BAY

Prerequisite: M21. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Bring it in** — EnterVehicleObjective: Guess — fly the bullion into the Alamo. |
| 2 | Guess | **Drop the container** — MissionInteraction: Guess: hover 20m over the water marker and release the container |
| 3 | Guess | **The beach** — DeliverVehicleObjective: Guess: land the Cargobob on the marked shore and stop. |
| 4 | Guess | **Blaine County** — DialogueFinishedObjective: Listen to the emergency call. The foundry has been hit. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: Base.CypressFlats, M22.AlamoDrop, M22.Beach.

## M23 — GHOST IN THE SAGE

Prerequisite: M22. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Breach the dome** — ReachZoneObjective: Ice — get up to the radar dome walkway. |
| 2 | Ice | **Clear the radar yard** — KillTargetsObjective: Clear the cartel squatters out. |
| 3 | Guess | **Secure the bays** — MultiHoldObjective: Guess — check the three storage bays. |
| 4 | Gohan | **Power up** — MissionInteraction: Gohan — bring the marked generator online. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M23.BayOne, M23.BayThree, M23.BayTwo, M23.BunkerDoor, M23.DomeApproach, M23.Generator.

## M24 — LIQUID GOLD

Prerequisite: M23. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Into the shallows** — DeliverVehicleObjective: Guess — park the recovery truck at the dry shoreline marker. |
| 2 | Ice | **Dredge the crates** — AssignedWorkObjective: Guess works the recovery cable. Ice: cover him from the ridge.<br>SurviveWavesObjective: Ice — keep the deputies off the haul. |
| 3 | Guess | **Back to the bunker** — DeliverVehicleObjective: Get the haul to the radar base. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M23.BunkerDoor, M24.CraneSpawn, M24.RidgeLine.

## M25 — BOUNTY HUNTERS' CANYON

Prerequisite: M24. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **High ground** — ReachZoneObjective: Take the bridge deck. |
| 2 | Ice | **Seal the pass** — DestroyVehicleObjective: Detonate the fuel tanker across the southern pass. |
| 3 | Ice | **Hold the bridge** — SurviveWavesObjective: Ice: defeat the three red-marked assault waves. Use cover and your rifle. |
| 4 | Ice | **Off the bridge** — ReachZoneObjective: Ice: parachute down toward the marked extraction boat. |
| 5 | Ice | **River extraction** — EnterVehicleObjective: Get in the boat. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M25.BridgeDeck, M25.Riverbed, M25.TankerSpot.

## M26 — THE ALAMO SCRAMBLE

Prerequisite: M25. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Scramble** — EnterVehicleObjective: Guess — take off in the marked Lazer. |
| 2 | Guess | **First spotter** — DestroyVehicleObjective: Splash the lead spotter. |
| 3 | Guess | **Second spotter** — DestroyVehicleObjective: The second one is diving for Grapeseed — kill him. |
| 4 | Guess | **Home** — DeliverVehicleObjective: Guess: land the Lazer at McKenzie and stop. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M26.DusterPad, M26.PatrolBox.

## M27 — FLIGHT RISK

Prerequisite: M26. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Get on his rudder** — EnterVehicleObjective: Guess — take the stunt plane up. |
| 2 | Guess | **Match the Shamal** — ShadowTargetObjective: Climb to the Shamal and hold station inside 60 meters. |
| 3 | Ice | **Zero-G** — MissionInteraction: Ice: take the flight ledger from the cabin locker |
| 4 | Ice | **Terminal dive** — BailOutObjective: The pilot put her over — get out. |
| 5 | Ice | **Sea pickup** — EnterVehicleObjective: Ice: parachute to the green boat marker, then climb aboard Gohan's dinghy. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M26.DusterPad, M27.FormUp, M27.JetTrack, M27.SeaPickup.

## M28 — OFF THE GRID

Prerequisite: M27. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Identify the relay** — ReachZoneObjective: Gohan: reach the yellow relay-yard entrance. Ice covers the opposite approach. |
| 2 | Ice | **Clear the transformer yard** — KillTargetsObjective: Ice: clear the four red-marked relay guards before Gohan enters. |
| 3 | Gohan | **Read the cabinet** — TechnicalChoiceObjective: Gohan: choose which relay system to cut first |
| 4 | Gohan | **Connect the surge unit** — MissionInteraction: Gohan: connect the surge unit at the relay service cabinet |
| 5 | Ice | **Cover the splice** — AssignedWorkObjective: Gohan continues the splice. Ice: defeat the responding squads.<br>SurviveWavesObjective: Ice: clear both response squads marked red. |
| 6 | Guess | **Extraction** — EnterVehicleObjective: Guess: take the Granger driver seat. Wait for both brothers to board. |
| 7 | Guess | **Back to shelter** — DeliverVehicleObjective: Guess: bring the crew's Granger back to the radar bunker. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M23.DomeApproach, M28.Approach, M28.Cover, M28.Pickup, M28.Relay, M28.Response.

## M29 — DUST & DIESEL

Prerequisite: M28. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Survey the transfer depot** — ReachZoneObjective: Guess: reach the yellow rail-depot entrance. The fuel will leave by road. |
| 2 | Ice | **Secure the loading valve** — KillTargetsObjective: Ice: clear the five red guards. Keep the tanker intact. |
| 3 | Ice | **Transfer the fuel** — MissionInteraction: Ice: open the marked transfer valve and fill the tanker |
| 4 | Guess | **Take the tractor** — EnterVehicleObjective: Guess: take the orange-marked Phantom tractor attached to the fuel tanker. |
| 5 | Guess | **Fuel for the bunker** — DeliverVehicleObjective: Guess: deliver the Phantom AND its tanker to the bunker. Reconnect if you detach it. |
| 6 | Guess | **Unload the reserves** — MissionInteraction: Guess: stop beside the bunker fuel connection and unload |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M29.Approach, M29.Cover, M29.Delivery, M29.Truck, M29.Valve.

## M30 — REDLINE RIDGE

Prerequisite: M29. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Collect the satellite parts** — EnterVehicleObjective: Guess: take the Dubsta 6x6. Ice and Gohan ride with the parts. |
| 2 | Guess | **Down the ridge** — DeliverVehicleObjective: Guess: drive the loaded 6x6 to the yellow canyon bend. Use the road, not the cliff face. |
| 3 | Guess | **Sheltered approach** — DeliverVehicleObjective: Guess: follow the yellow road marker out of the canyon. Passengers cover the gunship. |
| 4 | Guess | **Deliver the parts** — DeliverVehicleObjective: Guess: park the same 6x6 at the bunker unloading marker. |
| 5 | Guess | **Unload** — MissionInteraction: Guess: unload the satellite parts |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M29.Delivery, M30.Bend, M30.Exit, M30.Start.

## SM01 — LEAD & KEVLAR

Prerequisite: M03. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Breach** — ReachZoneObjective: Breach the side entrance. |
| 2 | Ice | **Clear the floor** — KillTargetsObjective: Clear Sergei's men. |
| 3 | Ice | **Sergei** — MissionInteraction: Ice: approach Sergei and demand the crate codes |
| 4 | Ice | **The crates** — MissionInteraction: Ice: load the two marked crates into your car |
| 5 | Ice | **Bring it back** — DeliverVehicleObjective: Ice: drive the crates back to your door. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: SM01.CrateLoad, SM01.SergeiOffice, SM01.WarehouseGate.

## SM02 — ZERO-DAY INJECTION

Prerequisite: M03. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Rooftop** — ReachZoneObjective: Get onto the annex roof. |
| 2 | Gohan | **Server bay** — SubdueTargetsObjective: Gohan: use the stun gun on both marked guards. Keep them alive. |
| 3 | Gohan | **Root terminal** — MissionInteraction: Inject the worm at the root terminal. |
| 4 | Gohan | **Fire escape** — ReachZoneObjective: Down the fire escape before IT traces the connection. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: SM02.Exit, SM02.RoofAccess, SM02.ServerBay, SM02.Terminal.

## SM03 — MIDNIGHT DRIFT

Prerequisite: M03. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Starting line** — EnterVehicleObjective: Get in the drift coupe. |
| 2 | Guess | **Three laps** — RaceCheckpointObjective: Win the circuit — three laps. |
| 3 | Guess | **They pulled guns** — KillTargetsObjective: Stop the marked shooters OR drive the coupe to the yellow finish marker.<br>DeliverVehicleObjective: Escape in the drift coupe to the marked finish, or stop the shooters. |
| 4 | Guess | **The chop bay** — DeliverVehicleObjective: Guess: drive the coupe and the prize to the Burro Heights chop bay. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M11.ChopShop, SM03.Checkpoint1, SM03.Checkpoint2, SM03.Checkpoint3, SM03.Checkpoint4, SM03.StartLine.

## SM04 — DEAD DROP QUARRY

Prerequisite: M28. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Identify the nests** — ReachZoneObjective: Ice: reach the yellow quarry overlook. Both marksmen will be marked red. |
| 2 | Ice | **First marksman** — KillTargetsObjective: Ice: eliminate the first red-marked marksman from cover. |
| 3 | Ice | **Second marksman** — KillTargetsObjective: Ice: eliminate the remaining marksman before collecting the radios. |
| 4 | Ice | **First radio** — MissionInteraction: Ice: collect the patrol radio at the first marked nest |
| 5 | Ice | **Second radio** — MissionInteraction: Ice: collect the other marksman's radio |
| 6 | Ice | **Leave the quarry** — ReachZoneObjective: Ice: take both radios back to the yellow approach marker. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: SM04.Approach, SM04.NestOne, SM04.NestTwo, SM04.Overlook.

## SM05 — BLACK BOX ESTUARY

Prerequisite: M28. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Into the estuary** — EnterVehicleObjective: Gohan: board the marked dinghy. Follow the yellow route to the monitoring buoy. |
| 2 | Gohan | **Find the service harness** — DeliverVehicleObjective: Gohan: stop the dinghy within 12m of the yellow buoy marker. |
| 3 | Gohan | **Fit the interceptor** — MissionInteraction: Gohan: exit the dinghy and swim beside the buoy service harness |
| 4 | Gohan | **Back aboard** — EnterVehicleObjective: Gohan: climb back into the orange-marked dinghy. |
| 5 | Gohan | **Shore pickup** — DeliverVehicleObjective: Gohan: return the dinghy to the yellow water pickup beside shore. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: SM05.Boat, SM05.Buoy, SM05.Shore.

## SM06 — CANYON RUNNER

Prerequisite: M28. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Take the fuel rig** — EnterVehicleObjective: Guess: take the marked Phantom tractor. Keep its aviation-fuel tanker attached. |
| 2 | Guess | **Canyon run** — DeliverVehicleObjective: Guess: take the fuel rig through the yellow canyon-road checkpoint. Slow for corners. |
| 3 | Guess | **Airfield reserves** — DeliverVehicleObjective: Guess: deliver the tractor and attached tanker to McKenzie's yellow fuel marker. |
| 4 | Guess | **Unload safely** — MissionInteraction: Guess: unload the tanker at the airfield connection |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: SM06.Approach, SM06.Bend, SM06.Delivery, SM06.Truck.

## Boundaries of this audit

The harness exercises objective progression, role ownership, fail/retry cleanup, scene release and persistence with GTA stand-ins. It does not establish road driveability, roof access, helicopter aim, swimming/streaming, trailer physics or camera framing. New desert positions remain estimates; safe-ground/water checks reject unavailable sites instead of deploying in the sky. Use the survey when you return. M31–M70 and SM07–SM09 are detailed in CAMPAIGN-REMAINDER.md and remain unimplemented gameplay.

# Playable mission flow and retry map

54 scripted missions. Generated from current production stage declarations. These are code-checked flows, not live playthrough results.

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
| 6 | Guess | **The depot answers** — TravelObjective: Guess: get back to the depot. Ice and Gohan are pinned at the truck. |
| 7 | any brother | **Hold the depot** — KillTargetsObjective: Clear the depot: the gunmen marked RED, and the cars pulling in behind you. Switch to any brother. |
| 8 | Guess | **Run it home** — EnterVehicleObjective: Guess: take the orange-marked Benson truck (driver seat). Ice rides with you; Gohan rides in the back.<br>ConditionObjective: Ice is boarding the cab and Gohan the back of the Benson. |
| 9 | Guess | **Cypress Flats** — LoseWantedObjective: Lose the police before the foundry. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: Base.CypressFlats, M03.ArrivalSpawn, M03.ArrivalStop, M03.CraneControls, M03.Crate, M03.DepotGate, M03.HaulerSpawn, M03.RailJunction.

## M04 — SEVERED WIRE

Prerequisite: M03. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Drive to the lot** — TravelObjective: Guess: meet the crew at the Pillbox Hill lot. Ice and Gohan have their own approaches. |
| 2 | Guess | **In position** — WaitForRolesObjective: Hold at the exit. Ice and Gohan are in position. |
| 3 | Gohan | **Kill the lights** — MissionInteraction: Gohan: cut the marked surface-lot breaker |
| 4 | Gohan | **Miller runs** — SwitchWindowObjective: Miller is running. Take Guess to intercept; Ron is already on him |
| 5 | Guess | **Run him down** — PursueTargetObjective: Guess: chase the red marker. Disable Miller's car or stop Miller, then collect his drive. |
| 6 | Guess | **Recover the drive** — MissionInteraction: Guess: collect Miller's drive |
| 7 | Guess | **Lose them** — LoseWantedObjective: Lose the police. Ice and Gohan are making their own way back to the hideout. |
| 8 | Guess | **Back to the hideout** — TravelObjective: Guess: meet Ice and Gohan back at the Cypress Flats hideout. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: Base.CypressFlats, M04.Breaker, M04.ChaseCar, M04.GarageEntry, M04.IceWatch, M04.RampGuards.

## M05 — TIDAL LOCK

Prerequisite: M04. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Cliff overwatch** — KillTargetsObjective: Ice — take the generator crew off the cave mouth. |
| 2 | Guess | **Light the cove** — Follow the current objective; detailed rule is defined by this stage’s objective type. |
| 3 | Guess | **Breach the grotto** — Follow the current objective; detailed rule is defined by this stage’s objective type. |
| 4 | Guess | **Disable Mateo's boat** — Follow the current objective; detailed rule is defined by this stage’s objective type. |
| 5 | Gohan | **Take him aboard** — MissionInteraction: Gohan: take Mateo aboard the stopped dinghy. Press E / D-pad Right |
| 6 | any brother | **Mateo's account** — DialogueFinishedObjective: Hold the dinghy. Mateo is aboard. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M05.Chase1, M05.Chase2, M05.Chase3, M05.Chase4, M05.Chase5, M05.Chase6, M05.Chase7, M05.CliffPerch, M05.DinghySpawn, M05.GrottoMouth, M05.LightCrew, M05.Sandbar.

## M06 — CLEAN SWEEP

Prerequisite: M05. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Cut the power** — MissionInteraction: Gohan: cut the marked power feeder |
| 2 | Ice | **Sally port** — ReachZoneObjective: Ice: walk into the yellow depot entrance marker. |
| 3 | Ice | **Burn the racks** — AssignedWorkObjective: Gohan is preparing the thermite. Ice: hold the alley while he works.<br>SurviveWavesObjective: Ice: defeat the RED-marked SWAT waves while Gohan finishes the burn. Stay on Ice. |
| 4 | Guess | **The pickup** — ReachZoneObjective: Guess: bring the Granger to the alley mouth for Ice and Gohan. |
| 5 | any brother | **Everyone aboard** — ConditionObjective: Hold at the alley mouth until all three are in the Granger. Once aboard, any brother can continue. |
| 6 | any brother | **Out of Vespucci** — LoseWantedObjective: Lose the police.<br>ConditionObjective: Keep all three aboard the Granger for the escape. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M06.AlleyHold, M06.ConvoySpawn, M06.ConvoyStop, M06.Culvert, M06.Feeder, M06.GrangerSpawn, M06.SallyPort, M06.ServerRacks.

## M07 — WIRETAP WALTZ

Prerequisite: M06. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **The mast** — ReachZoneObjective: Ice — parachute from the tower to the marked roof and reach the antenna platform. |
| 2 | Ice | **Clamp the receiver** — MissionInteraction: Ice: fit the sniffer to the satellite dish. Press E / D-pad Right. |
| 3 | Ice | **Off the roof** — ReachZoneObjective: Grab the chute by the platform, jump, and reach Guess's marked pickup below. |
| 4 | Ice | **Moving pickup** — EnterVehicleObjective: Ice: board a rear seat behind Guess. F / Y, or E / D-pad Right beside the car. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M07.GarageRoof, M07.IceStart, M07.LandingZone, M07.MastTop.

## M08 — SUPPLY & SEVER

Prerequisite: M07. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Loop the cameras** — MissionInteraction: Gohan: use the security cabinet outside the port security cabin to loop CCTV. Press E / D-pad Right. |
| 2 | Ice | **Drop the sentries** — KillTargetsObjective: Ice — drop the sentries at the marked warehouse posts. |
| 3 | Guess | **Take the forklift** — EnterVehicleObjective: Guess — take the forklift. |
| 4 | Guess | **Crate one** — Follow the current objective; detailed rule is defined by this stage’s objective type. |
| 5 | Guess | **Crate two, the technical** — DestroyVehicleObjective: Ice — put the Aegis technical down.<br>ConditionObjective: Secure both turbine crates on the flatbed. |
| 6 | Guess | **Take the hauler** — EnterVehicleObjective: Guess — take the flatbed. Ice rides with you; Gohan brings the Granger. |
| 7 | Guess | **Lose the police** — LoseWantedObjective: Lose the police before the stash. |
| 8 | Guess | **The stash** — DeliverVehicleObjective: Guess: bring the loaded flatbed to the connector stash. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M08.CameraRoom, M08.Connector, M08.CratePadOne, M08.CratePadTwo, M08.HaulerSpawn, M08.WarehouseGate.

## M09 — ROLLING THUNDER

Prerequisite: M08. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Get airborne** — EnterVehicleObjective: Guess: get into the Frogger's pilot seat.<br>ConditionObjective: Guess: lift off at least 8m above the ground; then the convoy starts moving. |
| 2 | Guess | **Shadow the convoy** — Follow the current objective; detailed rule is defined by this stage’s objective type. |
| 3 | Ice | **Take the driver** — KillTargetsObjective: Ice: shoot the marked rear escort DRIVER through the cab window. Keep his truck intact; Guess holds the Frogger overhead. |
| 4 | Ice | **Rip the transponder** — MissionInteraction: Ice: get out, approach the stopped escort cab, and take its IFF transponder. |
| 5 | Guess | **The pickup** — DeliverVehicleObjective: Guess: land the Frogger on the marked open flat north of the road. |
| 6 | Ice | **Ice aboard** — EnterVehicleObjective: Ice: board the landed Frogger. Use the passenger door or E / D-pad Right nearby. |
| 7 | Guess | **Transponder extraction** — DeliverVehicleObjective: Guess: fly Ice and the transponder to the marked drop point and land.<br>ConditionObjective: Keep Ice aboard with the transponder. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M09.AmbushPoint, M09.Bunker, M09.ConvoyStart, M09.GohanStart, M09.HeliSpawn, M09.IceStart, M09.Pickup.

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
| 3 | Ice | **Manifold pressure** — DynoObjective: Ice: RT / W raises pressure; LT / S lowers it. Release both to HOLD 20-30 PSI. Stay in the driver seat. |
| 4 | Ice | **Berth 44** — DialogueFinishedObjective: Gohan has the manifest. Hear him out. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M11.ChopShop, M11.DynoPad, M11.FlatbedSpawn, M11.GohanStart, M11.GuessStart, M11.IceStart, M11.Workbench.

## M12 — BLACK TIDE RECON

Prerequisite: M11. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Launch the ROV** — EnterVehicleObjective: Gohan — take the ROV out from the south jetty. |
| 2 | Gohan | **Under the sonar** — DeliverVehicleObjective: Gohan: descend in the sub to the underwater yellow marker. |
| 3 | Gohan | **Map the hull** — MissionInteraction: Acoustic-scan hold 3's bulkhead. |
| 4 | Gohan | **Back to the jetty** — DeliverVehicleObjective: Gohan: surface in the sub beside the jetty. |
| 5 | any brother | **Clear the hunting launches** — KillTargetsObjective: Ice / Gohan: stop the armed crews on both pursuit boats. Switch freely now the sub is back at the jetty. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M12.FreighterHull, M12.GuessStart, M12.KrakenReturn, M12.KrakenSpawn, M12.PatrolRoute, M12.PatrolSpawn, M12.PierWatch, M12.SonarBuoy, M12.SouthJetty.

## M13 — SMUGGLER'S CUT

Prerequisite: M12. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Into the basin** — EnterVehicleObjective: Ice — take the water scooter into the basin. |
| 2 | Ice | **Limpets** — MultiHoldObjective: Ice: approach the marked side of each fuel boat and press E / D-pad Right to plant a charge. |
| 3 | Ice | **Clear the water** — ReachZoneObjective: Ice: return to the southern quay, climb out onto the dock, and reach Guess at the car marker. |
| 4 | Ice | **Board Guess's car** — EnterVehicleObjective: Ice: get into an empty passenger seat in Guess's Granger (F / Y or E / D-pad Right). |
| 5 | Ice | **Blow the basin** — MissionInteraction: Ice: board the Granger, then trigger the planted charges |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M13.AlarmSpawn, M13.AlarmSpawn2, M13.BargeOne, M13.BargeThree, M13.BargeTwo, M13.CanalSlipway, M13.KayakLaunch, M13.Watchman.

## M14 — AIRSPACE BLACKOUT

Prerequisite: M13. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Overwatch** — KillTargetsObjective: Ice — clear the apron from the ridge. |
| 2 | Guess | **The hangar** — ReachZoneObjective: Guess — get to the hangar door. |
| 3 | Guess | **Hotwire** — EnterVehicleObjective: Guess: take the marked jammer aircraft. Fight or board under fire. |
| 4 | Guess | **Under the radar** — AltitudeCeilingObjective: Hug the terrain — stay under 50 meters above the terrain.<br>DeliverVehicleObjective: Guess: land the jammer aircraft at McKenzie and stop. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M14.ApronGuard, M14.GohanStart, M14.GrangerSpawn, M14.GuessStart, M14.HangarDoor, M14.HangarGuard, M14.McKenzieHangar, M14.OverwatchRidge, M14.PlaneSpawn.

## M15 — CRAWLSPACE

Prerequisite: M14. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Leave the arrival car** — ConditionObjective: Gohan: get out of Guess's car. Ice exits with you; Guess waits here for extraction. |
| 2 | Gohan | **Maintenance level** — ReachZoneObjective: Gohan: reach the marked maintenance access outside the administration building. |
| 3 | Ice | **Clear the rounds** — SubdueTargetsObjective: Ice — put the watchmen down without killing them. |
| 4 | Gohan | **Splice the trunk** — MissionInteraction: Gohan — splice the optical bypass. |
| 5 | any brother | **Out clean** — ReachZoneObjective: Gohan: return to Guess at the original arrival car. Ice: cover the withdrawal.<br>EnterVehicleObjective: Board Guess's car. Use F / Y or the interact button at an empty passenger door.<br>ConditionObjective: Get Ice and Gohan aboard. Switch freely to help either brother return to Guess. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M15.AdminEntry, M15.Cabinet, M15.Exit, M15.FiberSplice, M15.GrangerSpawn, M15.MaintenanceVault, M15.ServiceOffice, M15.Watchman, M15.Watchman1, M15.Watchman2, M15.Watchman3.

## M16 — THE HEAVY LIFT

Prerequisite: M15. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Walk in** — ReachZoneObjective: Ice: drive from the freeway approach to the hangar, then clear its guards. |
| 2 | Ice | **Take the helipad** — KillTargetsObjective: Clear the military police off the pad. |
| 3 | Guess | **Spool the twins** — EnterVehicleObjective: Guess — take the Cargobob. |
| 4 | Guess | **Ice aboard** — ConditionObjective: Hold the pad while Ice boards. |
| 5 | Guess | **Raton Canyon** — AltitudeCeilingObjective: Hug the canyon — stay under 60 meters above terrain.<br>DeliverVehicleObjective: Guess: fly the Cargobob through the marked canyon route. |
| 6 | Guess | **Clear the canyon** — ReachZoneObjective: Guess: clear the canyon exit. Gohan keeps base anti-air offline until the lift is delivered. |
| 7 | Guess | **Terminal Island** — DeliverVehicleObjective: Put the Cargobob down at Terminal Island. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M16.CanyonRun, M16.CargobobSpawn, M16.DepotFence, M16.GrangerSpawn, M16.Helipad, M16.Marine, M16.TankEntry, M16.TerminalDrop.

## M17 — SUB-ZERO PAYLOAD

Prerequisite: M16. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Calibrate the torches** — Follow the current objective; detailed rule is defined by this stage’s objective type. |
| 2 | Guess | **Grapple test** — MissionInteraction: Guess — test the fifty-ton magnetic lock. |
| 3 | Gohan | **The release** — MissionInteraction: Gohan: configure the external grapple release at the marked workbench. |
| 4 | Gohan | **Collect the Kraken** — EnterVehicleObjective: Gohan: collect the prepared Kraken from the water beside the dock. |
| 5 | Gohan | **Ready** — DialogueFinishedObjective: Finish the radio check before staging the heist. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M17.DrySlip, M17.GearSpawn, M17.GohanStart, M17.GuessStart, M17.IceStart, M17.KrakenSpawn, M17.ReleasePoint, M17.WeldOne, M17.WeldThree, M17.WeldTwo, M17.Workbench.

## M18 — THE STAGING LINE

Prerequisite: M17. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Sub into the channel** — EnterVehicleObjective: Gohan — take the Kraken out.<br>DeliverVehicleObjective: Hold her in the channel. |
| 2 | Guess | **Bird in the hangar** — DeliverVehicleObjective: Guess — put the Cargobob in the salt hangar. |
| 3 | Guess | **Collect the jammer** — MissionInteraction: Guess: collect the jammer from the equipment box. |
| 4 | Guess | **Fit the jammer to the lift** — MissionInteraction: Guess: fit the jammer at the rear of the Cargobob. |
| 5 | Ice | **Load the launchers** — DeliverVehicleObjective: Ice — bring the hauler onto the line. |
| 6 | Ice | **Load the parked hauler** — MissionInteraction: Ice: collect the launchers from the marked equipment crate. |
| 7 | Ice | **Secure the launchers in the truck** — MissionInteraction: Ice: load and secure the launchers at the back of the hauler. |
| 8 | Ice | **Countdown** — DialogueFinishedObjective: Keep your assigned vehicle in place. Listen to the final radio check. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M18.CargobobSpawn, M18.ChannelMark, M18.HaulerMark, M18.HaulerSpawn, M18.KrakenSpawn, M18.LauncherCrate, M18.LauncherWork, M18.PodCrate, M18.PodWork, M18.SaltHangar.

## M19 — THE PORT HEIST: UNDERWATER BREACH

Prerequisite: M18. Story gate: SM01, SM02, SM03 must be complete first (QA may bypass). Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Dive** — EnterVehicleObjective: Take the Kraken down. |
| 2 | Gohan | **Cut the bulkhead** — MissionInteraction: Burn the breach into hold 3. |
| 3 | Gohan | **Clamp the floats** — MultiHoldObjective: Clamp the ballast floats to the container. |
| 4 | Gohan | **Surface** — SurfaceSubObjective: Gohan: surface the Kraken at the support marker. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M12.PierWatch, M18.SaltHangar, M19.BullionSurface, M19.ClampOne, M19.ClampTwo, M19.DiveStart, M19.HullBreach, M19.Surface, M19.Worksite.

## M20 — THE PORT HEIST: SKY HOOK

Prerequisite: M19. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Get on the water** — EnterVehicleObjective: Guess — take the Cargobob over the basin. |
| 2 | Ice | **Suppress the deck** — KillTargetsObjective: Ice — clear the marked quayside gunners from the pier. |
| 3 | Guess | **Lock the cable** — MissionInteraction: Guess — hold the hover over the container. |
| 4 | Guess | **Climb out** — DeliverVehicleObjective: Guess: climb in the Cargobob to the elevated yellow marker. |
| 5 | Guess | **The escort** — ConditionObjective: Guess: hold the lift over the basin while Gohan and Ice board the escort launch. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M12.PierWatch, M18.SaltHangar, M19.Surface, M20.ClimbOut, M20.DeckGunners, M20.HoverPoint, M21.LaunchSpawn.

## M21 — THE PORT HEIST: OPEN WATER

Prerequisite: M20. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Get on the water** — EnterVehicleObjective: Gohan — take the armed launch. |
| 2 | Gohan | **Draw the locks** — ShadowTargetObjective: Stay on the Cargobob's wing. |
| 3 | Gohan | **Kill the speedboats** — KillTargetsObjective: Clear the Aegis boats before they close. |
| 4 | Gohan | **The breakwater** — DeliverVehicleObjective: Gohan: take the launch through the yellow breakwater exit. |
| 5 | Gohan | **Shore transfer** — DeliverVehicleObjective: Gohan: bring the launch in to the marked shore landing. |
| 6 | Gohan | **The road north** — ConditionObjective: Gohan and Ice: out of the launch and into the Granger at the road, Gohan at the wheel. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M12.PierWatch, M21.Breakwater, M21.LaunchSpawn, M21.RidgeCross, M21.RoadPickup, M21.ShoreLanding.

## M22 — THE PORT HEIST: SCORCHED BAY

Prerequisite: M21. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Bring it in** — EnterVehicleObjective: Guess — fly the bullion into the Alamo. |
| 2 | Guess | **Drop the container** — MissionInteraction: Guess: hover 20m over the water marker and release the container |
| 3 | Guess | **The beach** — DeliverVehicleObjective: Guess: land the Cargobob on the marked shore and stop. |
| 4 | Guess | **Regroup** — ConditionObjective: Guess: on foot to Ice and Gohan on the beach. Wait for their road arrival. |
| 5 | Guess | **Blaine County** — DialogueFinishedObjective: The foundry is gone. Listen. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: Base.CypressFlats, M12.PierWatch, M22.AlamoDrop, M22.Beach, M22.RoadArrival.

## M23 — GHOST IN THE SAGE

Prerequisite: M22. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Drive to the approach** — DeliverVehicleObjective: Guess: drive the crew to the marked bunker approach and stop. Ice takes point once you arrive.<br>ConditionObjective: Stop the crew car with Ice and Gohan aboard. |
| 2 | Ice | **Approach the bunker** — ReachZoneObjective: Ice: approach the marked bunker entrance. Guess covers the west side; Gohan watches the generator. |
| 3 | Ice | **Clear the bunker yard** — KillTargetsObjective: Clear the cartel squatters out. |
| 4 | Guess | **Secure the bays** — MultiHoldObjective: Guess: inspect the marked workbench, empty fuel drum, and vehicle storage bay. |
| 5 | Gohan | **Power up** — MissionInteraction: Gohan: use the control side of the visible generator to restore bunker access. |
| 6 | Gohan | **Enter the bunker** — Follow the current objective; detailed rule is defined by this stage’s objective type. |
| 7 | Gohan | **Check the interior** — MissionInteraction: Gohan: inspect the bunker entry room at the yellow marker. Stay here while checking the lights and shelter; the crew waits outside. |
| 8 | Gohan | **Return outside** — Follow the current objective; detailed rule is defined by this stage’s objective type. |
| 9 | Gohan | **Regroup at the entrance** — ReachZoneObjective: Gohan: rejoin the crew at the bunker entrance. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M23.Approach, M23.Entrance, M23.EscapeRoad, M23.FuelBay, M23.Guard, M23.GuessStart, M23.PowerPanel, M23.RoadStart, M23.ToolBay, M23.VehicleBay.

## M24 — LIQUID GOLD

Prerequisite: M23. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Into the shallows** — DeliverVehicleObjective: Guess — park the recovery truck at the dry shoreline marker. |
| 2 | Ice | **Dredge the crates** — AssignedWorkObjective: Guess works the recovery cable. Ice: cover him from the ridge.<br>SurviveWavesObjective: Ice — keep the deputies off the haul. |
| 3 | Ice | **Gohan aboard** — ConditionObjective: Gohan is boarding the truck. Hold here until he is in. |
| 4 | Guess | **Back to the bunker** — DeliverVehicleObjective: Get the haul to the radar base. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M22.AlamoDrop, M23.ToolBay, M23.VehicleBay, M24.CraneSpawn, M24.GohanWork, M24.IceStart, M24.RecoveryPad, M24.RidgeLine, M24.RidgeRoad.

## M25 — BOUNTY HUNTERS' CANYON

Prerequisite: M24. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **High ground** — ReachZoneObjective: Take the bridge deck. |
| 2 | Ice | **Seal the pass** — DestroyVehicleObjective: Detonate the fuel tanker across the southern pass. |
| 3 | Ice | **Hold the bridge** — SurviveWavesObjective: Ice: defeat the three red-marked assault waves. Use cover and your rifle. |
| 4 | Ice | **Off the bridge** — ReachZoneObjective: Ice: parachute down toward the marked extraction boat. |
| 5 | Ice | **River extraction** — EnterVehicleObjective: Ice: board the extraction boat as a passenger. |
| 6 | any brother | **Clear the pickup** — ConditionObjective: Stay in the boat while Guess takes you at least 100 meters clear of the pickup. You can switch to Guess and drive. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M25.BoatEscape, M25.BridgeDeck, M25.DeckApproach, M25.HunterApproach, M25.NorthTunnel, M25.RimPost, M25.Riverbed, M25.SouthTunnel, M25.TankerSpot.

## M26 — THE ALAMO SCRAMBLE

Prerequisite: M25. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Scramble** — EnterVehicleObjective: Guess — take off in the marked Lazer. |
| 2 | Guess | **First spotter** — DestroyVehicleObjective: Splash the lead spotter. |
| 3 | Guess | **The charter** — ConditionObjective: Keep the second spotter in sight while Gohan pulls the charter's call sign from his traffic. |
| 4 | Guess | **Second spotter** — DestroyVehicleObjective: The second one is diving for Grapeseed — kill him. |
| 5 | Guess | **Home** — DeliverVehicleObjective: Guess: land the Lazer at McKenzie and stop. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M26.CrewCar, M26.CrewStart, M26.DusterPad, M26.GohanPost, M26.IcePost, M26.PatrolBox, M26.RunwayStart, M26.SparePlane.

## M27 — FLIGHT RISK

Prerequisite: M26. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Get on his rudder** — EnterVehicleObjective: Guess — take the Duster up with Ice in the second seat. |
| 2 | Guess | **Match the Shamal** — ShadowTargetObjective: Climb to the Shamal and hold station inside 60 meters. |
| 3 | Ice | **The locker** — MissionInteraction: Ice: take the flight ledger from the cabin locker |
| 4 | Ice | **Terminal dive** — BailOutObjective: The pilot put her over — get out. |
| 5 | Ice | **Sea pickup** — EnterVehicleObjective: Ice: parachute to the green boat marker, then climb aboard Gohan's dinghy. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M26.DusterPad, M26.RunwayStart, M26.SparePlane, M27.CrewStart, M27.FormUp, M27.IcePost, M27.JetTrack, M27.ParkedLazer, M27.RunwayStart, M27.SeaPickup.

## M28 — OFF THE GRID

Prerequisite: M27. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Identify the relay** — ReachZoneObjective: Gohan: reach the yellow relay-yard entrance. Ice covers the opposite approach. |
| 2 | Ice | **Clear the transformer yard** — KillTargetsObjective: Ice: clear the four red-marked relay guards before Gohan enters. |
| 3 | Gohan | **Read the cabinet** — TechnicalChoiceObjective: Gohan: choose which relay system to cut first |
| 4 | Gohan | **Connect the surge unit** — MissionInteraction: Gohan: connect the case to the laptop on the relay worktable |
| 5 | Ice | **Cover the splice** — AssignedWorkObjective: Gohan continues the splice. Ice: defeat the responding squads.<br>SurviveWavesObjective: Ice: clear both response squads marked red. |
| 6 | Guess | **Extraction** — EnterVehicleObjective: Guess: take the Granger driver seat. Wait for both brothers to board. |
| 7 | Guess | **Back to shelter** — DeliverVehicleObjective: Guess: bring the crew's Granger back to the Senora bunker. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M23.VehicleBay, M28.Approach, M28.Cover, M28.Pickup, M28.Relay, M28.Response.

## M29 — DUST & DIESEL

Prerequisite: M28. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Survey the transfer depot** — ReachZoneObjective: Guess: reach the yellow rail-depot entrance. The fuel will leave by road. |
| 2 | Ice | **Secure the loading valve** — KillTargetsObjective: Ice: clear the five red guards. Keep the tanker intact. |
| 3 | Ice | **Transfer the fuel** — MissionInteraction: Ice: use the laptop on the marked transfer table to fill the coupled tanker |
| 4 | Guess | **Take the tractor** — EnterVehicleObjective: Guess: take the orange-marked Phantom tractor attached to the fuel tanker. The brothers will ride or follow in another car. |
| 5 | Guess | **Fuel for the bunker** — DeliverVehicleObjective: Guess: deliver the Phantom AND its tanker to the bunker. Reconnect if you detach it. |
| 6 | Guess | **Unload the reserves** — MissionInteraction: Guess: keep the rig stopped at the bunker and start unloading the fuel reserves |
| 7 | Guess | **Reserves received** — ConditionObjective: The fuel is being secured in the bunker yard. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M23.FuelBay, M29.Approach, M29.Cover, M29.Senora.Delivery, M29.Truck, M29.Valve.

## M30 — REDLINE RIDGE

Prerequisite: M29. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Collect the satellite parts** — EnterVehicleObjective: Guess: take the Dubsta 6x6. Ice and Gohan ride with the parts. |
| 2 | Guess | **Down the ridge** — DeliverVehicleObjective: Guess: drive the loaded 6x6 to the yellow canyon bend. Use the road, not the cliff face. |
| 3 | Guess | **Sheltered approach** — DeliverVehicleObjective: Guess: follow the yellow road marker out of the canyon. Passengers cover the gunship. |
| 4 | Guess | **Deliver the parts** — DeliverVehicleObjective: Guess: park the same 6x6 at the bunker unloading marker. |
| 5 | Guess | **Unload** — MissionInteraction: Guess: stop the 6x6 in the unloading area; transfer its roof case to the bunker workbench |
| 6 | Gohan | **Fit the receiver** — MissionInteraction: Gohan: get out and use the laptop beside the delivered parts on the marked bunker workbench |
| 7 | Gohan | **First reception** — ConditionObjective: Listen to the receiver check. The parts must be fitted before the job is complete. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M23.ToolBay, M29.Senora.Delivery, M30.Bend, M30.Exit, M30.Start.

## M31 — THE IRON PERIMETER

Prerequisite: M30. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Read the approaches** — MultiHoldObjective: Ice: inspect each yellow approach marker beside the three barriers; press E / D-pad Right |
| 2 | Gohan | **Arm the perimeter** — MultiHoldObjective: Gohan: arm each marked barrier charge; press E / D-pad Right |
| 3 | Guess | **Prove the withdrawal road** — TravelObjective: Guess: drive the crew car to the yellow withdrawal marker and stop. Leave the road clear. |
| 4 | Guess | **Hold the perimeter** — KillTargetsObjective: Defend the generator. Stop the marked probe convoy; planted charges fire only when enemies enter their lane and the crew is clear. |
| 5 | Gohan | **Check the damage** — MissionInteraction: Gohan: inspect the surviving generator controls |
| 6 | Gohan | **Keep the base concealed** — LoseWantedObjective: Lose any police pursuit before returning to the perimeter. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M31.Senora.Barrier, M31.Senora.Barrier1, M31.Senora.Barrier2, M31.Senora.Charge, M31.Senora.Convoy1, M31.Senora.Convoy2, M31.Senora.CrewCar, M31.Senora.Generator, M31.Senora.GeneratorWork, M31.Senora.GohanStart, M31.Senora.IceStart, M31.Senora.Retreat, M31.Senora.Work.

## M32 — BLACK SITE ZANCUDO

Prerequisite: M31. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Reach the coastal landing** — TravelObjective: Gohan: pilot the dinghy to the yellow coastal landing and stop near shore |
| 2 | Gohan | **Open exterior access** — MissionInteraction: Gohan: leave the dinghy, walk up the bank and use the marked exterior electrical cabinet |
| 3 | Ice | **Secure the ordnance yard** — KillTargetsObjective: Ice: stop the four marked yard guards. Keep both yellow EMP cases intact. |
| 4 | Guess | **Bring the case carrier** — TravelObjective: Guess: drive the crew car to the yellow pickup beside the cleared ordnance post and stop |
| 5 | Gohan | **First case** — MissionInteraction: Gohan: pick up the first marked EMP case |
| 6 | Gohan | **Stow first case** — MissionInteraction: Gohan: carry the case to the rear of Guess's car and stow it |
| 7 | Gohan | **Second case** — MissionInteraction: Gohan: return for the second marked EMP case |
| 8 | Gohan | **Stow second case** — MissionInteraction: Gohan: stow the second case beside the first in Guess's car |
| 9 | Guess | **Extract the team** — EnterVehicleObjective: Guess: take the extraction car's driver seat<br>ConditionObjective: Stop the car and wait for Ice and Gohan to board their rear seats |
| 10 | Guess | **Leave the base** — TravelObjective: Drive the case-loaded crew car to the yellow road escape marker |
| 11 | Guess | **Lose pursuit** — LoseWantedObjective: Lose the police before bringing military cargo to the bunker. |
| 12 | Guess | **Deliver both cases** — TravelObjective: Return the same car with both cases to the bunker unloading marker and stop |
| 13 | Gohan | **Secure the warheads** — MissionInteraction: Gohan: get out and secure both cases at the marked bunker work area |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M32.Boat, M32.CaseOne, M32.CaseTwo, M32.CrewCar, M32.Exit, M32.Gate, M32.Guard, M32.LandingWater, M32.Office, M32.Panel, M32.PanelWork, M32.Pickup, M32.Response, M32.Senora.Delivery, M32.Senora.Workbench.

## M33 — THE INFORMANT'S GRAVE

Prerequisite: M32. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Identify the prisoner** — ReachZoneObjective: Ice: reach the yellow observation point; Ramos is the unarmed prisoner. Do not shoot him. |
| 2 | Ice | **Stop the execution** — KillTargetsObjective: Stop the four red execution guards before the rescue clock expires. Protect Ramos. |
| 3 | Gohan | **Free Ramos** — MissionInteraction: Gohan: reach Ramos and cut his restraints |
| 4 | Guess | **Bring the extraction car** — TravelObjective: Guess: drive to the yellow pickup marker beside Ramos and stop |
| 5 | Guess | **All four aboard** — ConditionObjective: Keep the car stopped: Ramos takes the front passenger seat, Ice and Gohan take the back |
| 6 | Guess | **Reach armored transport** — TravelObjective: Drive Ramos and both brothers to the yellow armored-transfer marker<br>ConditionObjective: Ramos must remain in the extraction car |
| 7 | Guess | **Move Ramos into cover** — ConditionObjective: Stop beside the half-track and wait for Ramos to board its front passenger seat |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M33.CrewCar, M33.Guard, M33.Halftrack, M33.Observe, M33.Pickup, M33.Ramos, M33.Response, M33.Transfer, M34.Start.

## M34 — MUD & IRON

Prerequisite: M33. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | as assigned | **Leave the transfer site** — ConvoyRouteObjective: Drive the half-track carrying Ramos into the wind-farm route. Ice covers the rear; Gohan follows in the crew car. |
| 2 | as assigned | **First road barrier** — ConvoyRouteObjective: Continue past the first yellow road marker; Gohan drops the barrier only after both crew vehicles are clear<br>ConditionObjective: Wait for Gohan's escort car to clear the first barrier |
| 3 | as assigned | **Second response** — ConvoyRouteObjective: Follow the second yellow road marker through the gully. Keep Ramos in the half-track. |
| 4 | as assigned | **Close the rear route** — ConvoyRouteObjective: Clear the second barrier with both vehicles and follow the shelter road<br>ConditionObjective: Both crew vehicles must clear the second road barrier |
| 5 | as assigned | **Lose the police** — LoseWantedObjective: Lose any police pursuit before arriving at the medical shelter. |
| 6 | as assigned | **Medical shelter** — ConvoyRouteObjective: Stop the half-track at the yellow medical shelter marker with Ramos aboard |
| 7 | Gohan | **Patient first** — ConditionObjective: Wait for Ramos to leave the stopped half-track<br>MissionInteraction: Gohan: get out and prepare the marked medical kit for Ramos |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M34.BarrierPark, M34.BarrierRoad, M34.CrewCar, M34.Exit, M34.Halftrack, M34.Ramos, M34.Response1, M34.Response2, M34.Route1, M34.Route2, M34.Route3, M34.Senora.MedicalKit, M34.Senora.MedicalWork, M34.Senora.Shelter.

## M35 — THE CHIANSKI AMBUSH

Prerequisite: M34. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Prepare the trap** — MultiHoldObjective: Ice: plant both marked roadside charges; press E / D-pad Right |
| 2 | Guess | **Close the far exit** — TravelObjective: Guess: park the crew car across the yellow far-exit marker and stop |
| 3 | Gohan | **Identify the target** — MissionInteraction: Gohan: use the laptop on the marked field table to identify the convoy's gun truck |
| 4 | Gohan | **Watch the pass** — ConditionObjective: Wait in cover for the red lead escort to enter the yellow trap. The orange gun truck must stay intact. |
| 5 | Gohan | **Capture the technical** — KillTargetsObjective: Stop the escort and gun-truck crew. Shoot the occupants, not the orange technical. |
| 6 | Guess | **Take the driver seat** — EnterVehicleObjective: Guess: take the captured technical's driver seat |
| 7 | Guess | **Bring both brothers** — ConditionObjective: Stop the technical: Gohan boards the front passenger seat and Ice takes the rear gun seat |
| 8 | Guess | **Lose pursuit** — LoseWantedObjective: Lose the police before taking the captured technical to the bunker. |
| 9 | Guess | **Deliver the gun truck** — TravelObjective: Deliver the same technical with both brothers to the bunker vehicle bay and stop |
| 10 | Gohan | **Inspect the capture** — MissionInteraction: Gohan: get out and inspect the gun mount at the rear of the parked technical |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M35.BlockExit, M35.Charge, M35.ChargeWork, M35.CrewCar, M35.DeviceWork, M35.IceCover, M35.LeadSpawn, M35.Senora.Delivery, M35.Table, M35.TechnicalHold, M35.TechnicalSpawn, M35.Trap.

## M36 — DEEP WELL RECON

Prerequisite: M35. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Survey band ** — DeliverVehicleObjective: Gohan: descend to underwater survey marker <br>MissionInteraction: Gohan: hold beside the visible seabed sensor and record survey  |
| 2 | Guess | **Move the surface pickup** — TravelObjective: Guess: move the dinghy to the alternate yellow pickup, away from the patrol, and stop |
| 3 | Gohan | **Bring the survey home** — DeliverVehicleObjective: Gohan: surface in the sub beside Guess's relocated dinghy<br>ConditionObjective: The pickup boat must remain at the alternate marker |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M36.AlternatePickup, M36.IceStart, M36.Patrol, M36.PatrolGoal, M36.Scan, M36.Survey.

## M37 — THE GRAPESEED HARVEST

Prerequisite: M36. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **First aircraft** — EnterVehicleObjective: Guess: board the first orange crop duster in its pilot seat |
| 2 | Guess | **First landing** — DeliverVehicleObjective: Guess: land the first crop duster at Sandy Shores and stop in its yellow apron marker |
| 3 | Ice | **Secure the second strip** — KillTargetsObjective: Ice: clear the three red guards around the second aircraft |
| 4 | Ice | **Second aircraft** — EnterVehicleObjective: Ice: take the second crop duster's pilot seat |
| 5 | Ice | **Second landing** — DeliverVehicleObjective: Ice: land the second crop duster at its separate Sandy Shores apron marker and stop |
| 6 | Gohan | **Fit smoke kit ** — MissionInteraction: Gohan: fit the smoke canisters beside parked aircraft  |
| 7 | Gohan | **Check both releases** — MissionInteraction: Gohan: test the smoke release at the first parked aircraft |
| 8 | Gohan | **Check second release** — MissionInteraction: Gohan: test the second aircraft's smoke release |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M37.Duster, M37.Guard, M37.Land1, M37.Land2.

## M38 — BLOOD IN THE QUARRY

Prerequisite: M37. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Bring moving cover** — TravelObjective: Guess: drive the Benson into the yellow loading lane under fire. Ice covers the approach; keep the truck moving until you reach the marker |
| 2 | Guess | **Clear the loading yard** — KillTargetsObjective: Use Ice or fight as Guess: stop the four red quarry guards, using the positioned truck as cover. Keep the yellow packages intact |
| 3 | Gohan | **Release blasting stock** — MissionInteraction: Gohan: unlock the marked stock-control cabinet beside the crates |
| 4 | Gohan | **Collect package ** — MissionInteraction: Gohan: pick up marked charge package  |
| 5 | Gohan | **Load package ** — MissionInteraction: Gohan: carry the package to the back of the stopped Benson |
| 6 | Guess | **All cargo aboard** — EnterVehicleObjective: Guess: take the Benson driver seat<br>ConditionObjective: Stop and wait for Ice in front and Gohan in the rear cargo seat |
| 7 | Guess | **Escape the quarry** — TravelObjective: Drive the loaded Benson out through the yellow quarry escape marker |
| 8 | Guess | **Lose pursuit** — LoseWantedObjective: Lose the police before returning with explosives |
| 9 | Guess | **Deliver seismic stock** — TravelObjective: Stop the same loaded Benson at the bunker delivery marker |
| 10 | Gohan | **Verify the load** — MissionInteraction: Gohan: inspect all four packages at the back of the stopped truck |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M38.Cabinet, M38.CabinetWork, M38.Crate, M38.Exit, M38.GohanStart, M38.Guard, M38.Hauler, M38.IceStart, M38.Load, M38.Response, M38.Senora.Delivery.

## M39 — THE PALETO CABLE

Prerequisite: M38. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Follow cable route ** — DeliverVehicleObjective: Gohan: follow the underwater yellow route marker  |
| 2 | Gohan | **Clamp the junction** — MissionInteraction: Gohan: stop the sub beside the marked cable junction and fit its cutter clamp |
| 3 | Gohan | **Run and verify cutter** — MissionInteraction: Gohan: hold the sub beside the junction while the cutter runs and verify the link is down |
| 4 | Ice | **Protect the return shore** — KillTargetsObjective: Ice: stop the four marked shoreline response guards so Gohan can return |
| 5 | Gohan | **Return with the crew** — DeliverVehicleObjective: Gohan: surface the sub at the yellow cove return marker; Guess's boat is waiting nearby |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M39.CutterWork, M39.Guard, M39.Junction, M39.Return, M39.Route.

## M40 — THE PHANTOM RIGGING

Prerequisite: M39. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Prepare hull kit ** — MissionInteraction: Guess: prepare and fit the sealed reinforcement kit for boat  |
| 2 | Ice | **Test boarding weapons** — ConditionObjective: Ice: use your firearm to hit both yellow floating practice barrels; these are passenger weapons, not mounted boat guns |
| 3 | Gohan | **Verify navigation** — MissionInteraction: Gohan: use the laptop on the shore table to verify both sea-trial routes |
| 4 | Gohan | **Trial boat ** — Follow the current objective; detailed rule is defined by this stage’s objective type. |
| 5 | Gohan | **Run sea trial ** — DeliverVehicleObjective: Drive the assigned Tropic through its offshore yellow test marker |
| 6 | Gohan | **Return boat ** — TravelObjective: Return the same boat to its yellow cove marker and stop |
| 7 | Gohan | **Sign off the fleet** — ConditionObjective: Both tested boats and reinforcement kits must be back at the cove |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M40.Boat, M40.Kit, M40.Nav, M40.NavWork, M40.Return, M40.Return1, M40.Target, M40.Trial.

## M41 — THE GENERAL'S WIRE

Prerequisite: M40. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Identify Bradley** — MissionInteraction: Ice: observe from the yellow lookout and identify the marine officer; spare the lodge worker |
| 2 | Ice | **Wait for a clear shot** — ConditionObjective: Ice: watch Bradley walk clear of the lodge worker; do not fire until he reaches the meeting point |
| 3 | Ice | **Stop Bradley** — ConditionObjective: Ice: eliminate Bradley before he escapes; the red officer carries the access card |
| 4 | Ice | **Recover the card** — MissionInteraction: Ice: recover the access card beside Bradley's body |
| 5 | Ice | **Bring the card to extraction** — MissionInteraction: Ice: bring the card to the yellow rear-of-car marker for Gohan to verify |
| 6 | any brother | **Board the extraction car** — ConditionObjective: Guess: hold the crew car still while Ice takes a rear seat; Gohan stays in the front passenger seat |
| 7 | Guess | **Extract the card** — TravelObjective: Guess: drive the whole crew and Bradley's card to the yellow trail exit and stop<br>ConditionObjective: All three brothers must remain aboard the extraction car |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M41.Bradley, M41.EscapeCar, M41.EscapeEnd, M41.Exit, M41.Guard, M41.Meeting, M41.Observe, M41.Pickup, M41.Worker.

## M42 — SKYFALL DELIVERY

Prerequisite: M41. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Fly the drop corridor** — ConditionObjective: Guess: fly over the offshore yellow ring, 150-350m above the sea, under 85m/s; press E / D-pad Right to release the sub |
| 2 | Guess | **Watch the cargo descent** — ConditionObjective: Guess: circle within 1.2km of the descending sub; keep flying until Gohan confirms a stable splashdown |
| 3 | Gohan | **Pilot the delivered sub** — DeliverVehicleObjective: Gohan: take the floating Kraken to the yellow coastal rendezvous; Guess holds the Titan offshore |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M42.Delivery, M42.Drop, M42.GohanStart, M42.Hold, M42.Titan.

## M43 — STAGING PALETO

Prerequisite: M42. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Position the sub** — TravelObjective: Gohan: move the Kraken to its yellow offshore holding marker and stop |
| 2 | Ice | **Position the launch** — TravelObjective: Ice: move the Tropic to the yellow surface holding marker and stop |
| 3 | Guess | **Land the extraction helicopter** — DeliverVehicleObjective: Guess: fly the Annihilator to the yellow Paleto coastal staging lot and land at its center |
| 4 | Guess | **Check the preparation ledger** — ConditionObjective: Guess: all three vehicles must stay in their holding positions; complete any missing preparation missions shown below |
| 5 | Guess | **Commit the staging plan** — MissionInteraction: Guess: walk to the laptop beside the Paleto landing area and confirm the offshore plan |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M43.Board, M43.BoardWork, M43.Boat, M43.BoatReady, M43.Helicopter, M43.Land, M43.ShoreCar, M43.Sub, M43.SubReady.

## M44 — PALETO DEEP-SEA: SUB-SURFACE

Prerequisite: M43. Story gate: SM04, SM05, SM06 must be complete first (QA may bypass). Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Take the Kraken under the hull** — TravelObjective: Gohan: dive the Kraken to the marker under the hull |
| 2 | Gohan | **Clamp the seismic charges** — Follow the current objective; detailed rule is defined by this stage’s objective type. |
| 3 | Gohan | **Cut the sensor line** — MultiHoldObjective: Gohan: hold at the sensor junction until the line is cut |
| 4 | Gohan | **Surface clear of the hull** — SurfaceSubObjective: Gohan: surface the Kraken at the support marker. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M44.Clamp1, M44.Clamp2, M44.Clamp3, M44.Sensor, M44.Start, M44.Sub, M44.Surface.

## M45 — PALETO DEEP-SEA: HELIPAD BREACH

Prerequisite: M44. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Bring the helicopter over the rail** — TravelObjective: Guess: fly the Annihilator to the hold marker above the vessel |
| 2 | Ice | **Put Ice on the upper deck** — ReachZoneObjective: Ice: get out onto the upper deck |
| 3 | Ice | **Clear the upper deck** — KillTargetsObjective: Ice: clear the deck detail |
| 4 | Gohan | **Bring the Kraken to the stern** — TravelObjective: Gohan: take the Kraken alongside the stern platform |
| 5 | Gohan | **Get Gohan aboard** — ReachZoneObjective: Gohan: climb the stern platform onto the vessel |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M44.Surface, M45.Approach, M45.Board, M45.Helipad, M45.Hold, M45.Stern.

## M46 — PALETO DEEP-SEA: VAULT CRACK

Prerequisite: M45. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Reach the command deck** — ReachZoneObjective: Gohan: take the stairs to the command deck |
| 2 | Gohan | **Use Bradley's card on the vault** — MissionInteraction: Gohan: hold Bradley's card against the vault reader |
| 3 | Gohan | **Take the ledger and the bonds** — MultiHoldObjective: Ice and Gohan: clear the vault shelves |
| 4 | Gohan | **Arm the charges** — MissionInteraction: Gohan: arm the seismic charges from the command console |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M46.Bonds, M46.Bridge, M46.Console, M46.Ledger, M46.Stairs, M46.Vault.

## M47 — PALETO DEEP-SEA: COLLAPSE

Prerequisite: M46. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Put the helicopter down and take the boat** — DeliverVehicleObjective: Guess: land the Annihilator on the cove strip<br>EnterVehicleObjective: Guess: take the Tropic out to the pickup marker |
| 2 | Gohan | **Trigger the charges** — MissionInteraction: Gohan: trigger the charges from the rail |
| 3 | Gohan | **Go off the side** — ConditionObjective: Ice and Gohan: get to the edge and go into the water |
| 4 | Guess | **Pick them up** — ConditionObjective: Guess: bring the boat onto both swimmers until all three are aboard |
| 5 | Guess | **Clear the demolition area** — TravelObjective: Guess: take the boat clear of the burning vessel |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M44.Clamp1, M44.Clamp2, M44.Clamp3, M45.Hold, M47.BoatStart, M47.Clear, M47.Jump, M47.Landing, M47.Trigger.

## M48 — THE ROAD BACK SOUTH

Prerequisite: M47. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Bring the boat ashore** — TravelObjective: Guess: run the boat onto the cove beach |
| 2 | Guess | **Move the crew and the evidence into the technical** — EnterVehicleObjective: All three: get into the technical with the ledger<br>ConditionObjective: Nobody stays at the beach |
| 3 | Guess | **Break the outer cordon** — KillTargetsObjective: Clear the roadblock at the cove exit |
| 4 | Guess | **Run south** — TravelObjective: Drive south past the county line |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: M47.Clear, M48.Cordon, M48.Shore, M48.South, M48.Technical.

## SM01 — LEAD & KEVLAR

Prerequisite: M03. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Breach** — ReachZoneObjective: Ice: enter the open loading lane and confront Sergei's men. |
| 2 | Ice | **Clear the loading lane** — KillTargetsObjective: Clear Sergei's men. |
| 3 | Ice | **Sergei** — MissionInteraction: Ice: approach Sergei and demand the crate codes |
| 4 | Ice | **The crates** — MissionInteraction: Ice: collect the two cases at the outside loading point |
| 5 | Ice | **Bring it back** — DeliverVehicleObjective: Ice: drive the crates back to your door.<br>ConditionObjective: Secure both cases at your door and lose the police. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: SM01.Car, SM01.Case, SM01.CrateLoad, SM01.Guard, SM01.SergeiOffice, SM01.WarehouseGate.

## SM02 — ZERO-DAY INJECTION

Prerequisite: M03. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Rooftop** — MissionInteraction: Gohan: run to the building's service door, then take the maintenance stairs to the roof. |
| 2 | Gohan | **Server bay** — SubdueTargetsObjective: Gohan: use the stun gun on both marked guards. Keep them alive. |
| 3 | Gohan | **Root terminal** — MissionInteraction: Inject the worm at the root terminal. |
| 4 | Gohan | **Fire escape** — MissionInteraction: Gohan: return to the roof access and take the maintenance stairs down before IT traces you. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: SM02.Approach, SM02.Exit, SM02.Guard, SM02.RoofAccess, SM02.ServerBay, SM02.StairEntry, SM02.Terminal.

## SM03 — MIDNIGHT DRIFT

Prerequisite: M03. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Ready on the grid** — MissionInteraction: Guess: ready up in your own car to start the northbound sprint |
| 2 | Guess | **Freeway to Chiliad** — RaceCheckpointObjective: Guess: beat both rivals to the summit. Follow the street, freeway and mountain trail gates. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: SM03.RivalGrid, SM03.Sprint, SM03.StartLine, SM03.Summit.

## SM04 — DEAD DROP QUARRY

Prerequisite: M28. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Ice | **Identify the nests** — ReachZoneObjective: Ice: reach the yellow quarry overlook. Locate both marksmen before moving down to their radios. |
| 2 | Ice | **Clear both listening posts** — KillTargetsObjective: Ice: eliminate BOTH red-marked marksmen. Use the ridge as cover. |
| 3 | Ice | **Recover radio ** — MissionInteraction: Ice: recover radio  |
| 4 | Ice | **Return with both radios** — ReachZoneObjective: Ice: take BOTH radios back to the yellow quarry approach. Gohan will copy their frequencies over the radio. |
| 5 | Ice | **Frequencies copied** — ConditionObjective: Listen while Gohan confirms both recovered radio channels. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: SM04.Approach, SM04.NestOne, SM04.NestTwo, SM04.Overlook.

## SM05 — BLACK BOX ESTUARY

Prerequisite: M28. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Gohan | **Into the estuary** — EnterVehicleObjective: Gohan: board the marked dinghy and drive to the monitoring buoy. |
| 2 | Gohan | **Moor beside the buoy** — DeliverVehicleObjective: Gohan: stop within 8m of the yellow buoy. Your dinghy will wait while you fit the interceptor. |
| 3 | Gohan | **Fit the interceptor** — MissionInteraction: Gohan: exit and swim beside the buoy; fit the interceptor to its marked service point |
| 4 | Gohan | **Verify telemetry** — ConditionObjective: Verify the fitted interceptor and wait for telemetry confirmation. |
| 5 | Gohan | **Back aboard** — EnterVehicleObjective: Gohan: swim back and climb into the orange dinghy. The mooring releases when you are aboard. |
| 6 | Gohan | **Shore pickup** — DeliverVehicleObjective: Gohan: return the dinghy to the yellow water pickup beside shore. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: SM05.Boat, SM05.Buoy, SM05.Shore.

## SM06 — CANYON RUNNER

Prerequisite: M28. Retry: full mission restart.

| Step | Role | Stage and on-screen base instruction |
|---|---|---|
| 1 | Guess | **Take the fuel rig** — EnterVehicleObjective: Guess: take the orange Phantom tractor. Keep its aviation-fuel tanker attached. |
| 2 | Guess | **Canyon run** — Follow the current objective; detailed rule is defined by this stage’s objective type. |
| 3 | Guess | **Airfield reserves** — DeliverVehicleObjective: Guess: deliver the tractor AND attached tanker to McKenzie's yellow fuel bay. |
| 4 | Guess | **Unload safely** — Follow the current objective; detailed rule is defined by this stage’s objective type. |
| 5 | Guess | **Delivery received** — ConditionObjective: Confirm the fuel delivery at the receiving equipment. |

Final gameplay dialogue drains before the pass/aftermath transition.

Survey references: SM06.Approach, SM06.Bend, SM06.Delivery, SM06.Truck.

## Boundaries of this audit

The harness exercises objective progression, role ownership, fail/retry cleanup, scene release and persistence with GTA stand-ins. It does not establish road driveability, roof access, helicopter aim, swimming/streaming, trailer physics or camera framing. New desert positions remain estimates; safe-ground/water checks reject unavailable sites instead of deploying in the sky. Use the survey when you return. M44–M70 and SM07–SM09 are detailed in CAMPAIGN-REMAINDER.md and remain unimplemented gameplay.

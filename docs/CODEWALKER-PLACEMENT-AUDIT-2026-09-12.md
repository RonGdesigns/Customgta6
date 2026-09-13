# CodeWalker placement audit - September 12, 2026

This audit actually ran CodeWalker against the installed GTA V Enhanced archives. It did not use district centers as a substitute for map geometry. No mission coordinates or personal survey files were modified. M01-M07 and every saved survey/override, including M30, were protected.

## Scope and evidence

- CodeWalker source commit: `485d56bec00262ed7fa472261cce7bbc6202b96e`. Its library was built locally with the existing Roslyn compiler; no SDK installation was needed.
- Installed map: 5,407 archives, 10,663 indexed collision bounds, 67,454 vehicle-path nodes; selected DLC `mp2026_01_g9ec`.
- Checked **232 placement references**: 219 authored keys plus 13 optional editor previews. Checked **212 nearby/height candidates** separately.
- Every recorded top/below query completed. Both final runs reported zero asset-reader errors. The first exploratory pass lacked a shader-conversion support file; that pass was discarded, the dependency supplied, and the full audit rerun.
- Results: 76 without a basic support flag, 102 needing review, 41 special interior/air/underground references, 13 optional previews. A supported point is not a complete mission pass.
- Effective positions combine repository defaults with the installed saved survey and non-retired overrides. The checked source includes the pending Foundry update. It is not a capture of a running mission after its scripts reposition actors.

## Your saved placements

**Keep M07.** GarageRoof and MastTop stand about 1m above real rooftop collision at Z 95.198. IceStart stands about 1m above its platform at Z 320.292. LandingZone has a supporting surface at Z 26.631. A higher structure above IceStart does not make his platform wrong.

The M04 breaker and lookout, M05 generator crew/start, M06 sally port, and both M30 saved points have consistent support. No blanket changes to those are justified by this audit.

| Saved point | Finding | Recommendation |
| --- | --- | --- |
| M05.DinghySpawn | Static seabed about -0.964; roughly 0.96m depth at the static sea surface | Review a point 10m east: X 2673.20, Y -1161.70. Candidate seabed -1.895 gives about 1.90m depth. Preserve heading and let runtime resolve boat height. |
| M05.GrottoMouth | Static seabed about -0.871; roughly 0.87m depth | Review a point 20m east: X 2556.40, Y -1258.14. Candidate seabed -2.043 gives about 2.04m depth. Preserve story approach and boat facing. |
| M05.CliffPerch | Center supports the current position, with sloped/uneven ground nearby | Keep the center; verify room for movement and any nearby enemy formation. |
| M06.Feeder | Center height matches support; adjacent one-meter samples vary about 2.5m | Keep the center unless interaction access is obstructed; inspect the edge/nearby geometry. |
| M07.IceStart | Valid platform, but neighboring samples include a height change | Keep the saved position. Check step-off direction rather than flattening the rooftop. |
| Stash.CrewVan | Saved/reference Z 30.5 has no hit below at its XY; upper collision at 43.70 | Review separately as a parking location. Do not move it to the roof automatically. This save entry may come from a prior shipped/default capture, not a manual edit. |

## Untouched mission placements that need tightening

| Priority | Mission | Evidence | Required change |
| --- | --- | --- | --- |
| High | M09 | Ambush at (480,2760,42) is 79.1m from nearest road node; terrain is about Z46.34. Road candidate (487,2681.25,43.06) has level support. | Reposition the convoy stop together with Ice and pickup blocking. Ice's current fixed -35m Y offset intersects bad geometry at the road candidate; do not apply the vehicle candidate alone. |
| High | M11 | Both shop points use Z30 while actual terrain is about Z93.4-93.5. | Restage at a suitable shop or explicitly adapt the setting. Raising the pair to about Z94.4-94.5 fixes support only, not the presence of a garage/workshop. |
| High | M25 | Proposed bridge deck Z155.8 is about 139.6m above the collision at that XY. None of the checked upper collision there is the intended high bridge. | Locate the actual intended bridge and move deck, approach, rim, tanker and water extraction together. Lowering all points would remove the bridge/jump scene. |
| High | SM04 | Four quarry references are roughly 87-94m above the loaded surfaces. | Re-establish the quarry overlook and sniper perches as a coherent set; inspect sightlines after geometry placement. |
| High | M13 | Water launch/barges include dry land or no water quad; the supposed land slipway is over submerged ground. | Place the launch, barges and vehicle extraction around the actual basin shoreline as a group. |
| High | M22 / M24 | Alamo water/dredge points hit dry collision around Z39.71; shoreline points also have large errors. | Relocate water and beach/crane approach together. Runtime water search can move estimates, but cannot prove the resulting route or staging. |
| Medium | M08 | Second crate anchor intersects stacked/uneven geometry. | Select a flat loading patch with clearance for the crate and forklift. Nearby grid candidates were generated. |
| Medium | SM02 | Server/terminal positions float about 7.2m, but approach/exit have different overhead geometry. | Coordinate the whole route. The server/terminal height candidates alone do not establish roof access. |
| Medium | M28 | Pickup floats about 7.8m; cover point intersects steep, highly uneven geometry. | Pickup has a supported height candidate around Z63.16. Reposition cover separately; do not force a steep rock into a cover anchor. |
| Medium | M30 | Unedited Start floats about 25.7m, 85.5m from nearest road node. Saved Bend/Exit are supported. | Review Start candidate (-432.5,3946.25,67.63), then check route continuity to the existing Bend. Leave Bend and Exit. |

## What the automated pass does and does not establish

The reader casts down near the authored height and from above the map, checks four adjacent samples, inspects normals, records collision sources, reads static water quads and finds vehicle-path nodes. A normal player origin is approximately a meter above ground; that offset is not a floating-placement defect. A zero-distance ray hit may mean the probe starts inside a solid, not that its reported position is a walkable floor.

The latest DLC map state can include interiors/props not active in a particular Story Mode scene. Interior shells need their IPLs/entity sets loaded. A top-down hit on a roof or dock does not by itself prove that water underneath is absent. Static water depth does not model waves or hull clearance. Scripted platforms, actors, vehicles, ground snapping, road routing and variable editor groups can change the final placement.

The 13 optional editor positions are previews only until saved. Their default geometry flags are not proof of a broken live mission spawn, because those missions retain dynamic spawn logic.

The audit intentionally reports a correction candidate instead of blindly moving linked mission positions. Your saved INI hash and the repository locations hash were rechecked at the end and remained unchanged.

## Files

Raw extracted results and candidates are retained locally under `build/codewalker-audit-2026-09-12/`; they are not part of the mod package. `classified-placements.tsv` contains every point, source/protection, supporting surface, nearest road reference and finding. `collision-results.tsv` retains unclassified ray evidence. `candidate-results.tsv` contains the alternative probes.

[CodeWalker source](https://github.com/dexyfex/CodeWalker) supplies the map reader. All game geometry used for this run came from the local licensed installation.

## Complete point register

| Point | Source / protection | Assessment | Finding |
| --- | --- | --- | --- |
| M01.IceApproach | authored / protected | review | Collision above origin / probe intersects solid; inspect clearance; Adjacent surface height varies >1m |
| M01.CraneNest | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M01.YachtDeck | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M01.ServiceTerminal | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M01.WarehouseBay | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M01.PrototypeCar | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M01.CapoSpawn | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M01.LaunchEscape | authored / protected | review | No visible static water quad at XY |
| M01.RegroupPoint | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M01.ExitPoint | authored / protected | review | Adjacent surface height varies >1m |
| M01.GuessApproach | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M01.GohanApproach | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| SM01.WarehouseGate | authored | review | Adjacent surface height varies >1m |
| SM01.SergeiOffice | authored | review | Collision above origin / probe intersects solid; inspect clearance |
| SM01.CrateLoad | authored | review | Collision above origin / probe intersects solid; inspect clearance |
| M02.InterceptStart | authored / protected | review | Collision above origin / probe intersects solid; inspect clearance; Adjacent surface height varies >1m |
| M02.CanalEscape | authored / protected | review | No support found below authored height; upper surface Z 29.69 |
| M03.RailJunction | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M03.DepotGate | authored / protected | review | No support found below authored height; upper surface Z 42.26 |
| M03.CraneControls | authored / protected | review | No support found below authored height; upper surface Z 42.95 |
| M03.HaulerSpawn | authored / protected | review | Adjacent surface height varies >1m |
| M04.GarageEntry | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M04.Breaker | saved survey / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M04.RampGuards | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M04.ChaseCar | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M04.TextileCrash | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M05.CliffPerch | saved survey / protected | review | Adjacent surface height varies >1m |
| M05.CoveAir | authored / protected | review | Top collision is dry / shallower than 1.2m; inspect water column |
| M05.GrottoMouth | saved survey / protected | review | Top collision is dry / shallower than 1.2m; inspect water column |
| M05.Sandbar | authored / protected | review | Top collision is dry / shallower than 1.2m; inspect water column |
| M05.DinghySpawn | saved survey / protected | review | Top collision is dry / shallower than 1.2m; inspect water column |
| M06.Culvert | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M06.Feeder | saved survey / protected | review | Adjacent surface height varies >1m |
| M06.SallyPort | saved survey / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M06.ServerRacks | authored / protected | review | Collision above origin / probe intersects solid; inspect clearance |
| M06.AlleyHold | authored / protected | review | Collision above origin / probe intersects solid; inspect clearance |
| M06.GrangerSpawn | authored / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M07.GarageRoof | saved survey / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M07.MastTop | saved survey / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M07.LandingZone | saved survey / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M08.WarehouseGate | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M08.CameraRoom | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M08.CratePadOne | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M08.CratePadTwo | authored | review | Collision above origin / probe intersects solid; inspect clearance; Adjacent surface height varies >1m |
| M08.HaulerSpawn | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M08.Connector | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| SM02.RoofAccess | authored | review | 18.5m above support; Steep or downward-facing hit |
| SM02.ServerBay | authored | review | 7.2m above support |
| SM02.Terminal | authored | review | 7.3m above support |
| SM02.Exit | authored | review | 11.5m above support; Steep or downward-facing hit |
| SM03.StartLine | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| SM03.Checkpoint1 | authored | review | 14.9m above support |
| SM03.Checkpoint2 | authored | review | 16.7m above support; Steep or downward-facing hit; Adjacent surface height varies >1m |
| SM03.Checkpoint3 | authored | review | 15.6m above support; Adjacent surface height varies >1m |
| SM03.Checkpoint4 | authored | review | Collision above origin / probe intersects solid; inspect clearance; Adjacent surface height varies >1m |
| Base.CypressFlats | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| Stash.CrewVan | saved survey / protected | review | No support found below authored height; upper surface Z 43.70 |
| M09.HeliSpawn | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M09.ConvoyStart | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M09.AmbushPoint | authored | review | No support found below authored height; upper surface Z 46.34 |
| M09.Bunker | authored | review | Collision above origin / probe intersects solid; inspect clearance; Adjacent surface height varies >1m |
| M09.Pickup | authored | review | Collision above origin / probe intersects solid; inspect clearance |
| M10.ConvoyStart | authored | review | Collision above origin / probe intersects solid; inspect clearance; Adjacent surface height varies >1m |
| M10.BridgePoint | authored | review | 8.3m above support |
| M10.TunnelMouth | authored | review | 2.3m above support |
| M11.ChopShop | authored | review | No support found below authored height; upper surface Z 93.50 |
| M11.DynoPad | authored | review | No support found below authored height; upper surface Z 93.42 |
| M12.SouthJetty | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M12.FreighterHull | authored | review | No visible static water quad at XY |
| M12.SonarBuoy | authored | review | No visible static water quad at XY |
| M12.PierWatch | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M13.KayakLaunch | authored | review | No visible static water quad at XY |
| M13.BargeOne | authored | review | No visible static water quad at XY |
| M13.BargeTwo | authored | review | Top collision is dry / shallower than 1.2m; inspect water column |
| M13.BargeThree | authored | review | Top collision is dry / shallower than 1.2m; inspect water column |
| M13.CanalSlipway | authored | review | 11.0m above support |
| M14.OverwatchRidge | authored | review | 5.4m above support |
| M14.HangarDoor | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M14.PlaneSpawn | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M14.McKenzieHangar | authored | review | Adjacent surface height varies >1m |
| M15.AdminEntry | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M15.MaintenanceVault | authored | review | Steep or downward-facing hit |
| M15.FiberSplice | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M15.Exit | authored | review | Collision above origin / probe intersects solid; inspect clearance; Adjacent surface height varies >1m |
| M16.DepotFence | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M16.Helipad | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M16.CargobobSpawn | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M16.CanyonRun | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| M16.TerminalDrop | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M17.DrySlip | authored | review | Collision above origin / probe intersects solid; inspect clearance; Adjacent surface height varies >1m |
| M17.WeldOne | authored | review | Collision above origin / probe intersects solid; inspect clearance |
| M17.WeldTwo | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M17.WeldThree | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M18.ChannelMark | authored | review | No visible static water quad at XY |
| M18.SaltHangar | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M18.HaulerMark | authored | review | Adjacent surface height varies >1m |
| M19.DiveStart | authored | review | No visible static water quad at XY |
| M19.HullBreach | authored | review | No visible static water quad at XY |
| M19.ClampOne | authored | review | No visible static water quad at XY |
| M19.ClampTwo | authored | review | No visible static water quad at XY |
| M19.Surface | authored | review | No visible static water quad at XY |
| M20.HoverPoint | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| M20.DeckGunners | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M20.ClimbOut | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| M21.LaunchSpawn | authored | review | No visible static water quad at XY |
| M21.Breakwater | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M21.RidgeCross | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| M21.ShoreLanding | authored | review | No visible static water quad at XY |
| M21.RoadPickup | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M22.AlamoDrop | authored | review | Top collision is dry / shallower than 1.2m; inspect water column |
| M22.Beach | authored | review | No support found below authored height; upper surface Z 44.85 |
| M22.RoadArrival | authored | review | No support found below authored height; upper surface Z 55.31 |
| M23.DomeApproach | authored | review | Collision above origin / probe intersects solid; inspect clearance |
| M23.BunkerDoor | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M23.BayOne | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M23.BayTwo | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M23.BayThree | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M23.Generator | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M23.SecondExit | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M24.CraneSpawn | authored | review | No support found below authored height; upper surface Z 35.63 |
| M24.DredgePoint | authored | review | Top collision is dry / shallower than 1.2m; inspect water column |
| M24.RidgeLine | authored | review | 10.6m above support |
| M24.RidgeRoad | authored | review | 25.0m above support |
| M25.BridgeDeck | authored | review | 139.6m above support |
| M25.DeckApproach | authored | review | 126.1m above support; Steep or downward-facing hit |
| M25.RimPost | authored | review | 105.0m above support |
| M25.TankerSpot | authored | review | 137.2m above support |
| M25.Riverbed | authored | review | No visible static water quad at XY |
| M26.DusterPad | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M26.PatrolBox | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| M27.FormUp | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| M27.JetTrack | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| M27.SeaPickup | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| Home.Ice | authored | review | No support found below authored height; upper surface Z 11.38 |
| Home.Gohan | authored | review | No support found below authored height; upper surface Z 29.35 |
| Home.Guess | authored | review | Adjacent surface height varies >1m |
| Apartment.Starter.Ice | authored | review | Adjacent surface height varies >1m |
| Apartment.Starter.Gohan | authored | review | Adjacent surface height varies >1m |
| Apartment.Starter.Guess | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| Apartment.Luxury.Entrance | saved survey / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| Apartment.Starter.Interior.Ice | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Starter.Interior.Gohan | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Starter.Interior.Guess | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Top.Entrance | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| Apartment.Top.Interior | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.Ice.Door | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.Ice.Message | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.Ice.Wardrobe | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.Ice.Bed | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.Ice.Locker | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.Gohan.Door | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.Gohan.Message | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.Gohan.Wardrobe | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.Gohan.Bed | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.Gohan.Locker | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.Guess.Door | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.Guess.Message | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.Guess.Wardrobe | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.Guess.Bed | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.Guess.Locker | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Luxury.Ice | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Luxury.Gohan | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Luxury.Guess | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| M28.Approach | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M28.Relay | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M28.Cover | authored | review | Collision above origin / probe intersects solid; inspect clearance; Steep or downward-facing hit; Adjacent surface height varies >1m |
| M28.Pickup | authored | review | 7.8m above support |
| M28.Response | authored | review | 7.3m above support |
| M29.Approach | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M29.Cover | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M29.Valve | authored | review | No support found below authored height; upper surface Z 47.69 |
| M29.Truck | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M29.Delivery | authored | review | Collision above origin / probe intersects solid; inspect clearance |
| M30.Start | authored | review | 25.7m above support; Steep or downward-facing hit; Adjacent surface height varies >1m |
| M30.Bend | saved survey / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M30.Exit | saved survey / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| SM04.Approach | authored | review | 89.5m above support |
| SM04.Overlook | authored | review | 89.2m above support |
| SM04.NestOne | authored | review | 87.4m above support |
| SM04.NestTwo | authored | review | 93.6m above support |
| SM05.Shore | authored | review | 2.1m above support |
| SM05.Boat | authored | review | Top collision is dry / shallower than 1.2m; inspect water column |
| SM05.Buoy | authored | review | Top collision is dry / shallower than 1.2m; inspect water column |
| SM06.Approach | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| SM06.Truck | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| Prologue.LSIACurb | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| Prologue.LSIACar | saved survey / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| SM06.Bend | authored | review | No support found below authored height; upper surface Z 21.41 |
| SM06.Delivery | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M19.Worksite | authored | review | No visible static water quad at XY |
| M19.BullionSurface | authored | review | No visible static water quad at XY |
| M04.IceWatch | saved survey / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| Base.CypressFlatsCar | saved survey / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M05.LightCrew | saved survey / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M05.Start | saved survey / protected | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| M07.IceStart | saved survey / protected | review | Adjacent surface height varies >1m |
| Apartment.MissionRow.Interior | saved survey / protected | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.MissionRow.Door | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.MissionRow.Message | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.MissionRow.Wardrobe | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.MissionRow.Bed | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Apartment.Room.MissionRow.Locker | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Garage.Eclipse | authored | review | Adjacent surface height varies >1m |
| Garage.Diamond | authored | review | No support found below authored height; upper surface Z 111.55 |
| Garage.MissionRow | authored | review | No support found below authored height; upper surface Z 51.58 |
| Garage.LittleSeoul | authored | review | 3.6m above support |
| Garage.Richards | authored | review | 2.5m above support; Adjacent surface height varies >1m |
| Garage.PopularStreet | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| Garage.Rancho | authored | review | Adjacent surface height varies >1m |
| Garage.DelPerro | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| Garage.Vinewood | authored | review | No support found below authored height; upper surface Z 110.33 |
| Garage.Pillbox | authored | review | Collision above origin / probe intersects solid; inspect clearance |
| Hideout.Foundry.Entrance | authored | supported | Supporting geometry consistent with origin height; gameplay reachability not established |
| Hideout.Foundry.Interior | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Hideout.Foundry.Room.Door | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Hideout.Foundry.Room.Planning | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Hideout.Foundry.Room.Wardrobe | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Hideout.Foundry.Room.Bed | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| Hideout.Foundry.Room.Locker | authored | special | Special interior/air/underground placement: requires loaded-scene review |
| M03.ArrivalSpawn1 | optional editor preview / protected | optional preview | No support found below authored height; upper surface Z 42.75 |
| M03.ArrivalStop1 | optional editor preview / protected | optional preview | No support found below authored height; upper surface Z 42.26 |
| M03.Crate1 | optional editor preview / protected | optional preview | No support found below authored height; upper surface Z 42.26 |
| M03.ArrivalSpawn2 | optional editor preview / protected | optional preview | No support found below authored height; upper surface Z 43.13 |
| M03.ArrivalStop2 | optional editor preview / protected | optional preview | No support found below authored height; upper surface Z 42.26 |
| M03.Crate2 | optional editor preview / protected | optional preview | No support found below authored height; upper surface Z 42.26 |
| M03.ArrivalSpawn3 | optional editor preview / protected | optional preview | No support found below authored height; upper surface Z 43.31 |
| M03.ArrivalStop3 | optional editor preview / protected | optional preview | No support found below authored height; upper surface Z 42.27 |
| M03.Crate3 | optional editor preview / protected | optional preview | No support found below authored height; upper surface Z 42.26 |
| M06.ConvoySpawn1 | optional editor preview / protected | optional preview | No support found below authored height; upper surface Z 6.59 |
| M06.ConvoyStop1 | optional editor preview / protected | optional preview | No support found below authored height; upper surface Z 10.17; Adjacent surface height varies >1m |
| M06.ConvoySpawn2 | optional editor preview / protected | optional preview | Supporting geometry consistent with origin height; gameplay reachability not established |
| M06.ConvoyStop2 | optional editor preview / protected | optional preview | Collision above origin / probe intersects solid; inspect clearance; Steep or downward-facing hit |

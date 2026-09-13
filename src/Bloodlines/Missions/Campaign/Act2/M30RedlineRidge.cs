using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;
namespace Bloodlines.Missions.Campaign
{
    public sealed class M30RedlineRidge : DesertOperation
    {
        public override string Id => "M30";public override string Title=>"Redline Ridge";
        private Vehicle _truck,_heli;private Ped _pilot;
        private Prop _parts, _bench, _receiver;
        private Blip _gunshipBlip;
        private bool _unloaded, _received;
        public Prop Parts => _parts;
        public bool Unloaded => _unloaded;
        public bool Received => _received;
        protected override MissionEndpoint Endpoint => MissionEndpoint.SafehouseArrival;
        protected override bool Setup()
        {
            if(!MissionSites.Prepare(Ctx.Locations,Id)||!Ctx.Crew.Deploy(CrewSlot.Guess,At("M30.Start"),0))return false;
            ProtectCrew();
            _truck=Car("dubsta3",At("M30.Start"),Ctx.Locations.Heading("M30.Start"));if(!RequireAssets(_truck))return false;
            _parts=WorkProp("prop_ld_case_01",_truck.Position,false);
            if(!RequireAssets(_parts))return false;
            // Carry a visible secured case on the roof, using both models' bounds.
            if(!StowPropStep.Stow(_parts,_truck,new Vector3(0,0,_truck.Model.Dimensions.Item2.Z-_parts.Model.Dimensions.Item1.Z+.01f)))return false;
            RequireAsset(_truck,"The satellite parts truck was destroyed.");
            RequireAsset(_parts,"The case of satellite parts was lost.");
            Station(CrewSlot.Ice,_truck,VehicleSeat.Passenger);Station(CrewSlot.Gohan,_truck,VehicleSeat.LeftRear);
            Ctx.Cutscenes.Play(new SceneSpec {MissionId=Id,Phase="approach",Title="Read the road first",
                Reason="The actual 6x6 carries the visible parts case. Show the starting road; explain the bend and canyon exit while the passengers remain seated.",
                Blocking=new SceneBlocking().Then(ShotStep.Low(2400,_truck,6,4,3))
                    .Then(ShotStep.Watching(1800,Ctx.Crew.PedFor(CrewSlot.Gohan),_parts))});
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Collect the satellite parts",new EnterVehicleObjective("Guess: take the Dubsta 6x6. Ice and Gohan ride with the parts.",()=>_truck,VehicleSeat.Driver,true)).OwnedBy(CrewSlot.Guess).OnExit(c=>ReleaseForPickup());
            yield return new MissionStage("Down the ridge",new DeliverVehicleObjective("Guess: drive the loaded 6x6 to the yellow canyon bend. Use the road, not the cliff face.",()=>_truck,()=>At("M30.Bend"),25),new ProtectObjective("",()=>_truck,"The truck and satellite parts were destroyed.")).OwnedBy(CrewSlot.Guess).OnEnter(c=>LaunchPursuit()).WithCues("M30_S1_01_GUESS").AfterCues("M30_S1_02_GOHAN");
            yield return new MissionStage("Sheltered approach",new DeliverVehicleObjective("Guess: follow the yellow road marker out of the canyon. Passengers cover the gunship.",()=>_truck,()=>At("M30.Exit"),30),new ProtectObjective("",()=>_truck,"The parts truck was lost.")).OwnedBy(CrewSlot.Guess).OnExit(c=>LoseGunship());
            yield return new MissionStage("Deliver the parts",new DeliverVehicleObjective("Guess: park the same 6x6 at the bunker unloading marker.",()=>_truck,()=>At("M29.Delivery"),25),new ProtectObjective("",()=>_truck,"The satellite parts never reached the bunker.")).OwnedBy(CrewSlot.Guess);
            yield return new MissionStage("Unload",new MissionInteraction("Guess: stop the 6x6 in the unloading area; transfer its roof case to the bunker workbench",()=>At("M29.Delivery"),5,30,()=>_truck,stopVehicle:true)).OwnedBy(CrewSlot.Guess).OnExit(c=>UnloadParts());
            yield return new MissionStage("Fit the receiver",new MissionInteraction("Gohan: get out and use the laptop beside the delivered parts on the marked bunker workbench",()=>At("M23.BayOne"),6,animation:MissionInteraction.ReachInside)).OwnedBy(CrewSlot.Gohan).OnExit(c=>ReceiveSignal());
            yield return new MissionStage("First reception",new ConditionObjective("Listen to the receiver check. The parts must be fitted before the job is complete.",()=>_received&&!Ctx.Cutscenes.IsActive)).OwnedBy(CrewSlot.Gohan).AfterCues("M30_S1_03_GUESS");
        }
        private void UnloadParts()
        {
            if(!Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY,_parts,_truck))throw new System.InvalidOperationException("The truck arrived without its satellite parts case.");
            // Opposite the M23 tool bench, with the shared work marker between them.
            _bench=WorkProp("prop_table_03",At("M23.BayOne")+new Vector3(0,-1.5f,0),reuse:true);
            if(!RequireAssets(_bench))throw new System.InvalidOperationException("The receiving workbench failed to load.");
            _receiver=WorkProp("prop_laptop_01a",PropPlacement.OnTop(_bench,_bench.Model,new Model("prop_laptop_01a"),0,-.35f),false,true);
            if(!RequireAssets(_receiver))throw new System.InvalidOperationException("The receiver controls failed to load.");
            RequireAsset(_receiver,"The receiver controls were destroyed.");
            RequiredScene("unload","From truck to workbench","The same parts case is lifted off the 6x6 onto the bunker worktable. Passengers stay in their existing seats until gameplay asks Gohan to get out.",
                new SceneBlocking().Then(new TransferPropStep(_parts,_bench,new Vector3(.35f,0,_bench.Model.Dimensions.Item2.Z-_parts.Model.Dimensions.Item1.Z+.01f),1500,_truck))
                    .Then(new VerifySceneStep("Parts on workbench",()=>Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY,_parts,_bench),()=>_unloaded=true))
                    .Then(ShotStep.Low(2000,_bench,4,2,2)));
        }
        private void ReceiveSignal()
        {
            if(!_unloaded)throw new System.InvalidOperationException("No delivered parts are available to fit.");
            RequiredScene("reception","A picture, not a plan","Gohan tests the delivered receiver at its actual workbench. Offshore traffic can now be observed, but this is not access codes or a complete attack plan.",
                new SceneBlocking().Then(new InspectStep(Ctx.Crew.PedFor(CrewSlot.Gohan),_receiver.Position,1600))
                    .Then(ShotStep.Low(1800,_receiver,2,1,1))
                    .Then(new VerifySceneStep("Receiver fitted",()=>_unloaded&&_receiver.Exists()&&Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY,_parts,_bench),()=>_received=true)));
        }
        private void LaunchPursuit()
        {
            _heli=Car("buzzard",_truck.Position-_truck.ForwardVector*180f+new Vector3(0,0,95f),0,false);_pilot=Guard(At("M30.Start"));
            if(!RequireAssets(_heli,_pilot))throw new System.InvalidOperationException("The pursuit helicopter failed to load.");
            _pilot.SetIntoVehicle(_heli,VehicleSeat.Driver);_heli.IsEngineRunning=true;
            Function.Call(Hash.SET_HELI_BLADES_FULL_SPEED,_heli);
            _gunshipBlip=Track(_heli.AddBlip());
            if(_gunshipBlip!=null){_gunshipBlip.Color=BlipColor.Red;_gunshipBlip.Name="Ridge gunship";}
            _pilot.Task.StartHeliMission(_heli,Game.Player.Character,VehicleMissionType.Attack,35,35,60,25,0,20,HeliMissionFlags.None);
            Radio("GOHAN","Gunship behind the truck. Follow the bend, then the canyon exit. We cover you from our seats; keep your eyes on the road.","M30_GUNSHIP");
        }
        private void LoseGunship()
        {
            if(_gunshipBlip!=null&&_gunshipBlip.Exists())_gunshipBlip.Delete();
            if(_heli!=null&&_heli.Exists()&&!_heli.IsDead&&_pilot!=null&&_pilot.Exists()&&!_pilot.IsDead)
            {
                _pilot.Task.ClearAll();
                var away=_truck.Position+new Vector3(0,-1500,200);
                _pilot.Task.StartHeliMission(_heli,away,VehicleMissionType.GoTo,45f,35f,(int)away.Z,80,-1f,70f,(HeliMissionFlags)(256|4096));
            }
            Radio("ICE","Gunship has broken off over the ridge. Keep going; we still need that case on Gohan's workbench.","M30_CLEAR");
        }
        protected override void OnUpdate()
        {
            if(Ctx.Cutscenes.IsActive)return;
            if(!_unloaded&&_parts!=null&&!Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY,_parts,_truck))
            {Fail("The secured satellite parts came off the truck. Restart the delivery.");return;}
            base.OnUpdate();
        }
        protected override void OnPassed()
        {
            if(!_received||!_unloaded)throw new System.InvalidOperationException("The satellite receiver has not been fitted and checked.");
            Ctx.State?.SetCargo("satelliteParts","M23.BayOne");
            foreach(var prop in new[]{_parts,_receiver,_bench})if(prop!=null&&prop.Exists())Release(prop);
        }
    }
}

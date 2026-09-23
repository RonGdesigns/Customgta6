using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
namespace Bloodlines.Missions.Campaign
{
    public sealed class M35TheChianskiAmbush : PreparationOperation
    {
        public override string Id => "M35";public override string Title => "The Chianski Ambush";
        protected override MissionEndpoint Endpoint=>MissionEndpoint.SecuredDelivery;
        private Vehicle _lead,_technical;private Ped _leadDriver,_technicalDriver;
        private readonly List<Prop> _charges=new List<Prop>();private Prop _table,_device;
        private bool _convoyStarted,_trapped,_delivered;private int _convoyAt;
        /// <summary>
        /// When each convoy driver was last given his route, and whether he has moved since.
        /// The route used to be handed over again every three seconds, which restarts the
        /// drive task each time; a driver re-planning on a clock is a good way to stall the
        /// lead car short of the trap until the 150-second limit fails the ambush. Each
        /// driver is ordered once and only re-ordered when he has genuinely stopped short
        /// (the September 22 audit).
        /// </summary>
        private int _leadOrderAt,_technicalOrderAt,_leadStillSince=-1,_technicalStillSince=-1;
        /// <summary>How long a convoy vehicle may sit still short of its mark before its order is treated as lost.</summary>
        public const int ConvoyStallMs = 4000;
        public Vehicle Technical=>_technical;public bool ConvoyStarted=>_convoyStarted;public bool Trapped=>_trapped;
        /// <summary>The hostile still riding the bed gun, if he is alive on it.</summary>
        public Ped TruckGunner=>Opposition.FirstOrDefault(p=>MountedGunner(p)&&!p.IsDead);
        /// <summary>Whether the crew are expected to be fighting, which is what lets Ice use the gun.</summary>
        public bool CrewFighting=>Fighting;
        protected override bool Setup()
        {
            if(!BeginCrew(CrewSlot.Ice))return false;
            CrewCar=CrewTransport("M35.CrewCar");if(!RequireAssets(CrewCar))return false;
            Station(CrewSlot.Guess,CrewCar,VehicleSeat.Driver);Roles.For(CrewSlot.Guess).Stop();
            _table=Equipment("prop_table_03","M35.Table");
            _device=WorkProp("prop_laptop_01a",PropPlacement.OnTop(_table,_table.Model,new Model("prop_laptop_01a")),false);
            if(!RequireAssets(_device))return false;
            Establish("approach","Take the gun truck alive","Ice prepares the marked roadside charges, Guess parks across the far exit, and Gohan identifies the gun truck on the field laptop. The convoy waits until setup is complete; the lead escort is the blast target, not the technical.",_device,CrewCar);
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Prepare the trap",new MultiHoldObjective("Ice: plant both marked roadside charges; press E / D-pad Right",Enumerable.Range(1,2).Select(i=>At("M35.ChargeWork"+i)),4,3f,"Planting charge"){Animation=MissionInteraction.ReachInside,SiteDone=i=>_charges.Add(Equipment("prop_ld_bomb_01","M35.Charge"+(i+1)))}).OwnedBy(CrewSlot.Ice).OnExit(c=>Roles.For(CrewSlot.Ice).Approach(At("M35.IceCover"),At("M35.IceCover")));
            yield return new MissionStage("Close the far exit",new TravelObjective("Guess: park the crew car across the road at the south end of the pass and stop. That closes the convoy's only way out and keeps you beside the gun truck",()=>At("M35.BlockExit"),8,()=>CrewCar)).OwnedBy(CrewSlot.Guess);
            yield return new MissionStage("Identify the target",new MissionInteraction("Gohan: use the laptop on the marked field table to identify the convoy's gun truck",()=>At("M35.DeviceWork"),4,3f,animation:MissionInteraction.ReachInside,face:()=>_device.Position)).OwnedBy(CrewSlot.Gohan).OnExit(c=>StartConvoy());
            // Both of these are the whole crew's, not Gohan's. Unowned, they inherited him
            // from the laptop stage, which told Ice in cover to switch away and, because a
            // kill objective only completes while its owner is in play, would not let the
            // shooter finish the capture (the September 22 audit).
            yield return new MissionStage("Watch the pass",new ConditionObjective("Wait in cover for the red lead escort to enter the yellow trap. The orange gun truck must stay intact.",()=>_trapped)).AnyBrother();
            yield return new MissionStage("Capture the technical",new KillTargetsObjective("Stop the escort and gun-truck crew. Shoot the occupants, not the orange technical.",()=>Opposition),new ProtectObjective("",()=>_technical,"The technical was destroyed before capture."))
                .AnyBrother()
                .WithCues("M35_S1_01_ICE");
            yield return new MissionStage("Take the driver seat",new EnterVehicleObjective("Guess: take the captured technical's driver seat",()=>_technical,VehicleSeat.Driver)).OwnedBy(CrewSlot.Guess).AfterCues("M35_S1_02_GUESS");
            yield return new MissionStage("Bring both brothers",new ConditionObjective("Stop the technical: Gohan boards the front passenger seat and Ice takes the rear gun seat",()=>BoardTeam())).OwnedBy(CrewSlot.Guess)
                .OnExit(c=>
                {
                    // Fighting stays on through the run home. It is what lets Ice work the
                    // mounted gun from the bed and Gohan return fire from the cab; with it
                    // off, Ron reported Ice sitting there while the police shot at them.
                    Fighting=true;
                    DrivingDestination=()=>At("M35.Senora.Delivery");
                    ResponseCar("M35.Response",_technical.Position);
                    // Either brother is playable for the run: Guess keeps driving on his own
                    // while the player rides the gun, and the player can take the wheel back.
                    Ctx.Switching.SetUnlocked();
                    Radio("GUESS","I have the wheel all the way to the bunker. Take Ice on the gun or Gohan in the cab if you would rather shoot than drive.","M35_RUNHOME");
                });
            yield return new MissionStage("Lose pursuit",new LoseWantedObjective("Lose the pursuit. Switch to Ice for the mounted gun or Gohan in the cab while Guess drives; the wheel is yours whenever you want it"));
            yield return new MissionStage("Deliver the gun truck",new TravelObjective("Take the same technical with both brothers to the bunker vehicle bay and stop. Guess drives if you are someone else",()=>At("M35.Senora.Delivery"),15,()=>_technical));
            yield return new MissionStage("Inspect the capture",new MissionInteraction("Gohan: get out and inspect the gun mount at the rear of the parked technical",()=>_technical.Position-_technical.ForwardVector*2f,5,4f,animation:MissionInteraction.ReachInside)).OwnedBy(CrewSlot.Gohan)
                .OnEnter(c=>DrivingDestination=null).OnExit(c=>{_delivered=true;Establish("delivery","Protection for the way home","The captured technical arrives intact. Gohan checks the real gun mount. It provides machine-gun cover against exposed targets and low aircraft; it is not a missile launcher or an automatic air-defense system.",_technical);}).AfterCues("M35_S1_03_GOHAN");
        }
        private void StartConvoy()
        {
            if(_charges.Count!=2)throw new InvalidOperationException("The pass has not been prepared.");
            _lead=Car("mesa",At("M35.LeadSpawn"),Ctx.Locations.Heading("M35.LeadSpawn"),false);
            _technical=Car("technical",At("M35.TechnicalSpawn"),Ctx.Locations.Heading("M35.TechnicalSpawn"));
            // These four ride; they never stand. Asking the guard spawner for standing
            // space at two shared points is what failed this mission in Ron's run.
            if(!RequireAssets(_lead,_technical))throw new InvalidOperationException("The pass convoy vehicles failed to load.");
            _leadDriver=Occupant(_lead,VehicleSeat.Driver);var escort=Occupant(_lead,VehicleSeat.Passenger);
            _technicalDriver=Occupant(_technical,VehicleSeat.Driver);var gunner=Occupant(_technical,VehicleSeat.LeftRear);
            if(!RequireAssets(_leadDriver,_technicalDriver,gunner,escort))throw new InvalidOperationException("The pass convoy crew could not be seated.");
            Opposition.AddRange(new[]{_leadDriver,_technicalDriver,gunner,escort});
            RequireAsset(_technical,"The required technical was destroyed. Restart the ambush.");
            var blip=Track(_lead.AddBlip());blip.Color=BlipColor.Red;blip.Name="Lead escort - trap this vehicle";
            _lead.IsEngineRunning=true;_technical.IsEngineRunning=true;_convoyAt=Game.GameTime;_convoyStarted=true;
            _leadOrderAt=_technicalOrderAt=0;_leadStillSince=_technicalStillSince=-1;
            Radio("GOHAN","Lead escort first, gun truck behind it. Wait for the lead vehicle in the trap. Guess has the far exit; keep the technical out of the blast.","M35_IDENTIFIED");
        }
        private void Trap()
        {
            if(_trapped)return;
            var point=At("M35.Trap");
            if(Protagonist.All.Any(h=>Ctx.Crew.PedFor(h.Slot).Position.DistanceTo(point)<25f)||_technical.Position.DistanceTo(point)<35f)
            {GameUtils.Subtitle("Clear the trap area. The crew and technical must be outside the blast radius.",2500);return;}
            _trapped=true;_lead.EngineHealth=0f;_lead.IsEngineRunning=false;
            World.AddExplosion(point,ExplosionType.Grenade,1f,.4f,Game.Player.Character,true,false);
            _leadDriver.Task.LeaveVehicle();_technicalDriver.Task.ClearAll();_technical.IsEngineRunning=false;
            // The escort's vehicle is dead, so its crew gets out. The gun truck does not:
            // it has a mounted gun in the bed and a man on it, and standing him in the
            // road instead of letting him use it was the reason nothing shot back.
            foreach(var enemy in Opposition)
            {
                if(enemy==null||!enemy.Exists())continue;
                if(MountedGunner(enemy))continue;
                enemy.Task.LeaveVehicle();
            }
            Fighting=true;
        }
        /// <summary>How often the mounted gunner's order is refreshed while he is firing.</summary>
        public const int GunnerOrderMs = 4000;
        private int _gunnerOrder;

        /// <summary>
        /// A hostile riding the gun truck's bed gun rather than a seat he should abandon.
        /// The technical's rear position is the mount, which is why he stays in it.
        /// </summary>
        private bool MountedGunner(Ped enemy) =>
            _technical != null && _technical.Exists() && enemy != null && enemy.Exists() &&
            enemy.IsInVehicle(_technical) && enemy.SeatIndex == VehicleSeat.LeftRear;

        /// <summary>
        /// Put the gun in the bed to work. Issued on a cooldown rather than every frame,
        /// for the same reason a guard's combat order is: re-tasking restarts it.
        /// </summary>
        private void WorkTheMountedGun()
        {
            if(!Fighting||_trapped==false)return;
            if(Game.GameTime<_gunnerOrder)return;
            var gunner=Opposition.FirstOrDefault(MountedGunner);
            if(gunner==null||gunner.IsDead)return;
            var target=Protagonist.All.Select(h=>Ctx.Crew.PedFor(h.Slot))
                .Where(p=>p!=null&&p.Exists()&&!p.IsDead)
                .OrderBy(p=>p.Position.DistanceTo(gunner.Position)).FirstOrDefault();
            if(target==null)return;
            _gunnerOrder=Game.GameTime+GunnerOrderMs;
            gunner.Task.VehicleShootAtPed(target);
        }

        /// <summary>One convoy driver's route: given once, and again only after a real stall short of the mark.</summary>
        private void KeepConvoyMoving(Ped driver,Vehicle car,Vector3 mark,float radius,float speed,ref int orderedAt,ref int stillSince)
        {
            if(driver==null||!driver.Exists()||driver.IsDead||car==null||!car.Exists()||!driver.IsInVehicle(car))return;
            bool arrived=car.Position.DistanceTo(mark)<=radius+2f;
            if(car.Speed>1f||arrived)stillSince=-1;
            else if(stillSince<0)stillSince=Game.GameTime;
            bool stalled=orderedAt!=0&&stillSince>=0&&Game.GameTime-stillSince>ConvoyStallMs&&Game.GameTime-orderedAt>ConvoyStallMs;
            if(orderedAt!=0&&!stalled)return;
            driver.Task.DriveTo(car,mark,radius,speed,(DrivingStyle)CrewDriving.TrafficFlags);
            if(stalled)Logger.Info(Id+": a convoy driver had stopped short of his mark; reissued the route.");
            orderedAt=Game.GameTime;stillSince=-1;
        }
        private bool BoardTeam()
        {
            Roles.Release();Ctx.Crew.CompanionsHoldPosition=false;
            bool gohan=Board(Ctx.Crew.PedFor(CrewSlot.Gohan),_technical,VehicleSeat.Passenger);
            bool ice=Board(Ctx.Crew.PedFor(CrewSlot.Ice),_technical,VehicleSeat.LeftRear);
            return gohan&&ice;
        }
        protected override void OnUpdate()
        {
            WorkTheMountedGun();
            if(_convoyStarted&&!_trapped)
            {
                if(Game.GameTime-_convoyAt>150000){Fail("The convoy did not reach the prepared trap. Retry or survey its road approach.");return;}
                KeepConvoyMoving(_leadDriver,_lead,At("M35.Trap"),4f,17f,ref _leadOrderAt,ref _leadStillSince);
                KeepConvoyMoving(_technicalDriver,_technical,At("M35.TechnicalHold"),10f,15f,ref _technicalOrderAt,ref _technicalStillSince);
                // Not yet trapped is the stage: the watch opens the moment the convoy starts
                // and closes on the blast, so no stage number is needed.
                if(_lead.Position.DistanceTo(At("M35.Trap"))<8f)Trap();
            }
            base.OnUpdate();
        }
        protected override void OnPassed(){if(!_delivered)throw new InvalidOperationException("The captured technical has not been inspected.");Ctx.State?.SetCargo("antiAirTechnical","M35.Senora.Delivery");Release(_technical);}
    }
}

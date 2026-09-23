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
    public sealed class M31TheIronPerimeter : PreparationOperation
    {
        public override string Id => "M31";
        public override string Title => "The Iron Perimeter";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SafehouseArrival;
        private Prop _generator;
        private readonly List<Prop> _barriers = new List<Prop>();
        private readonly List<Prop> _charges = new List<Prop>();
        private readonly HashSet<int> _fired = new HashSet<int>();
        private bool _probeStarted;
        public bool ProbeStarted => _probeStarted;
        public int ArmedCount { get; private set; }
        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Ice)) return false;
            CrewCar = CrewTransport("M31.Senora.CrewCar");
            _generator = Equipment("prop_generator_03b", "M31.Senora.Generator");
            RequireAsset(_generator, "The perimeter generator was destroyed. Restart the defense.");
            for (int i = 1; i <= 3; i++) _barriers.Add(Equipment("prop_barrier_work05", "M31.Senora.Barrier" + i));
            Station(CrewSlot.Guess, CrewCar, VehicleSeat.Driver); Roles.For(CrewSlot.Guess).Stop();
            if (!RequireAssets(CrewCar)) return false;
            Establish("approach", "Warning, cover, an exit", "Show the generator and actual barriers. Ice checks the three approaches, Gohan arms the charges, Guess proves an escape route. These are limited defenses, not automated guns.", _generator, _barriers[0], CrewCar);
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Read the approaches", new MultiHoldObjective("Ice: inspect each yellow approach marker beside the three barriers; press E / D-pad Right", Enumerable.Range(1,3).Select(i=>At("M31.Senora.Work"+i)),2,3f,"Checking the approach") { Animation = MissionInteraction.Inspect }).OwnedBy(CrewSlot.Ice);
            yield return new MissionStage("Arm the perimeter", new MultiHoldObjective("Gohan: arm each marked barrier charge; press E / D-pad Right",Enumerable.Range(1,3).Select(i=>At("M31.Senora.Work"+i)),4,3f,"Arming charge") { Animation=MissionInteraction.Kneel, SiteDone=i=>Arm(i) }).OwnedBy(CrewSlot.Gohan)
                .OnEnter(c=>Roles.For(CrewSlot.Ice).Observe(At("M31.Senora.IceStart"),At("M31.Senora.IceStart"))).AfterCues("M31_S1_01_ICE");
            yield return new MissionStage("Prove the withdrawal road",new TravelObjective("Guess: drive the crew car to the yellow withdrawal marker and stop. Leave the road clear.",()=>At("M31.Senora.Retreat"),12,()=>CrewCar)).OwnedBy(CrewSlot.Guess)
                .OnExit(c=>StartProbe());
            // Any brother. An unowned stage inherits the previous stage's owner, which here
            // was Guess from the withdrawal drive, and a kill objective only completes while
            // its owner is the one in play: the defense could not finish as Ice, the man on
            // the rifle (the September 22 audit).
            yield return new MissionStage("Hold the perimeter",new KillTargetsObjective("Defend the generator. Stop the marked probe convoy; planted charges fire only when enemies enter their lane and the crew is clear.",()=>Opposition))
                .AnyBrother()
                .WithCues("M31_S1_02_GOHAN").OnEnter(c=>{Roles.For(CrewSlot.Guess).Stop();Roles.For(CrewSlot.Ice).TakeCover(At("M31.Senora.IceStart"));Roles.For(CrewSlot.Gohan).TakeCover(At("M31.Senora.GohanStart"));})
                .AfterCues("M31_S1_03_ICE");
            yield return new MissionStage("Check the damage",new MissionInteraction("Gohan: inspect the surviving generator controls",()=>At("M31.Senora.GeneratorWork"),4,3f,animation:MissionInteraction.Inspect,face:()=>_generator.Position)).OwnedBy(CrewSlot.Gohan).OnEnter(c=>Fighting=false);
            yield return new MissionStage("Keep the base concealed",new LoseWantedObjective("Lose any police pursuit before returning to the perimeter."));
        }
        private void Arm(int i)
        {
            var charge=Equipment("prop_ld_bomb_01","M31.Senora.Charge"+(i+1));_charges.Add(charge);ArmedCount++;
        }
        private void StartProbe()
        {
            if(ArmedCount!=3)throw new InvalidOperationException("The perimeter has not been armed.");
            _probeStarted=true;Fighting=true;
            ResponseCar("M31.Senora.Convoy1",At("M31.Senora.Barrier1"));ResponseCar("M31.Senora.Convoy2",At("M31.Senora.Barrier2"));
            Radio("GOHAN","Two probe vehicles on the road. Charges are armed, but stay clear of the barriers. We hold the generator and keep Guess's exit open.","M31_PROBE");
        }
        protected override void OnUpdate()
        {
            if(_probeStarted)
                for(int i=0;i<_charges.Count;i++)
                {
                    if(_fired.Contains(i)||!_charges[i].Exists())continue;
                    var p=_charges[i].Position;
                    bool clear=Protagonist.All.All(h=>Ctx.Crew.PedFor(h.Slot).Position.DistanceTo(p)>22f);
                    if(clear&&Opposition.Any(e=>e!=null&&e.Exists()&&!e.IsDead&&e.Position.DistanceTo(p)<6f))
                    {_fired.Add(i);World.AddExplosion(p,ExplosionType.Grenade,2f,.3f,Game.Player.Character,true,false);}
                }
            base.OnUpdate();
        }
        protected override void OnPassed(){Ctx.State?.SetUpgrade("bunkerPerimeterReady",true);}
    }
}

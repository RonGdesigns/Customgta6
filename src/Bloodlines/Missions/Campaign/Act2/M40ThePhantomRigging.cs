using System;
using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;
namespace Bloodlines.Missions.Campaign
{
    public sealed class M40ThePhantomRigging : CoastalOperation
    {
        public override string Id => "M40";
        public override string Title => "The Phantom Rigging";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;
        private readonly List<Vehicle> _boats=new List<Vehicle>();private readonly List<Prop> _kits=new List<Prop>();
        private readonly List<Prop> _targets=new List<Prop>();private Prop _table,_nav;private int _fitted,_trials;private bool _checked;
        public IList<Vehicle> Boats => _boats;
        public IList<Prop> Targets => _targets;
        public int Trials => _trials;
        protected override bool Setup()
        {
            if(!BeginCrew(CrewSlot.Guess))return false;
            for(int i=1;i<=2;i++)
            {
                var boat=Boat("tropic","M40.Boat"+i,2f,1.5f,4.5f);if(!RequireAssets(boat))return false;_boats.Add(boat);
                var kit=Equipment("prop_security_case_01","M40.Kit"+i);RequireAsset(kit,"An extraction hull kit was destroyed.");_kits.Add(kit);
                var target=WorkProp("prop_barrel_02a",At("M40.Target"+i),false);if(!RequireAssets(target))return false;target.IsInvincible=true;_targets.Add(target);
            }
            _table=Equipment("prop_table_03","M40.Nav");_nav=WorkProp("prop_laptop_01a",PropPlacement.OnTop(_table,_table.Model,new Model("prop_laptop_01a")),false);if(!RequireAssets(_nav))return false;
            Establish("approach","Build at the open cove","Two Tropics float beside the open staging beach. Guess prepares the hull kits, Ice tests carried weapons on the marked floating targets, and Gohan verifies the route at a real laptop. No walkable cave or mounted turret is implied.",_boats[0],_kits[0],_nav);
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            for(int i=0;i<2;i++)
            {
                int n=i;
                yield return new MissionStage("Prepare hull kit "+(n+1),new MissionInteraction("Guess: prepare and fit the sealed reinforcement kit for boat "+(n+1)+" at its yellow shore marker",()=>_kits[n].Position,5,3f,animation:MissionInteraction.ReachInside)).OwnedBy(CrewSlot.Guess)
                    .OnExit(c=>{SaveCargo(_kits[n],_boats[n],new Vector3(0,-1.4f,.6f));_boats[n].MaxHealth+=200;_boats[n].Health+=200;_boats[n].BodyHealth+=200;_fitted++;});
            }
            yield return new MissionStage("Test boarding weapons",new ConditionObjective("Ice: use your firearm to hit both yellow floating practice barrels; these are passenger weapons, not mounted boat guns",()=>TargetsHit())).OwnedBy(CrewSlot.Ice)
                .OnEnter(c=>{Ctx.Crew.PedFor(CrewSlot.Ice).Weapons.Give(WeaponHash.CombatMG,100,true,true);foreach(var t in _targets){t.IsInvincible=false;Function.Call(Hash.CLEAR_ENTITY_LAST_DAMAGE_ENTITY,t);}})
                .WithCues("M40_S1_01_GUESS").AfterCues("M40_S1_02_ICE");
            yield return new MissionStage("Verify navigation",new MissionInteraction("Gohan: use the laptop on the shore table to verify both sea-trial routes",()=>At("M40.NavWork"),4,3f,animation:MissionInteraction.ReachInside,face:()=>_nav.Position)).OwnedBy(CrewSlot.Gohan).OnExit(c=>_checked=true);
            for(int i=0;i<2;i++)
            {
                int n=i;var slot=n==0?CrewSlot.Guess:CrewSlot.Ice;
                yield return new MissionStage("Trial boat "+(n+1),new EnterVehicleObjective(Protagonist.Of(slot).DisplayName+": board the orange boat "+(n+1)+" as driver",()=>_boats[n],VehicleSeat.Driver)).OwnedBy(slot);
                yield return new MissionStage("Run sea trial "+(n+1),new DeliverVehicleObjective("Drive the assigned Tropic through its offshore yellow test marker",()=>_boats[n],()=>At("M40.Trial"+(n+1)),15f)).OwnedBy(slot);
                yield return new MissionStage("Return boat "+(n+1),new TravelObjective("Return the same boat to its yellow cove marker and stop",()=>At("M40.Return"+(n+1)),10f,()=>_boats[n])).OwnedBy(slot).OnExit(c=>{_trials++;Roles.For(slot).Stop();});
            }
            yield return new MissionStage("Sign off the fleet",new ConditionObjective("Both tested boats and reinforcement kits must be back at the cove",()=>_fitted==2&&_checked&&_trials==2&&Attached(_kits[0],_boats[0])&&Attached(_kits[1],_boats[1])&&BothAtCove()))
                .OnExit(c=>Establish("fleet","An exit they tested","Both loaded boats have completed a real sea trial and returned. The crew keeps its separate positions while agreeing the next step: Bradley's access card.",_boats[0],_boats[1])).AfterCues("M40_S1_03_GOHAN");
        }
        private bool BothAtCove()
        {
            bool both=true;
            for(int i=0;i<2;i++){var p=At("M40.Return"+(i+1));GameUtils.DrawObjectiveMarker(p,System.Drawing.Color.Yellow,3f);if(_boats[i].Position.DistanceTo(p)>20f)both=false;}
            return both;
        }
        private readonly HashSet<int> _hit=new HashSet<int>();
        private bool TargetsHit()
        {
            for(int i=0;i<_targets.Count;i++)
            {
                var t=_targets[i];if(t.Exists()){GameUtils.DrawObjectiveMarker(t.Position,System.Drawing.Color.Yellow,1f);if(Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY,t,Ctx.Crew.PedFor(CrewSlot.Ice),true))_hit.Add(i);}
            }
            GameUtils.Subtitle("Weapon check: "+_hit.Count+" / 2 targets hit",500);return _hit.Count==2;
        }
        protected override void OnPassed(){if(_trials!=2)throw new InvalidOperationException("Both sea trials are required.");Ctx.State?.SetCargo("extractionLaunches","M40.Return1");foreach(var b in _boats)Release(b);foreach(var kit in _kits)Release(kit);}
    }
}

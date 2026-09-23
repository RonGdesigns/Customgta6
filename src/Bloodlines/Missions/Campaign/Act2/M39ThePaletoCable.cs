using System;
using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
namespace Bloodlines.Missions.Campaign
{
    public sealed class M39ThePaletoCable : CoastalOperation
    {
        public override string Id => "M39";
        public override string Title => "The Paleto Cable";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;
        private Prop _junction,_clamp;private bool _cut;
        /// <summary>
        /// Why a stage exit could not finish. Both of this chapter's exits used to throw,
        /// which ends the attempt as a "Script error"; the reason is kept and fails it on
        /// the next frame instead (the September 22 audit).
        /// </summary>
        private string _fault;
        public bool CableCut => _cut;
        public Vehicle SurveySub => Sub;
        protected override bool Setup()
        {
            _fault=null;
            if(!BeginCoast())return false;
            _junction=SeabedEquipment("M39.Junction");RequireAsset(_junction,"The required cable junction was destroyed.");
            Establish("approach","Keep our return line","Gohan pilots the sub along two depth markers to an actual seabed junction. Ice protects the shore; Guess keeps the boat ready. The sub's equipment performs the cut, so no unequipped deep-water swim is required.",Sub,_junction,Launch);
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            for(int i=1;i<=2;i++){int n=i;yield return new MissionStage("Follow cable route "+n,new DeliverVehicleObjective("Gohan: follow the underwater yellow route marker "+n+" in the sub",()=>Sub,()=>At("M39.Route"+n),7f)).OwnedBy(CrewSlot.Gohan);}
            yield return new MissionStage("Clamp the junction",new MissionInteraction("Gohan: stop the sub beside the marked cable junction and fit its cutter clamp",()=>At("M39.CutterWork"),6,7f,()=>Sub,true)).OwnedBy(CrewSlot.Gohan)
                .OnExit(c=>FitClamp());
            yield return new MissionStage("Run and verify cutter",new MissionInteraction("Gohan: hold the sub beside the junction while the cutter runs and verify the link is down",()=>At("M39.CutterWork"),8,7f,()=>Sub,true)).OwnedBy(CrewSlot.Gohan)
                .OnExit(c=>{if(!Attached(_clamp,_junction)){_fault="The cutter detached from the junction before the cut completed. Retry the cable.";return;}_cut=true;for(int i=1;i<=4;i++)Enemy("M39.Guard"+i);Fighting=true;}).AfterCues("M39_S1_01_GOHAN");
            yield return new MissionStage("Protect the return shore",new KillTargetsObjective("Ice: stop the four marked shoreline response guards so Gohan can return",()=>Opposition)).OwnedBy(CrewSlot.Ice).AfterCues("M39_S1_02_GUESS");
            yield return new MissionStage("Return with the crew",new DeliverVehicleObjective("Gohan: surface the sub at the yellow cove return marker; Guess's boat is waiting nearby",()=>Sub,()=>At("M39.Return"),12f)).OwnedBy(CrewSlot.Gohan).OnEnter(c=>Fighting=false)
                .OnExit(c=>{Recorded=_cut;Establish("return","Our channel stayed open","The sub returns after the shoreline is clear. The mainland cable is cut; radio and air response still exist. Next the crew tests its extraction boats.",Sub,Launch);}).AfterCues("M39_S1_03_ICE");
        }
        private void FitClamp()
        {
            try
            {
                _clamp=WorkProp("prop_ld_bomb_01",_junction.Position,false);
                if(!RequireAssets(_clamp)){_fault="The cable cutter could not load. Retry the cable.";return;}
                SaveCargo(_clamp,_junction,new Vector3(0,-.6f,.6f));RequireAsset(_clamp,"The required cable cutter was destroyed.");
                Establish("clamp","Cutter attached","The cutter is attached to the visible junction. Gohan must run and verify it before calling the link severed.",_junction);
            }
            catch(Exception ex){Logger.Error(Id+" cutter clamp",ex);_fault="The cable cutter could not be fitted to the junction. Retry the cable.";}
        }
        protected override void OnUpdate()
        {
            if(_fault!=null){Fail(_fault);return;}
            if(Ctx.Crew.ActiveSlot==CrewSlot.Gohan)
            {
                if(CurrentStage<2)UnderwaterGuidance.Draw(Sub,At("M39.Route"+(CurrentStage+1)),7f);
                else if(CurrentStage<4)UnderwaterGuidance.Draw(Sub,At("M39.CutterWork"),7f);
                else if(CurrentStage==5)UnderwaterGuidance.Draw(Sub,At("M39.Return"),12f);
            }
            base.OnUpdate();
        }
        protected override void OnPassed(){if(!Recorded)throw new InvalidOperationException("The cable team has not returned.");Ctx.State?.SetUpgrade("rigMainlandCableCut",true);Release(Sub);Release(Launch);}
    }
}

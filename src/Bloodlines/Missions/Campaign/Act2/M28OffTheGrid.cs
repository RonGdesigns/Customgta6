using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
namespace Bloodlines.Missions.Campaign
{
    public sealed class M28OffTheGrid : DesertOperation
    {
        public override string Id => "M28";
        public override string Title => "Off the Grid";
        private Vehicle _pickup;
        private List<Ped> _guards;
        // Gohan's call at the cabinet shapes the response: which system he cuts first
        // decides how many come and how soon. Defaults match the pre-choice mission.
        private int _responseGapMs = 6000;
        private int _responseSize = 3;
        // With the yard cameras cut first the response does not know where Ice is:
        // squads arrive on guard and engage only when they actually see the crew,
        // instead of being sent straight at the yard's defenders.
        private bool _responseAlerted = true;
        /// <summary>
        /// What an unalerted response squad knows. With the yard cameras cut first they
        /// used to arrive on a guard post with permanent events blocked and no order ever
        /// coming, so they never reacted to anything, including Ice shooting them. The
        /// shared awareness model gives them sight, hearing and taking fire, and issues a
        /// combat order once, when one of them actually finds the crew (the September 22
        /// audit). Alerted squads are sent in directly and are not tracked here.
        /// </summary>
        private GuardAwareness _searching;
        private string _fault;
        private TechnicalChoiceObjective _choice;
        private Prop _desk, _panel, _surge;
        private RoleTracks _roles;
        private bool _spliced;
        public bool Spliced => _spliced;
        public Prop Panel => _panel;
        protected override MissionEndpoint Endpoint => MissionEndpoint.SafehouseArrival;
        protected override bool Setup()
        {
            _fault = null; _searching = null;
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, At("M28.Approach"), 0)) return false;
            ProtectCrew();
            _pickup = Car("granger", At("M28.Pickup"), Ctx.Locations.Heading("M28.Pickup"));
            _guards = Squad(At("M28.Relay"), 4);
            if (!RequireAssets(_pickup) || _guards.Count != 4) return false;
            _desk = WorkProp("prop_table_03", At("M28.Relay") + new Vector3(0,1,0));
            if (!RequireAssets(_desk)) return false;
            _panel = WorkProp("prop_laptop_01a", PropPlacement.OnTop(_desk, _desk.Model, new Model("prop_laptop_01a")), false);
            _surge = WorkProp("prop_ld_case_01", At("M28.Relay") + new Vector3(1,0,0));
            if (!RequireAssets(_panel, _surge)) return false;
            RequireAsset(_pickup, "The extraction Granger was destroyed.");
            RequireAsset(_panel, "The relay controls were destroyed before the splice.");
            RequireAsset(_surge, "The surge unit was destroyed.");
            Station(CrewSlot.Ice, At("M28.Cover")); Station(CrewSlot.Guess, _pickup, VehicleSeat.Driver);
            Ctx.Crew.CompanionsHoldPosition = true;
            _roles = new RoleTracks(Ctx.Crew, () => _guards);
            _roles.For(CrewSlot.Ice).Observe(At("M28.Cover"), At("M28.Cover"));
            Ctx.Cutscenes.Play(new SceneSpec { MissionId=Id, Phase="approach", Title="Three ways to cut the picture",
                Reason="The controls and surge case are in the yard. Ice covers the approach; Guess waits in the extraction Granger, away from the work.",
                Blocking=new SceneBlocking().Then(ShotStep.Watching(2200,Ctx.Crew.PedFor(CrewSlot.Gohan),_panel))
                    .Then(ShotStep.Watching(1800,Ctx.Crew.PedFor(CrewSlot.Ice),_panel))
                    .Then(ShotStep.Low(1800,_pickup,5,3,2)) });
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Identify the relay", new ReachZoneObjective("Gohan: reach the yellow relay-yard entrance. Ice covers the opposite approach.", ()=>At("M28.Approach") + new Vector3(0,20,0), 5)).OwnedBy(CrewSlot.Gohan);
            yield return new MissionStage("Clear the transformer yard", new KillTargetsObjective("Ice: clear the four red-marked relay guards before Gohan enters.", ()=>_guards)).OwnedBy(CrewSlot.Ice).OnEnter(c=>Attack(_guards));
            _choice = new TechnicalChoiceObjective("Gohan: choose which relay system to cut first", ()=>At("M28.Relay"), new[]
            {
                new TechnicalOption("Shared feed first", "The north loses its picture now. Dispatch still hears the alarm: full squads, but they take longer to find the yard.",
                    c => { _responseGapMs = 12000; _responseSize = 3; }),
                new TechnicalOption("Dispatch channel first", "Fewer responders get the call, but the ones who do already know where you are.",
                    c => { _responseGapMs = 3000; _responseSize = 2; }),
                new TechnicalOption("Yard cameras first", "Ice's cover stays unseen: the response arrives searching, not shooting, on the standard clock.",
                    c => { _responseGapMs = 6000; _responseSize = 3; _responseAlerted = false; })
            });
            yield return new MissionStage("Read the cabinet", _choice).OwnedBy(CrewSlot.Gohan)
                .OnEnter(c=>_roles.For(CrewSlot.Ice).Observe(At("M28.Cover"),At("M28.Cover")));
            yield return new MissionStage("Connect the surge unit", new MissionInteraction("Gohan: connect the case to the laptop on the relay worktable", ()=>At("M28.Relay"), 5, animation:MissionInteraction.ReachInside)).OwnedBy(CrewSlot.Gohan)
                // A failed connection used to throw here, which is a "Script error"; the
                // reason is kept and the attempt fails with it on the next frame.
                .OnExit(c=>{if(!StowPropStep.Stow(_surge,_desk,new Vector3(.55f,0,_desk.Model.Dimensions.Item2.Z-_surge.Model.Dimensions.Item1.Z+.01f)))_fault="The surge unit could not connect to the relay table. Retry the splice.";})
                .AfterCues("M28_S1_01_GOHAN");
            yield return new MissionStage("Cover the splice",
                new AssignedWorkObjective("Gohan continues the splice. Ice: defeat the responding squads.", CrewSlot.Gohan, ()=>At("M28.Relay"), 18),
                new SurviveWavesObjective("Ice: clear both response squads marked red.", Wave, 2, 6000) { GapProvider = () => _responseGapMs }).OwnedBy(CrewSlot.Ice)
                .WithCues("M28_S1_02_ICE").OnExit(c=>FinishSplice());
            yield return new MissionStage("Extraction", new EnterVehicleObjective("Guess: take the Granger driver seat. Wait for both brothers to board.", ()=>_pickup, VehicleSeat.Driver, true)).OwnedBy(CrewSlot.Guess).OnEnter(c=>{_roles.Release();ReleaseForPickup();});
            yield return new MissionStage("Back to shelter", new DeliverVehicleObjective("Guess: bring the crew's Granger back to the Senora bunker.", ()=>_pickup, ()=>At("M23.VehicleBay"), 25), new ProtectObjective("", ()=>_pickup,"The extraction Granger was destroyed.")).OwnedBy(CrewSlot.Guess).AfterCues("M28_S1_03_GOHAN");
        }
        private void FinishSplice()
        {
            RequiredScene("splice", "Northern picture interrupted", "The surge case is physically connected at the table before the crew withdraws.",
                new SceneBlocking().Then(new InspectStep(Ctx.Crew.PedFor(CrewSlot.Gohan),_panel.Position,1400))
                    .Then(new VerifySceneStep("Surge connected",()=>GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_ENTITY_ATTACHED_TO_ENTITY,_surge,_desk),()=>_spliced=true))
                    .Then(ShotStep.Low(1600,_panel,2,1,1)));
        }
        private IEnumerable<Ped> Wave(int wave)
        {
            var p=Squad(At("M28.Response"),_responseSize+wave); _guards.AddRange(p);
            if(_responseAlerted)Attack(p);
            else
            {
                if(_searching==null)_searching=new GuardAwareness(()=>Protagonist.All.Select(h=>Ctx.Crew.PedFor(h.Slot)).Where(x=>x!=null&&x.Exists()&&!x.IsDead));
                _searching.TrackAll(p);
            }
            Radio("ICE",_responseAlerted ? "Response on the road: " + p.Count + " coming straight at the yard. Your cut changed their dispatch." :
                "Road team is searching. Cameras are down; they haven't been directed onto our cover.","M28_RESPONSE_"+wave);
            return p;
        }
        protected override void OnUpdate()
        {
            if(_fault!=null){Fail(_fault);return;}
            if(Ctx.Cutscenes.IsActive)return;
            _roles?.Update();
            if(_searching!=null)
            {
                // A shot is not addressed to anyone: the model decides who is close
                // enough to hear it.
                foreach(var hero in Protagonist.All)
                {
                    var ped=Ctx.Crew.PedFor(hero.Slot);
                    if(ped!=null&&ped.Exists()&&!ped.IsDead&&ped.IsShooting)_searching.ReportToAll(Stimulus.GunshotHeard,ped.Position);
                }
                _searching.Update();
            }
            base.OnUpdate();
        }
        protected override void OnPassed()
        {
            if(!_spliced)throw new System.InvalidOperationException("The relay splice was not verified.");
        }
        protected override void OnCleanup() { _roles?.Release(); _searching?.Clear(); _searching=null; Ctx.Crew.CompanionsHoldPosition=false; }
    }
}

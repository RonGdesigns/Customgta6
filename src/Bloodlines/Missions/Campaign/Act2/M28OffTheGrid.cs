using System.Collections.Generic;
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
        private TechnicalChoiceObjective _choice;
        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, At("M28.Approach"), 0)) return false;
            _pickup = Car("granger", At("M28.Pickup"));
            _guards = Squad(At("M28.Relay"), 4);
            if (!RequireAssets(_pickup) || _guards.Count != 4) return false;
            Station(CrewSlot.Ice, At("M28.Cover")); Station(CrewSlot.Guess, _pickup, VehicleSeat.Driver);
            Ctx.Crew.CompanionsHoldPosition = true;
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
                new TechnicalOption("Yard cameras first", "Ice's cover position stays unseen. Standard response on the standard clock.",
                    c => { _responseGapMs = 6000; _responseSize = 3; })
            });
            yield return new MissionStage("Read the cabinet", _choice).OwnedBy(CrewSlot.Gohan);
            yield return new MissionStage("Connect the surge unit", new MissionInteraction("Gohan: connect the surge unit at the relay service cabinet", ()=>At("M28.Relay"), 5)).OwnedBy(CrewSlot.Gohan).AfterCues("M28_S1_01_GOHAN");
            yield return new MissionStage("Cover the splice",
                new AssignedWorkObjective("Gohan continues the splice. Ice: defeat the responding squads.", CrewSlot.Gohan, ()=>At("M28.Relay"), 18),
                new SurviveWavesObjective("Ice: clear both response squads marked red.", Wave, 2, 6000) { GapProvider = () => _responseGapMs }).OwnedBy(CrewSlot.Ice)
                .WithCues("M28_S1_02_ICE").AfterCues("M28_S1_03_GOHAN");
            yield return new MissionStage("Extraction", new EnterVehicleObjective("Guess: take the Granger driver seat. Wait for both brothers to board.", ()=>_pickup, VehicleSeat.Driver, true)).OwnedBy(CrewSlot.Guess).OnEnter(c=>ReleaseForPickup());
            yield return new MissionStage("Back to shelter", new DeliverVehicleObjective("Guess: bring the crew's Granger back to the radar bunker.", ()=>_pickup, ()=>At("M23.DomeApproach"), 25), new ProtectObjective("", ()=>_pickup,"The extraction Granger was destroyed.")).OwnedBy(CrewSlot.Guess);
        }
        private IEnumerable<Ped> Wave(int wave) { var p=Squad(At("M28.Response"),_responseSize+wave);Attack(p);return p; }
    }
}

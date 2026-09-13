using System;
using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    public sealed class SM05BlackBoxEstuary : DesertOperation
    {
        public override string Id => "SM05";
        public override string Title => "Black Box Estuary";
        private Vehicle _boat;
        private Prop _buoy, _interceptor;
        private bool _moored, _wasFrozen, _wasRunning, _installed;
        public Vehicle Boat => _boat;
        public Prop Buoy => _buoy;
        public Prop Interceptor => _interceptor;
        public bool Installed => _installed;
        public bool Moored => _moored;
        protected override bool Setup()
        {
            _installed = _moored = false;
            if (!MissionSites.Prepare(Ctx.Locations, Id) || !Ctx.Crew.DeploySolo(CrewSlot.Gohan, At("SM05.Shore"), Ctx.Locations.Heading("SM05.Shore"))) return false;
            _boat = Car("dinghy", At("SM05.Boat"), Ctx.Locations.Heading("SM05.Boat"));
            _buoy = WorkProp("prop_dock_bouy_3", At("SM05.Buoy"), false);
            _interceptor = WorkProp("prop_tool_box_04", At("SM05.Buoy") + new Vector3(.6f, 0, .1f), false);
            if (!RequireAssets(_boat, _buoy, _interceptor)) return false;
            RequireAsset(_boat, "The return dinghy was lost. Restart the estuary job.");
            RequireAsset(_buoy, "The monitoring buoy was destroyed.");
            RequireAsset(_interceptor, "The interceptor was lost before the return to shore.");
            _interceptor.IsVisible = false;
            GTA.Native.Function.Call(GTA.Native.Hash.SET_ENTITY_COLLISION, _interceptor, false, false);
            Ctx.Cutscenes.Play(new SceneSpec { MissionId = Id, Phase = "approach", Title = "A way back",
                Reason = "Show the waiting dinghy and monitoring buoy. Guess and Ice are voices on the radio, not nearby passengers.",
                Blocking = new SceneBlocking().Then(ShotStep.Low(2200, _boat, 7, 4, 3))
                    .Then(ShotStep.Low(2400, _buoy, 4, 3, 2)) });
            RequireSurvivor(Ctx.Crew.PedFor(CrewSlot.Gohan), "Gohan is down. Restart this solo mission.");
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Into the estuary", new EnterVehicleObjective("Gohan: board the marked dinghy and drive to the monitoring buoy.", () => _boat, VehicleSeat.Driver))
                .PlayedBy(CrewSlot.Gohan).AfterCues("SM05_S1_01_GOHAN");
            yield return new MissionStage("Moor beside the buoy", new DeliverVehicleObjective("Gohan: stop within 8m of the yellow buoy. Your dinghy will wait while you fit the interceptor.", () => _boat, () => _buoy.Position, 8), new ProtectObjective("", () => _boat, "The pickup dinghy was destroyed."))
                .PlayedBy(CrewSlot.Gohan).OnExit(c => Moor());
            yield return new MissionStage("Fit the interceptor", new MissionInteraction("Gohan: exit and swim beside the buoy; fit the interceptor to its marked service point", () => _interceptor.Position, 6, 3), new ProtectObjective("", () => _boat, "The dinghy was lost during installation."), new ProtectObjective("", () => _buoy, "The monitoring buoy was destroyed."))
                .PlayedBy(CrewSlot.Gohan).WithCues("SM05_S1_02_GOHAN").OnExit(c => FitInterceptor());
            yield return new MissionStage("Verify telemetry", new ConditionObjective("Verify the fitted interceptor and wait for telemetry confirmation.", () => _installed && !Ctx.Cutscenes.IsActive))
                .PlayedBy(CrewSlot.Gohan).AfterCues("SM05_S2_03_GOHAN");
            yield return new MissionStage("Back aboard", new EnterVehicleObjective("Gohan: swim back and climb into the orange dinghy. The mooring releases when you are aboard.", () => _boat, VehicleSeat.Driver), new ProtectObjective("", () => _boat, "The return dinghy was destroyed."))
                .PlayedBy(CrewSlot.Gohan).OnExit(c => Unmoor());
            yield return new MissionStage("Shore pickup", new DeliverVehicleObjective("Gohan: return the dinghy to the yellow water pickup beside shore.", () => _boat, () => At("SM05.Boat"), 12))
                .PlayedBy(CrewSlot.Gohan).OnExit(c => Radio("GOHAN", "Back at the landing. The interceptor is transmitting. I'm coming home.", "SM05_RETURN"));
        }
        private void Moor()
        {
            if (_moored || !RequireAssets(_boat)) return;
            _wasFrozen = _boat.IsPositionFrozen; _wasRunning = _boat.IsEngineRunning;
            _moored = true; _boat.IsPositionFrozen = true; _boat.IsEngineRunning = false;
        }
        private void Unmoor()
        {
            if (!_moored) return;
            _moored = false;
            if (_boat != null && _boat.Exists()) { _boat.IsPositionFrozen = _wasFrozen; _boat.IsEngineRunning = _wasRunning; }
        }
        private void FitInterceptor()
        {
            RequiredScene("interceptor", "A confirmed signal", "A close shot of the installed service box; Gohan stays at the buoy and the dinghy stays moored.",
                new SceneBlocking().Then(new VerifySceneStep("Buoy and interceptor intact", () => RequireAssets(_buoy, _interceptor) && !_buoy.IsDead, () => _interceptor.IsVisible = true))
                    .Then(new ShotStep(2400, _interceptor, new Vector3(-1.5f, -1.5f, .8f), _interceptor, new Vector3(0, 0, .15f)))
                    .Then(new VerifySceneStep("Interceptor transmitting", () => _interceptor != null && _interceptor.Exists() && _interceptor.IsVisible && _buoy.Exists() && !_buoy.IsDead && _boat.Exists() && !_boat.IsDead, () => _installed = true)));
        }
        protected override void OnUpdate() { if (!Ctx.Cutscenes.IsActive) base.OnUpdate(); }
        protected override void OnCleanup() { Unmoor(); }
        protected override void OnPassed()
        {
            if (!_installed) throw new InvalidOperationException("The interceptor was not verified.");
            Ctx.State?.SetEvidence("estuaryTelemetry", EvidenceState.CopyHeld);
            Preserve(_buoy); Preserve(_interceptor);
        }
    }
}

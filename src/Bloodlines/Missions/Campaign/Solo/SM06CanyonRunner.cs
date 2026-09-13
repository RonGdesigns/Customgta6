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
    public sealed class SM06CanyonRunner : DesertOperation
    {
        public override string Id => "SM06";
        public override string Title => "Canyon Runner";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;
        private Vehicle _truck, _trailer;
        private Prop _receiver;
        private bool _unloaded;
        private readonly List<Ped> _riders = new List<Ped>();
        public Vehicle Truck => _truck;
        public Vehicle Tanker => _trailer;
        public bool Unloaded => _unloaded;
        protected override bool Setup()
        {
            _unloaded = false; _riders.Clear();
            if (!MissionSites.Prepare(Ctx.Locations, Id) || !Ctx.Crew.DeploySolo(CrewSlot.Guess, At("SM06.Approach"), Ctx.Locations.Heading("SM06.Approach"))) return false;
            _truck = FuelRig("SM06.Truck", out _trailer);
            // Receiving equipment stands off the vehicle's delivery center, not in its path.
            _receiver = WorkProp("prop_air_fueltrail1", At("SM06.Delivery") + new Vector3(9, 0, 0));
            if (!RequireAssets(_truck, _trailer, _receiver)) return false;
            RequireAsset(_truck, "The fuel tractor was destroyed. Restart the canyon run.");
            RequireAsset(_trailer, "The aviation-fuel tanker was destroyed.");
            RequireAsset(_receiver, "The receiving equipment was destroyed.");
            Ctx.Cutscenes.Play(new SceneSpec { MissionId = Id, Phase = "approach", Title = "Fuel, not a race",
                Reason = "Show the coupled aviation-fuel tanker before Guess boards. Ice checks in by radio; nobody appears beside him.",
                Blocking = new SceneBlocking().Then(ShotStep.Low(2400, _trailer, 9, 6, 3))
                    .Then(ShotStep.Low(2000, _truck, 7, -4, 2)) });
            RequireSurvivor(Ctx.Crew.PedFor(CrewSlot.Guess), "Guess is down. Restart this solo mission.");
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Take the fuel rig", new EnterVehicleObjective("Guess: take the orange Phantom tractor. Keep its aviation-fuel tanker attached.", () => _truck, VehicleSeat.Driver))
                .PlayedBy(CrewSlot.Guess).AfterCues("SM06_S1_01_GUESS");
            yield return new MissionStage("Canyon run", new OccupiedVehicleDestination("Guess: drive THROUGH the yellow canyon checkpoint. Slow for bends; no need to stop.", () => _truck, () => At("SM06.Bend"), 35), new ProtectObjective("", () => _trailer, "The aviation-fuel tanker was destroyed."))
                .PlayedBy(CrewSlot.Guess).OnEnter(c => StartBikes()).AfterCues("SM06_S2_03_GUESS");
            yield return new MissionStage("Airfield reserves", new DeliverVehicleObjective("Guess: deliver the tractor AND attached tanker to McKenzie's yellow fuel bay.", () => _truck, () => At("SM06.Delivery"), 25), new TrailerDeliveryObjective(() => _truck, () => _trailer, () => At("SM06.Delivery")), new ProtectObjective("", () => _trailer, "The airfield fuel was lost."))
                .PlayedBy(CrewSlot.Guess);
            yield return new MissionStage("Unload safely", new FuelUnload(this), new ProtectObjective("", () => _trailer, "The tanker exploded before unloading finished."), new ProtectObjective("", () => _receiver, "The receiving fuel equipment was destroyed."))
                .PlayedBy(CrewSlot.Guess).OnExit(c => ReceiveFuel());
            yield return new MissionStage("Delivery received", new ConditionObjective("Confirm the fuel delivery at the receiving equipment.", () => _unloaded && !Ctx.Cutscenes.IsActive))
                .PlayedBy(CrewSlot.Guess).AfterCues("SM06_S2_04_GUESS");
        }
        public bool RigAtDelivery()
        {
            if (_truck == null || !_truck.Exists() || _truck.IsDead || _trailer == null || !_trailer.Exists() || _trailer.IsDead) return false;
            var coupled = new OutputArgument();
            return Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE, _truck, coupled) && coupled.GetResult<int>() == _trailer.Handle
                && _truck.Position.DistanceTo(At("SM06.Delivery")) <= 25f && _trailer.Position.DistanceTo(At("SM06.Delivery")) <= 35f
                && _truck.Speed < 1f && _trailer.Speed < 1f;
        }
        private sealed class FuelUnload : Objective
        {
            private readonly SM06CanyonRunner _mission;
            private readonly MissionInteraction _work;
            public FuelUnload(SM06CanyonRunner mission) : base("Guess: stop the attached fuel rig in the bay, then press E / D-pad Right to unload.")
            {
                _mission = mission;
                _work = new MissionInteraction("Guess: pump aviation fuel into the marked receiving equipment", () => mission.At("SM06.Delivery"), 6, 25, () => mission._truck, stopVehicle: true);
            }
            public override void Enter(MissionContext c) { base.Enter(c); _work.RequiredCharacter = CrewSlot.Guess; _work.Enter(c); }
            public override void Exit(MissionContext c) { _work.Exit(c); }
            public override void Update(MissionContext c)
            {
                if (!_mission.RigAtDelivery() || !IsOwnerActive(c) || Game.Player.Character.SeatIndex != VehicleSeat.Driver)
                {
                    _work.Exit(c); _work.Enter(c);
                    Label = "Reconnect the tanker and stop BOTH vehicles in the yellow fuel bay. Unloading restarts if you move or detach.";
                    ObjectiveMarkers.Navigation(_mission.At("SM06.Delivery"), RequiredCharacter, _mission._truck); return;
                }
                _work.Update(c); Label = _work.Label;
                if (_work.Status == ObjectiveStatus.Failed) Fail(_work.FailReason);
                else if (_work.Status == ObjectiveStatus.Complete) Complete();
            }
        }
        private void ReceiveFuel()
        {
            RequiredScene("delivery", "Fuel received", "The tractor and attached tanker stay parked. Show the receiving equipment before confirming fuel reserves.",
                new SceneBlocking().Then(ShotStep.Low(2200, _trailer, 8, 5, 3))
                    .Then(ShotStep.Low(1800, _receiver, 3, 2, 1.5f))
                    .Then(new VerifySceneStep("Attached tanker delivered", () => RigAtDelivery() && _receiver.Exists() && !_receiver.IsDead, () => _unloaded = true)));
        }
        private void StartBikes()
        {
            for (int i = 0; i < 3; i++)
            {
                var behind = _truck.Position - _truck.ForwardVector * (80 + i * 18);
                if (!GameUtils.NearestRoadNode(behind, 60, out var point, out float heading)) continue;
                var bike = Car("sanchez", point, heading, false); var rider = Guard(point, WeaponHash.MicroSMG);
                if (!RequireAssets(bike, rider)) { GameUtils.SafeDelete(bike); GameUtils.SafeDelete(rider); continue; }
                rider.SetIntoVehicle(bike, VehicleSeat.Driver);
                rider.Task.VehicleChase(Game.Player.Character); rider.Task.VehicleShootAtPed(Game.Player.Character); _riders.Add(rider);
                var blip = Track(bike.AddBlip()); if (blip != null) { blip.Color = BlipColor.Red; blip.Name = "Fuel hijacker"; }
            }
            if (_riders.Count > 0) Say("SM06_S1_02_GUESS");
            else Radio("ICE", "No bikes on the approach. Keep the cargo moving and watch the bends.", "SM06_CLEAR_APPROACH");
        }
        protected override void OnUpdate() { if (!Ctx.Cutscenes.IsActive) base.OnUpdate(); }
        protected override void OnPassed()
        {
            if (!_unloaded) throw new InvalidOperationException("The fuel delivery has not been verified.");
            Ctx.State?.SetCargo("airfieldFuel", "SM06.Delivery"); Preserve(_receiver);
        }
        protected override void OnCleanup() { _riders.Clear(); }
    }
}

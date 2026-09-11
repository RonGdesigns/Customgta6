using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M18 — "The Staging Line". Terminal Island salt flats, 20:00, salt fog.
    ///
    /// The night before the Port Heist. Gohan slips the Kraken into the channel, Guess
    /// hides the Cargobob in a salt hangar and fits the jammer pod to it, Ice loads
    /// the anti-air launchers into the hauler, and then Ice says the thing about
    /// finishing what started.
    ///
    /// Seen, not told: the plan on the hood before anyone moves (the sub's route,
    /// the lift point, the escort water route, the inland stash), with the operator
    /// of every vehicle named; each delivery a real endpoint and seat; the pod
    /// fitted to the lift, so M20 has a thing to switch on; the roll call as a scene
    /// with each man in his own seat, under one clock, with the abort rule and the
    /// people-first rule said; the open solo jobs called out before the operation,
    /// not after it. The endpoint is continuous: M19 follows under the same clock.
    /// </summary>
    public sealed class M18TheStagingLine : ComposedMission
    {
        private Vehicle _kraken;
        private Vehicle _cargobob;
        private Vehicle _hauler;
        private Prop _pod;
        private Vector3 _channel;
        private Vector3 _hangar;
        private Vector3 _haulerMark;
        private bool _podFitted, _rollCalled;

        public override string Id => "M18";
        public override string Title => "The Staging Line";
        protected override MissionEndpoint Endpoint => MissionEndpoint.ContinuousNext;

        public Vehicle Kraken => _kraken;
        public Vehicle Cargobob => _cargobob;
        public Vehicle Hauler => _hauler;
        public Prop Pod => _pod;
        public bool PodFitted => _podFitted;
        public bool RollCalled => _rollCalled;

        protected override bool Setup()
        {
            MissionSites.Ground(Ctx.Locations, "M18.SaltHangar");
            MissionSites.Ground(Ctx.Locations, "M18.HaulerMark");
            _channel = MarineSites.ResolveOrThrow(Ctx.Locations, "M18.ChannelMark", 6f);
            _hangar = Ctx.Locations.Position("M18.SaltHangar");
            _haulerMark = Ctx.Locations.Position("M18.HaulerMark");

            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, _hangar + new Vector3(0f, -20f, 0f), 0f)) return false;

            ApplyBibleSetting();

            SpawnAssets();
            if (!RequireAssets(_kraken, _cargobob, _hauler)) return false;
            Station(CrewSlot.Gohan, _kraken, VehicleSeat.Driver);
            Station(CrewSlot.Guess, _cargobob, VehicleSeat.Driver);
            Station(CrewSlot.Ice, _hauler, VehicleSeat.Driver);
            RemindOpenSolos();
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Sub into the channel",
                    new EnterVehicleObjective("Gohan — take the Kraken out.", () => _kraken,
                        VehicleSeat.Driver),
                    new DeliverVehicleObjective("Hold her in the channel.", () => _kraken,
                        () => _channel, 25f))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(context => Ctx.State?.SetCargo("kraken", "M18.ChannelMark"));

            yield return new MissionStage("Bird in the hangar",
                    new DeliverVehicleObjective("Guess — put the Cargobob in the salt hangar.",
                        () => _cargobob, () => _hangar, 30f, land: true))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => Ctx.State?.SetCargo("cargobob", "M18.SaltHangar"));

            // The pod from McKenzie goes on the lift here, so M20 has a thing to switch on.
            yield return new MissionStage("Fit the pod",
                    new MissionInteraction("Guess — fit the jammer pod to the Cargobob.", () => PodPoint(), 6, 4f))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => FitPod());

            yield return new MissionStage("Load the launchers",
                    new DeliverVehicleObjective("Ice — bring the hauler onto the line.",
                        () => _hauler, () => _haulerMark, 20f))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Load the parked hauler",
                    new MissionInteraction("Load the anti-air launchers.", () => _haulerMark, 10, 6f))
                .OwnedBy(CrewSlot.Ice)
                .OnExit(context => { Ctx.State?.SetCargo("hauler", "M18.HaulerMark"); PlayRollCall(); });

            yield return new MissionStage("Countdown",
                    new DialogueFinishedObjective("Keep your assigned vehicle in place. Listen to the final radio check."))
                .OnExit(context =>
                {
                    // Everything Act I has been buying is now in one place, under one clock.
                    Ctx.State?.SetCargo("heistClock", "M18");
                    GameUtils.Subtitle("~y~One clock from here. Abort is answered before it is argued; a container is not a fourth brother.", 7000);
                });
        }

        // ---------- beats ----------

        /// <summary>The open solo jobs, called out before the operation and not after it.</summary>
        private void RemindOpenSolos()
        {
            var open = new List<string>();
            foreach (var id in new[] { "SM01", "SM02", "SM03" })
                if (Ctx.State != null && !Ctx.State.IsComplete(id)) open.Add(id);
            if (open.Count == 0) return;
            GameUtils.Notify("~y~Open before the heist: " + string.Join(", ", open) + ". M19 waits on them; stage now, finish them after.");
            Logger.Info("M18: open solo jobs before the heist: " + string.Join(", ", open));
        }

        /// <summary>The plan on the hood: the sub's route, the lift point, the escort water route, the inland stash; every vehicle with its operator.</summary>
        private void PlayApproach()
        {
            var blocking = new SceneBlocking();
            if (_hauler != null && _hauler.Exists()) blocking.Then(new ShotStep(3400, _hauler, new Vector3(-4f, 3.5f, 1.6f), _hauler, new Vector3(0f, 2.2f, 1.1f), 0.5f));
            if (_kraken != null && _kraken.Exists()) blocking.Then(new ShotStep(2800, _kraken, new Vector3(-8f, 4f, 2.5f), _kraken, new Vector3(0f, 0f, 0.5f), 1.0f));
            if (_cargobob != null && _cargobob.Exists()) blocking.Then(new ShotStep(2800, _cargobob, new Vector3(-16f, 8f, 4f), _cargobob, new Vector3(0f, 0f, 1.5f), 1.4f));
            blocking.Then(ShotStep.Wide(3000, _channel + new Vector3(0f, 0f, 2f), 40f, 16f, 12f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The plan",
                Reason = "A map on the hauler's hood: Gohan takes the sub down the channel to hold 3; Ron lifts the container out from the flats; the escort launch runs the water route with Ice on it; the load goes inland to the Alamo. Sub, lift, launch, pickup: one operator each, no fourth man.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M18 approach scene did not play; the flats stand on their own.");
        }

        private Vector3 PodPoint() => _cargobob != null && _cargobob.Exists() ? _cargobob.Position + new Vector3(3f, 0f, 0f) : _hangar;

        /// <summary>The pod on the lift: a real part on the airframe, recorded where M20 finds it.</summary>
        private void FitPod()
        {
            _podFitted = true;
            if (_cargobob != null && _cargobob.Exists())
            {
                var model = new Model("prop_box_ammo03a");
                if (GameUtils.RequestModel(model))
                {
                    _pod = Track(World.CreateProp(model, _cargobob.Position + new Vector3(0f, 0f, 2f), false, false));
                    model.MarkAsNoLongerNeeded();
                    if (_pod != null && _pod.Exists()) { _pod.IsPersistent = true; StowPropStep.Stow(_pod, _cargobob, new Vector3(2.4f, -1.0f, 0.2f)); }
                }
            }
            Ctx.State?.SetCargo("radarPod", "M18.SaltHangar");
            GameUtils.Subtitle("~g~Jammer pod on the lift. Gohan switches it on from the channel when the load comes up.", 5000);
        }

        /// <summary>The roll call as a scene: each man in his own seat, the three stage lines, one clock.</summary>
        private void PlayRollCall()
        {
            _rollCalled = true;
            var blocking = new SceneBlocking();
            if (_hauler != null && _hauler.Exists()) blocking.Then(new ShotStep(3400, _hauler, new Vector3(-4f, 2f, 1.4f), _hauler, new Vector3(0f, 0.8f, 0.9f), 0.5f));
            if (_cargobob != null && _cargobob.Exists()) blocking.Then(new ShotStep(3400, _cargobob, new Vector3(-6f, 3f, 2f), _cargobob, new Vector3(0f, 1.5f, 1.2f), 0.6f));
            if (_kraken != null && _kraken.Exists()) blocking.Then(new ShotStep(3400, _kraken, new Vector3(-6f, 3f, 2f), _kraken, new Vector3(0f, 0f, 0.5f), 0.8f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "rollcall", Title = "The roll call",
                Reason = "Ice in the hauler, Ron in the lift, Gohan in the sub: each in his own seat at his own endpoint. One clock from here; abort is answered before it is argued; a container is not a fourth brother.",
                Blocking = blocking
            };
            var lines = new[] { Ctx.Data?.Cue("M18_S1_01_ICE"), Ctx.Data?.Cue("M18_S1_02_GUESS"), Ctx.Data?.Cue("M18_S1_03_GOHAN") };
            if (!Ctx.Cutscenes.PlayStaged(spec, lines))
            {
                Logger.Warn("M18 roll call scene did not play; the lines play as dialogue.");
                blocking.Complete();
                Say("M18_S1_01_ICE"); Say("M18_S1_02_GUESS"); Say("M18_S1_03_GOHAN");
            }
        }

        /// <summary>The aftermath: the three assets on the line, under one clock.</summary>
        public override SceneBlocking OutroBlocking()
        {
            return new SceneBlocking().Then(ShotStep.Wide(4500, _haulerMark + new Vector3(0f, 0f, 2f), 30f, 14f, 10f));
        }

        // ---------- world building ----------

        private void SpawnAssets()
        {
            var krakenModel = new Model("submersible2");
            if (GameUtils.RequestModel(krakenModel))
            {
                _kraken = Track(World.CreateVehicle(krakenModel, _channel + new Vector3(0f, 0f, -1.4f), 180f));
                krakenModel.MarkAsNoLongerNeeded();
                if (_kraken != null && _kraken.Exists())
                {
                    _kraken.IsPersistent = true;
                    var blip = Track(_kraken.AddBlip());
                    blip.Sprite = BlipSprite.Boat;
                    blip.Color = BlipColor.Green;
                    blip.Name = "Kraken";
                }
            }

            var heliModel = new Model("cargobob");
            if (GameUtils.RequestModel(heliModel))
            {
                _cargobob = Track(World.CreateVehicle(heliModel, _hangar + new Vector3(40f, 30f, 0f), 90f));
                heliModel.MarkAsNoLongerNeeded();
                if (_cargobob != null && _cargobob.Exists())
                {
                    _cargobob.IsPersistent = true;
                    var blip = Track(_cargobob.AddBlip());
                    blip.Sprite = BlipSprite.Helicopter;
                    blip.Color = BlipColor.Orange;
                    blip.Name = "Cargobob";
                }
            }

            var haulerModel = new Model("benson");
            if (GameUtils.RequestModel(haulerModel))
            {
                _hauler = Track(World.CreateVehicle(haulerModel, _haulerMark + new Vector3(-35f, -10f, 0f), 90f));
                haulerModel.MarkAsNoLongerNeeded();
                if (_hauler != null && _hauler.Exists())
                {
                    _hauler.IsPersistent = true;
                    var blip = Track(_hauler.AddBlip());
                    blip.Sprite = BlipSprite.ArmoredTruck;
                    blip.Color = BlipColor.Blue;
                    blip.Name = "Getaway hauler";
                }
            }
        }

        protected override void OnPassed()
        {
            // The staged assets are the next chapter's, not the mission's.
            if (_kraken != null && _kraken.Exists()) Release(_kraken);
            if (_cargobob != null && _cargobob.Exists()) Release(_cargobob);
            if (_hauler != null && _hauler.Exists()) Release(_hauler);
            if (_pod != null && _pod.Exists()) Release(_pod);
        }
    }
}

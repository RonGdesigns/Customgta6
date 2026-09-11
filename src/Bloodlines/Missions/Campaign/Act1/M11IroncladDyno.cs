using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M11 — "Ironclad Dyno". Burro Heights chop shop, 15:00.
    ///
    /// The quiet one. Nobody shoots at anybody: Guess and Ice drop the turbine into
    /// the Granger, argue about a stripped alternator fifteen years ago, and Gohan
    /// walks in with Berth 44.
    ///
    /// Seen, not told: the shop mid-work when the player walks in (Ron at the open
    /// bay with the first crate off the flatbed, Ice with the tools, Gohan coming
    /// up the drive), not a briefing; the calibration kept as the mission's whole
    /// mechanic; Gohan's turn to Berth 44 happening in the room, at the Granger's
    /// window, from the bible's own lines; the upgraded truck shown before the
    /// tools go down. Three billion is a manifest figure, not money.
    ///
    /// This mission exists because the campaign needs somewhere to breathe. If
    /// every mission is a firefight, the firefights stop meaning anything.
    /// </summary>
    public sealed class M11IroncladDyno : ComposedMission
    {
        private const float TargetBoostLow = 22f;
        private const float TargetBoostHigh = 28f;

        private Vehicle _granger;
        private Vehicle _flatbed;
        private Prop _crate;
        private Vector3 _shop;
        private Vector3 _dyno;
        private bool _berthPlayed;

        public override string Id => "M11";
        public override string Title => "Ironclad Dyno";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SafehouseArrival;

        public Vehicle Granger => _granger;
        public Vehicle Flatbed => _flatbed;
        public Prop Crate => _crate;
        public bool BerthPlayed => _berthPlayed;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _shop = Ctx.Locations.Position("M11.ChopShop");
            _dyno = Ctx.Locations.Position("M11.DynoPad");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, _dyno + new Vector3(-3f, 0f, 0f), Ctx.Locations.Heading("M11.ChopShop")))
            {
                return false;
            }

            ApplyBibleSetting();
            Ctx.Crew.CompanionsHoldPosition = true;
            SpawnGranger();
            SpawnFlatbed();
            if (!RequireAssets(_granger)) return false;
            Station(CrewSlot.Ice, _dyno + new Vector3(4f, 0f, 0f));
            Station(CrewSlot.Gohan, _shop + new Vector3(0f, 22f, 0f));
            PlayShop();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Mount the turbine",
                    new MissionInteraction("Guess — fabricate the motor mounts.", () => _dyno, 10, 4f))
                .OwnedBy(CrewSlot.Guess)
                .AfterCues("M11_S1_01_GUESS");

            yield return new MissionStage("On the dyno",
                    new EnterVehicleObjective("Ice — get in and hold it on the dyno.", () => _granger,
                        VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Manifold pressure",
                    new DynoObjective("Ice: use partial RT or tap W to hold 22-28 PSI. Stay in the driver seat.",
                        () => _granger, TargetBoostLow, TargetBoostHigh, 12))
                .OwnedBy(CrewSlot.Ice)
                .OnExit(context =>
                {
                    // The payoff is permanent and mechanical: FleetGarage applies this
                    // profile to the Granger for the rest of the campaign, awarded once
                    // by CampaignState.MarkComplete after the mission passes.
                    GameUtils.Subtitle("~g~Exhaust temps green. The 3600LX is done.", 5000);
                    PlayBerth();
                })
                .AfterCues("M11_S1_02_ICE");

            // Gohan's turn to Berth 44 happens in the room, at the window, from the
            // bible's own lines; the stage waits for that scene, nothing else.
            yield return new MissionStage("Berth 44",
                    new DialogueFinishedObjective("Gohan has the manifest. Hear him out."))
                .OnExit(context =>
                    GameUtils.Subtitle("~y~Three billion on the manifest, sitting in Berth 44. A claim, not cash: survey first.", 6000));
        }

        // ---------- beats ----------

        /// <summary>The shop mid-work: Ron at the open bay, Ice with the tools, Gohan coming up the drive. No briefing.</summary>
        private void PlayShop()
        {
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var blocking = new SceneBlocking();
            if (guess != null && guess.Exists()) blocking.Then(new InspectStep(guess, _dyno, 3200, "WORLD_HUMAN_WELDING"));
            if (ice != null && ice.Exists()) blocking.Then(new InspectStep(ice, _dyno + new Vector3(2f, 0f, 0f), 2600, "WORLD_HUMAN_CLIPBOARD"));
            if (gohan != null && gohan.Exists()) blocking.Then(new WalkToStep(gohan, _shop + new Vector3(0f, 8f, 0f), 1.2f));
            if (_granger != null && _granger.Exists()) blocking.Then(new ShotStep(3000, _granger, new Vector3(-5f, 3f, 1.6f), _granger, new Vector3(0f, 1.5f, 0.9f), 0.6f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "shop", Title = "The shop",
                Reason = "The first turbine crate is off the flatbed and open at the Granger's bay; Ron is on the mounts, Ice has the tools, Gohan is walking up with his findings. Nobody stops for a briefing.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M11 shop scene did not play; the work stands on its own.");
        }

        /// <summary>The engine shut down, Gohan comes to the window with Berth 44: the bible's stage-four lines, in the room.</summary>
        private void PlayBerth()
        {
            _berthPlayed = true;
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var blocking = new SceneBlocking { DialogueAfterStep = 1 };
            if (gohan != null && gohan.Exists() && _granger != null && _granger.Exists())
                blocking.Then(new WalkToStep(gohan, _granger.Position + new Vector3(-2.2f, 0.5f, 0f), 1.0f) { TimeoutMs = 12000 });
            else blocking.Then(new WaitStep(600));
            if (guess != null && guess.Exists() && gohan != null && gohan.Exists()) blocking.Then(ShotStep.OverShoulder(4200, guess, gohan, 0.3f));
            if (_granger != null && _granger.Exists()) blocking.Then(new ShotStep(4200, _granger, new Vector3(-4.5f, 2.5f, 1.5f), _granger, new Vector3(0f, 0f, 0.8f), 0.6f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "berth", Title = "Berth 44",
                Reason = "With the engine shut down, Gohan walks to the Granger's window with the manifest: Berth 44, three billion as a figure on paper. The attention shifts in the room.",
                Blocking = blocking
            };
            var lines = new[] { Ctx.Data?.Cue("M11_S1_03_GUESS"), Ctx.Data?.Cue("M11_S1_04_ICE"), Ctx.Data?.Cue("M11_S1_05_GOHAN") };
            if (!Ctx.Cutscenes.PlayStaged(spec, lines))
            {
                Logger.Warn("M11 berth scene did not play; the lines play as dialogue.");
                blocking.Complete();
                Say("M11_S1_03_GUESS"); Say("M11_S1_04_ICE"); Say("M11_S1_05_GOHAN");
            }
        }

        /// <summary>The aftermath: Ice out of the cab, the tools down, the upgraded Granger shown.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_granger == null || !_granger.Exists()) return null;
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var blocking = new SceneBlocking();
            if (ice != null && ice.Exists() && ice.IsInVehicle(_granger)) blocking.Then(new ExitVehicleStep(ice));
            return blocking.Then(new ShotStep(4500, _granger, new Vector3(-6f, 4f, 1.8f), _granger, new Vector3(0f, 0f, 0.8f), 1.2f));
        }

        // ---------- world building ----------

        private void SpawnGranger()
        {
            var model = new Model("granger");
            if (!GameUtils.RequestModel(model)) return;

            _granger = Track(World.CreateVehicle(model, _dyno, Ctx.Locations.Heading("M11.ChopShop")));
            model.MarkAsNoLongerNeeded();
            if (_granger == null || !_granger.Exists()) return;

            _granger.IsPersistent = true;
            // Bolted to the rollers: it revs, it does not go anywhere.
            _granger.IsPositionFrozen = true;

            var blip = Track(_granger.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Orange;
            blip.Name = "Granger 3600LX";
        }

        /// <summary>The flatbed M10 delivered, with the second crate still on it: the first is in the Granger's bay.</summary>
        private void SpawnFlatbed()
        {
            var model = new Model("flatbed");
            if (!GameUtils.RequestModel(model)) return;
            _flatbed = Track(World.CreateVehicle(model, _shop + new Vector3(9f, -6f, 0f), Ctx.Locations.Heading("M11.ChopShop")));
            model.MarkAsNoLongerNeeded();
            if (_flatbed == null || !_flatbed.Exists()) return;
            _flatbed.IsPersistent = true;
            _flatbed.IsEngineRunning = false;
            var crateModel = new Model("prop_mil_crate_01");
            if (!GameUtils.RequestModel(crateModel)) return;
            _crate = Track(World.CreateProp(crateModel, _flatbed.Position + new Vector3(0f, 0f, 2f), false, false));
            crateModel.MarkAsNoLongerNeeded();
            if (_crate == null || !_crate.Exists()) { _crate = null; return; }
            _crate.IsPersistent = true;
            StowPropStep.Stow(_crate, _flatbed, new Vector3(0f, -3.1f, 1.05f));
        }

        protected override void OnPassed()
        {
            // The second engine stays at the shop for the fleet; the truck is the shop's now.
            Ctx.State?.SetCargo("turbineEngines", "M11.ChopShop");
            if (_flatbed != null && _flatbed.Exists()) Release(_flatbed);
            if (_crate != null && _crate.Exists()) Release(_crate);
        }

        protected override void OnCleanup()
        {
            Ctx.Crew.CompanionsHoldPosition = false;
            if (_granger != null && _granger.Exists()) _granger.IsPositionFrozen = false;
        }
    }

    /// <summary>
    /// Hold the throttle inside a pressure band for a sustained time. A skill check
    /// with no threat attached — the mission's only mechanic, and deliberately so.
    /// </summary>
    internal sealed class DynoObjective : Objective
    {
        private readonly System.Func<Vehicle> _vehicle;
        private readonly float _low;
        private readonly float _high;
        private readonly int _holdSeconds;

        private float _held;
        private int _lastTick;

        public DynoObjective(string label, System.Func<Vehicle> vehicle, float low, float high, int holdSeconds)
            : base(label)
        {
            _vehicle = vehicle;
            _low = low;
            _high = high;
            _holdSeconds = holdSeconds;
        }

        public override void Enter(MissionContext context)
        {
            _held = 0f; _lastTick = Game.GameTime;
        }

        public override void Update(MissionContext context)
        {
            var vehicle = _vehicle();
            if (vehicle == null || !vehicle.Exists())
            {
                Fail("The Granger is gone.");
                return;
            }

            float delta = (Game.GameTime - _lastTick) / 1000f;
            _lastTick = Game.GameTime;
            if (delta <= 0f || delta > 1f) delta = 0.016f;

            if (!IsOwnerActive(context) || vehicle.GetPedOnSeat(VehicleSeat.Driver) != Game.Player.Character) return;

            // Engine revs stand in for manifold pressure: the game models no boost, but
            // CurrentRPM is exactly the value the player is modulating with the trigger.
            float psi = vehicle.CurrentRPM * 40f;
            bool inBand = psi >= _low && psi <= _high;

            if (inBand) _held += delta;
            else _held = System.Math.Max(0f, _held - delta * 0.5f);

            string color = inBand ? "~g~" : psi < _low ? "~y~" : "~r~";
            GameUtils.Subtitle(color + psi.ToString("0") + " PSI~s~   target " + _low + "–" + _high, 400);
            GameUtils.DrawProgressBar(_held / _holdSeconds);

            if (_held >= _holdSeconds) Complete();
        }
    }
}

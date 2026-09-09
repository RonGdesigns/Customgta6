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
    /// This mission exists because the campaign needs somewhere to breathe. Its whole
    /// mechanic is holding a throttle in a band on the dyno — a skill check with no
    /// threat attached — and its whole payload is three men talking. If every mission
    /// is a firefight, the firefights stop meaning anything.
    /// </summary>
    public sealed class M11IroncladDyno : ComposedMission
    {
        private const float TargetBoostLow = 22f;
        private const float TargetBoostHigh = 28f;

        private Vehicle _granger;
        private Vector3 _shop;
        private Vector3 _dyno;

        public override string Id => "M11";
        public override string Title => "Ironclad Dyno";

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _shop = Ctx.Locations.Position("M11.ChopShop");
            _dyno = Ctx.Locations.Position("M11.DynoPad");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, _shop, Ctx.Locations.Heading("M11.ChopShop")))
            {
                return false;
            }

            ApplyBibleSetting();
            Ctx.Crew.CompanionsHoldPosition = true;
            SpawnGranger();
            if (!RequireAssets(_granger)) return false;
            Station(CrewSlot.Ice, _dyno + new Vector3(4f, 0f, 0f));
            Station(CrewSlot.Gohan, _shop + new Vector3(0f, 10f, 0f));
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
                    // profile to the Granger for the rest of the campaign.
                    /* Awarded once by CampaignState.MarkComplete after the mission passes. */
                    /* Awarded once by CampaignState.MarkComplete after the mission passes. */
                    GameUtils.Subtitle("~g~Exhaust temps green. The 3600LX is done.", 5000);
                })
                .AfterCues("M11_S1_02_ICE");

            yield return new MissionStage("Berth 44",
                    new DialogueFinishedObjective("Listen to Gohan's Berth 44 briefing."))
                
                .OnExit(context =>
                    GameUtils.Subtitle("~y~Three billion in cartel gold, sitting in Berth 44.", 6000))
                .WithCues("M11_S1_03_GUESS", "M11_S1_04_ICE", "M11_S1_05_GOHAN");
        }

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

            string colour = inBand ? "~g~" : psi < _low ? "~y~" : "~r~";
            GameUtils.Subtitle(colour + psi.ToString("0") + " PSI~s~   target " + _low + "–" + _high +
                               "   held " + _held.ToString("0") + "/" + _holdSeconds + "s", 400);

            if (_held >= _holdSeconds) Complete();
        }
    }
}

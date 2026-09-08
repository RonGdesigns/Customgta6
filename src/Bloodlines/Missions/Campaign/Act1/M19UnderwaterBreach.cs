using System;
using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M19 — "The Port Heist: Underwater Breach". Berth 44, 03:00, storm.
    ///
    /// Part one of four. Gohan takes the Kraken under the Titan Star, burns a
    /// rectangular breach into hold 3, and clamps ballast floats to the bullion
    /// container while Aegis drops depth charges on the water above him.
    ///
    /// Faked per docs/FEASIBILITY.md: the container does not float up on physics, it
    /// is a prop moved on a curve once the clamps are set. What the player does —
    /// hold position at depth, in the dark, while the water keeps detonating — is
    /// real, and that is the part the mission is actually about.
    /// </summary>
    public sealed class M19UnderwaterBreach : ComposedMission
    {
        private readonly List<Vector3> _clamps = new List<Vector3>();

        private Vehicle _kraken;
        private Vector3 _dive;
        private Vector3 _breach;
        private Vector3 _surface;

        public override string Id => "M19";
        public override string Title => "The Port Heist: Underwater Breach";

        protected override bool Setup()
        {
            _dive = Ctx.Locations.Position("M19.DiveStart");
            _breach = Ctx.Locations.Position("M19.HullBreach");
            _surface = Ctx.Locations.Position("M19.Surface");
            _clamps.Add(Ctx.Locations.Position("M19.ClampOne"));
            _clamps.Add(Ctx.Locations.Position("M19.ClampTwo"));

            if (!Ctx.Crew.DeploySolo(CrewSlot.Gohan, _dive, Ctx.Locations.Heading("M19.DiveStart")))
            {
                return false;
            }

            ApplyBibleSetting();
            SpawnKraken();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Dive",
                    new EnterVehicleObjective("Take the Kraken down.", () => _kraken, VehicleSeat.Driver))
                .PlayedBy(CrewSlot.Gohan)
                .WithDialogue(1);

            yield return new MissionStage("Cut the bulkhead",
                    new HoldZoneObjective("Burn the breach into hold 3.", () => _breach, 16, 8f,
                        "Acoustic torch cutting"),
                    new DepthChargeHazard(() => _breach))
                .PlayedBy(CrewSlot.Gohan)
                .OnExit(context => GameUtils.Subtitle("~g~Hull breached. Hold 3 is flooding.", 4000));

            yield return new MissionStage("Clamp the floats",
                    new MultiHoldObjective("Clamp the ballast floats to the container.",
                        _clamps, 8, 5f, "Clamping"),
                    new DepthChargeHazard(() => _breach))
                .PlayedBy(CrewSlot.Gohan)
                .WithDialogue(1);

            yield return new MissionStage("Surface",
                    new ReachZoneObjective("Surface to the boarding launch.", () => _surface, 12f))
                .PlayedBy(CrewSlot.Gohan)
                .OnExit(context =>
                    GameUtils.Subtitle("~g~She's rising. Guess — bring the Cargobob in now.", 6000));
        }

        private void SpawnKraken()
        {
            var model = new Model("submersible2");
            if (!GameUtils.RequestModel(model)) return;

            _kraken = Track(World.CreateVehicle(model, _dive + new Vector3(0f, -6f, -3f), 180f));
            model.MarkAsNoLongerNeeded();
            if (_kraken == null || !_kraken.Exists()) return;

            _kraken.IsPersistent = true;

            var blip = Track(_kraken.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "Kraken";
        }
    }

    /// <summary>
    /// Depth charges going off around the work. Passive: it cannot be completed, only
    /// survived, and it exists to make holding a position at depth cost something.
    /// Charges land near the player rather than on them — the threat is the pressure
    /// to leave, not a coin flip.
    /// </summary>
    internal sealed class DepthChargeHazard : Objective
    {
        private static readonly Random Random = new Random();

        private readonly Func<Vector3> _centre;
        private int _nextDrop;

        public DepthChargeHazard(Func<Vector3> centre) : base("")
        {
            _centre = centre;
        }

        public override bool IsPassive => true;

        public override void Enter(MissionContext context)
        {
            _nextDrop = Game.GameTime + 4000;
        }

        public override void Update(MissionContext context)
        {
            if (Game.GameTime < _nextDrop) return;

            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            var offset = new Vector3(Random.Next(-14, 14), Random.Next(-14, 14), Random.Next(2, 8));
            World.AddExplosion(player.Position + offset, ExplosionType.Boat, 3.5f, 1.2f, null, true, false);

            GameUtils.Subtitle("~r~Depth charges.", 1200);
            _nextDrop = Game.GameTime + Random.Next(5000, 9000);
        }
    }
}

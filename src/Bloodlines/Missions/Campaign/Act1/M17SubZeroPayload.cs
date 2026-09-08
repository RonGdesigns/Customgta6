using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M17 — "Sub-Zero Payload". Elysian dry docks, 13:00.
    ///
    /// The second quiet mission. Gohan and Guess weld plasma-arc torches and magnetic
    /// ballast grapples onto a civilian Kraken in a hidden slip — the tool that opens
    /// hold 3 of the Titan Star in M19.
    ///
    /// Three weld points, done in any order, then a grapple test. Nobody shoots at
    /// anybody. Act I is a heist build-up, and a build-up that is all firefights has
    /// no build in it.
    /// </summary>
    public sealed class M17SubZeroPayload : ComposedMission
    {
        private readonly List<Vector3> _weldPoints = new List<Vector3>();

        private Vehicle _kraken;
        private Vector3 _slip;

        public override string Id => "M17";
        public override string Title => "Sub-Zero Payload";

        protected override bool Setup()
        {
            _slip = Ctx.Locations.Position("M17.DrySlip");
            _weldPoints.Add(Ctx.Locations.Position("M17.WeldOne"));
            _weldPoints.Add(Ctx.Locations.Position("M17.WeldTwo"));
            _weldPoints.Add(Ctx.Locations.Position("M17.WeldThree"));

            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, _slip, Ctx.Locations.Heading("M17.DrySlip")))
            {
                return false;
            }

            ApplyBibleSetting();
            Ctx.Crew.CompanionsHoldPosition = true;
            SpawnKraken();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Calibrate the torches",
                    new MultiHoldObjective("Weld the plasma-arc torches to the hull.",
                        _weldPoints, 8, 3f, "Welding"))
                .OwnedBy(CrewSlot.Gohan)
                .WithDialogue(1);

            yield return new MissionStage("Grapple test",
                    new HoldZoneObjective("Guess — test the fifty-ton magnetic lock.",
                        () => _slip, 10, 4f, "Testing the ballast grapples"))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context =>
                {
                    context.State.SetUpgrade("krakenSubmarineReinforced", true);
                    GameUtils.Subtitle("~g~Four minutes through eight inches of naval bulkhead.", 5000);
                });

            yield return new MissionStage("Ready",
                    new ReachZoneObjective("Regroup at the slip.", () => _slip, 8f))
                .WithDialogue(1)
                .OnExit(context => GameUtils.Subtitle("~y~The sub is ready. Tomorrow night, Berth 44.", 5000));
        }

        private void SpawnKraken()
        {
            var model = new Model("submersible2");
            if (!GameUtils.RequestModel(model)) return;

            _kraken = Track(World.CreateVehicle(model, _slip + new Vector3(0f, 8f, 0f),
                Ctx.Locations.Heading("M17.DrySlip")));
            model.MarkAsNoLongerNeeded();
            if (_kraken == null || !_kraken.Exists()) return;

            _kraken.IsPersistent = true;
            _kraken.IsPositionFrozen = true;

            var blip = Track(_kraken.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "Kraken";
        }

        protected override void OnCleanup()
        {
            Ctx.Crew.CompanionsHoldPosition = false;
            if (_kraken != null && _kraken.Exists()) _kraken.IsPositionFrozen = false;
        }
    }
}

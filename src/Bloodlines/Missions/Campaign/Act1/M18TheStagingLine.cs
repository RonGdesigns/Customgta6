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
    /// hides the Cargobob in a salt hangar, Ice loads the anti-air launchers into the
    /// hauler, and then Ice says the thing about finishing what started.
    ///
    /// Three deliveries, one per character, and the player has to switch to make each
    /// one — the mission is a rehearsal for the four-part heist that follows, which is
    /// exactly what a staging mission should be.
    /// </summary>
    public sealed class M18TheStagingLine : ComposedMission
    {
        private Vehicle _kraken;
        private Vehicle _cargobob;
        private Vehicle _hauler;
        private Vector3 _channel;
        private Vector3 _hangar;
        private Vector3 _haulerMark;

        public override string Id => "M18";
        public override string Title => "The Staging Line";

        protected override bool Setup()
        {
            _channel = Ctx.Locations.Position("M18.ChannelMark");
            _hangar = Ctx.Locations.Position("M18.SaltHangar");
            _haulerMark = Ctx.Locations.Position("M18.HaulerMark");

            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, _hangar + new Vector3(0f, -20f, 0f), 0f)) return false;

            ApplyBibleSetting();

            SpawnAssets();
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
                .WithDialogue(1);

            yield return new MissionStage("Bird in the hangar",
                    new DeliverVehicleObjective("Guess — put the Cargobob in the salt hangar.",
                        () => _cargobob, () => _hangar, 30f))
                .OwnedBy(CrewSlot.Guess);

            yield return new MissionStage("Load the launchers",
                    new DeliverVehicleObjective("Ice — bring the hauler onto the line.",
                        () => _hauler, () => _haulerMark, 20f),
                    new HoldZoneObjective("Load the anti-air launchers.", () => _haulerMark, 10, 6f,
                        "Loading launchers"))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Countdown",
                    new ReachZoneObjective("Regroup on the line.", () => _haulerMark, 10f))
                .WithDialogue(1)
                .OnExit(context =>
                {
                    // Everything Act I has been buying is now in one place.
                    GameUtils.Subtitle("~y~Tonight we hit Berth 44, take thirty tons of their gold, " +
                                       "and finish what started.", 7000);
                });
        }

        private void SpawnAssets()
        {
            var krakenModel = new Model("submersible2");
            if (GameUtils.RequestModel(krakenModel))
            {
                _kraken = Track(World.CreateVehicle(krakenModel, _channel + new Vector3(-30f, 20f, 0f), 180f));
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
    }
}

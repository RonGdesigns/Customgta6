using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M22 — "The Port Heist: Scorched Bay". Alamo Sea, 04:30, sunrise smoke.
    ///
    /// The end of Act I. The bullion goes into four feet of Alamo water, the crew
    /// stands on the beach, and Aegis answers by putting a cruise missile through
    /// their Los Santos shop. Everything they built in twenty-two missions is gone in
    /// one shot, and the exile to Blaine County is not a choice.
    ///
    /// The airstrike is a cutscene, per docs/FEASIBILITY.md: a fade, a detonation at
    /// the foundry's real coordinates, and a smoke column the player can see from the
    /// beach. The mission does not try to simulate a cruise missile and does not need
    /// to — the loss is what has to land, not the ordnance.
    /// </summary>
    public sealed class M22ScorchedBay : ComposedMission
    {
        private Vehicle _cargobob;
        private Prop _container;
        private Vector3 _drop;
        private Vector3 _beach;

        public override string Id => "M22";
        public override string Title => "The Port Heist: Scorched Bay";

        protected override bool Setup()
        {
            _drop = Ctx.Locations.Position("M22.AlamoDrop");
            _beach = Ctx.Locations.Position("M22.Beach");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, _drop + new Vector3(-60f, -60f, 60f), 45f)) return false;

            ApplyBibleSetting();
            SpawnLift();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Bring it in",
                    new EnterVehicleObjective("Guess — fly the bullion into the Alamo.",
                        () => _cargobob, VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                .WithDialogue(1);

            yield return new MissionStage("Drop the container",
                    new ReachZoneObjective("Hold it over the northern shallows.", () => _drop, 30f,
                        flat: true, requireVehicle: true))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context =>
                {
                    ReleaseContainer();
                    // Thirty tons in four feet of water: the campaign's bank until M24
                    // starts dredging it back out.
                    context.State.AlamoGoldDredgedTons = 0f;
                    context.State.CashOnHand += 150000;
                });

            yield return new MissionStage("The beach",
                    new ReachZoneObjective("Regroup on the shore.", () => _beach, 12f))
                .OnExit(context => Airstrike(context));

            yield return new MissionStage("Blaine County",
                    new ReachZoneObjective("Load the trucks.", () => _beach, 25f, flat: true))
                .WithDialogue(1)
                .OnExit(context =>
                {
                    // Act I closes on a loss, not a payday, and the save records both.
                    context.State.Safehouses["cypressFoundry"] = false;
                    context.State.Save();
                    GameUtils.Subtitle("~y~When we come back to Los Santos, we come back as an army.", 7000);
                });
        }

        /// <summary>
        /// The strike on the Cypress foundry. The player is a hundred miles away, so
        /// what sells it is the fade, the sound, and a smoke column standing over the
        /// city when they look south.
        /// </summary>
        private void Airstrike(MissionContext context)
        {
            var foundry = context.Locations.Position("Base.CypressFlats");

            GameUtils.FadeOut(1200);
            Script.Wait(1300);

            World.AddExplosion(foundry, ExplosionType.Plane, 25f, 3f, null, true, false);
            World.AddExplosion(foundry + new Vector3(8f, 6f, 0f), ExplosionType.Tanker, 20f, 2.5f, null, true, false);

            GameUtils.FadeIn(2500);
            GameUtils.PlayFrontendSound("Explosion_Textured", "GTAO_Speed_Convoy_Soundset");
            GameUtils.Subtitle("~r~A black column over the mountains, where the shop used to be.", 7000);
            Logger.Info("M22: foundry destroyed at " + foundry);
        }

        private void ReleaseContainer()
        {
            if (_container == null || !_container.Exists()) return;

            Function.Call(Hash.DETACH_ENTITY, _container, true, true);
            _container.Position = _drop;
            _container.IsPositionFrozen = true;

            if (_cargobob != null && _cargobob.Exists()) _cargobob.EnginePowerMultiplier = 1f;

            GameUtils.Subtitle("~g~Thirty tons of bullion, sitting in four feet of Alamo water.", 6000);
        }

        private void SpawnLift()
        {
            var heliModel = new Model("cargobob");
            var containerModel = new Model("prop_container_01a");
            if (!GameUtils.RequestModel(heliModel)) return;

            _cargobob = Track(World.CreateVehicle(heliModel, _drop + new Vector3(-70f, -70f, 55f), 45f));
            heliModel.MarkAsNoLongerNeeded();
            if (_cargobob == null || !_cargobob.Exists()) return;

            _cargobob.IsPersistent = true;
            _cargobob.EnginePowerMultiplier = 0.6f;

            var blip = Track(_cargobob.AddBlip());
            blip.Sprite = BlipSprite.Helicopter;
            blip.Color = BlipColor.Orange;
            blip.Name = "Bullion lift";

            if (!GameUtils.RequestModel(containerModel)) return;

            _container = Track(World.CreateProp(containerModel, _cargobob.Position - new Vector3(0f, 0f, 7f),
                false, false));
            containerModel.MarkAsNoLongerNeeded();
            if (_container == null || !_container.Exists()) return;

            _container.IsPersistent = true;
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, _container, _cargobob, 0,
                0f, 0f, -6.5f, 0f, 0f, 0f, false, false, true, false, 2, true);
        }

        protected override void OnCleanup()
        {
            if (_container != null && _container.Exists())
            {
                Function.Call(Hash.DETACH_ENTITY, _container, true, true);
            }
        }
    }
}

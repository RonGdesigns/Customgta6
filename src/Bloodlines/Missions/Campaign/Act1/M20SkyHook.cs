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
    /// M20 — "The Port Heist: Sky Hook". Port outer basin, 03:20, under AA fire.
    ///
    /// Part two, and the campaign's first Red-tier set piece: thirty tons of bullion
    /// lifted out of the water by a Cargobob while Aegis gunners work the freighter
    /// deck.
    ///
    /// Faked exactly as docs/FEASIBILITY.md prescribes — attach, do not simulate. The
    /// container is attached to the helicopter rather than slung on a rope with a
    /// weight the physics would have to solve; the hover is a hold, the lift is an
    /// attach, and the climb-out is a flight the player actually flies. Everything
    /// the player controls is real. The only thing that is faked is the one thing the
    /// engine cannot do.
    /// </summary>
    public sealed class M20SkyHook : ComposedMission
    {
        private readonly List<Ped> _gunners = new List<Ped>();

        private Vehicle _cargobob;
        private Prop _container;
        private Vector3 _hover;
        private Vector3 _deck;
        private Vector3 _climbOut;

        public override string Id => "M20";
        public override string Title => "The Port Heist: Sky Hook";

        protected override bool Setup()
        {
            _hover = Ctx.Locations.Position("M20.HoverPoint");
            _deck = Ctx.Locations.Position("M20.DeckGunners");
            _climbOut = Ctx.Locations.Position("M20.ClimbOut");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, _hover + new Vector3(30f, 30f, -30f), 180f)) return false;

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.MG, 400, false, true);

            SpawnCargobob();
            SpawnContainer();
            SpawnDeckGunners();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get on the water",
                    new EnterVehicleObjective("Guess — take the Cargobob over the basin.",
                        () => _cargobob, VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                .WithDialogue(1);

            // Ice's job while Guess holds the hover: the two objectives are the two
            // characters, running at once, which is the whole argument for the switch.
            yield return new MissionStage("Suppress the deck",
                    new KillTargetsObjective("Ice — clear the AA gunners off the freighter.",
                        () => _gunners),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down."))
                .OwnedBy(CrewSlot.Ice)
                .WithDialogue(1);

            yield return new MissionStage("Lock the cable",
                    new HoldZoneObjective("Guess — hold the hover over the container.",
                        () => _hover, 8, 14f, "Winching"),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down."))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => AttachContainer());

            yield return new MissionStage("Climb out",
                    new ReachZoneObjective("Get the bullion over the cranes.", () => _climbOut, 60f,
                        flat: true, requireVehicle: true),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down."))
                .OnExit(context =>
                    GameUtils.Subtitle("~g~Thirty tons airborne. Gohan — get on our wing.", 5000));
        }

        /// <summary>
        /// The lift. Attaching the container to the aircraft is the whole trick: a
        /// thirty-ton slung load is a physics problem the game will lose, and an
        /// attached prop under the fuselage is one it always wins.
        /// </summary>
        private void AttachContainer()
        {
            if (_container == null || !_container.Exists() || _cargobob == null || !_cargobob.Exists()) return;

            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, _container, _cargobob, 0,
                0f, 0f, -6.5f, 0f, 0f, 0f, false, false, true, false, 2, true);

            // The aircraft flies heavy from here — the weight is a handling change, not
            // a simulated load.
            _cargobob.EnginePowerMultiplier = 0.55f;
            GameUtils.Subtitle("~y~Cable locked. She's heavy — nurse her.", 5000);
        }

        private void SpawnCargobob()
        {
            var model = new Model("cargobob");
            if (!GameUtils.RequestModel(model)) return;

            _cargobob = Track(World.CreateVehicle(model, _hover + new Vector3(25f, 25f, -25f), 180f));
            model.MarkAsNoLongerNeeded();
            if (_cargobob == null || !_cargobob.Exists()) return;

            _cargobob.IsPersistent = true;

            var blip = Track(_cargobob.AddBlip());
            blip.Sprite = BlipSprite.Helicopter;
            blip.Color = BlipColor.Orange;
            blip.Name = "Cargobob";
        }

        private void SpawnContainer()
        {
            var model = new Model("prop_container_01a");
            if (!GameUtils.RequestModel(model)) return;

            _container = Track(World.CreateProp(model, _hover - new Vector3(0f, 0f, 34f), false, false));
            model.MarkAsNoLongerNeeded();
            if (_container == null || !_container.Exists()) return;

            _container.IsPersistent = true;

            var blip = Track(_container.AddBlip());
            blip.Sprite = BlipSprite.Standard;
            blip.Color = BlipColor.Yellow;
            blip.Name = "Bullion container";
        }

        private void SpawnDeckGunners()
        {
            var model = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(model)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            for (int i = 0; i < 5; i++)
            {
                var gunner = World.CreatePed(model, _deck + new Vector3(-10f + i * 5f, 0f, 0f), 180f);
                if (gunner == null || !gunner.Exists()) continue;

                gunner.RelationshipGroup = aegis;
                gunner.IsPersistent = true;
                gunner.BlockPermanentEvents = true;
                gunner.Accuracy = 40;
                gunner.Armor = 60;
                gunner.Weapons.Give(WeaponHash.MG, 300, true, true);
                gunner.Task.FightAgainstHatedTargets(200f);

                _gunners.Add(Track(gunner));
            }

            model.MarkAsNoLongerNeeded();
        }

        protected override void OnCleanup()
        {
            if (_container != null && _container.Exists())
            {
                Function.Call(Hash.DETACH_ENTITY, _container, true, true);
            }

            _gunners.Clear();
        }
    }
}

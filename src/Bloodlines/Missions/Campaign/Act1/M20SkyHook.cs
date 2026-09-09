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
        private Vehicle _kraken;
        private Prop _container;
        private Vector3 _hover;
        private Vector3 _deck;
        private Vector3 _climbOut;

        public override string Id => "M20";
        public override string Title => "The Port Heist: Sky Hook";

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _hover = Ctx.Locations.Position("M20.HoverPoint");
            _deck = Ctx.Locations.Position("M20.DeckGunners");
            _climbOut = Ctx.Locations.Position("M20.ClimbOut");

            // Start on the ground at the crew's own staging hangar from M18: a
            // deployment over open water drops three people into the harbour.
            var apron = Ctx.Locations.Position("M18.SaltHangar");
            if (!Ctx.Crew.Deploy(CrewSlot.Guess, apron, Ctx.Locations.Heading("M18.SaltHangar")))
            {
                return false;
            }

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.MG, 400, false, true);

            SpawnCargobob();
            SpawnContainer();
            SpawnDeckGunners();
            if (!RequireAssets(_cargobob, _container)) return false;
            RequireAsset(_cargobob, "The Cargobob went down.");
            RequireAsset(_container, "The bullion container was lost.");
            Station(CrewSlot.Ice, Ctx.Locations.Position("M12.PierWatch"));
            // Chapter continuity: if M19 just ended, Gohan is still in the Kraken on
            // the surface, not standing on the apron beside Guess.
            var handoff = Ctx.Handoffs.Take(PortHeist.Operation, Id);
            if (handoff != null && SpawnKraken(handoff)) Station(CrewSlot.Gohan, _kraken, VehicleSeat.Driver);
            else Station(CrewSlot.Gohan, apron + new Vector3(-15f, 0f, 0f));
            Ctx.Crew.PedFor(CrewSlot.Ice).Weapons.Give(WeaponHash.HeavySniper, 100, true, true);
            return true;
        }

        private bool SpawnKraken(OperationHandoff handoff)
        {
            var model = new Model("submersible2");
            if (!GameUtils.RequestModel(model)) return false;
            var point = handoff.VehicleModel.Length > 0 ? handoff.VehiclePosition : Ctx.Locations.Position("M19.Surface");
            _kraken = Track(World.CreateVehicle(model, point, handoff.VehicleHeading));
            model.MarkAsNoLongerNeeded();
            if (_kraken == null || !_kraken.Exists()) return false;
            _kraken.IsPersistent = true;
            return true;
        }

        /// <summary>The lift is airborne with the container under it: that is what M21 must show.</summary>
        protected override void OnPassed()
        {
            var record = OperationHandoff.Capture(PortHeist.Operation, Id, "M21", Ctx.Crew, _cargobob);
            record.CargoAttached = _container != null && _container.Exists();
            record.CargoModel = "prop_container_01a";
            Ctx.Handoffs.Record(record);
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get on the water",
                    new EnterVehicleObjective("Guess — take the Cargobob over the basin.",
                        () => _cargobob, VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                ;

            // Ice's job while Guess holds the hover: the two objectives are the two
            // characters, running at once, which is the whole argument for the switch.
            yield return new MissionStage("Suppress the deck",
                    new KillTargetsObjective("Ice — clear the marked quayside gunners from the pier.",
                        () => _gunners),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down."))
                .OwnedBy(CrewSlot.Ice)
                
                .AfterCues("M20_S1_02_ICE");

            yield return new MissionStage("Lock the cable",
                    new MissionInteraction("Guess — hold the hover over the container.", () => _hover, 8, 14f, () => _cargobob),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down."))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => AttachContainer())
                .AfterCues("M20_S1_01_GUESS");

            yield return new MissionStage("Climb out",
                    new DeliverVehicleObjective("Guess: climb in the Cargobob to the elevated yellow marker.", () => _cargobob, () => _climbOut, 30f),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down."))
                .OnExit(context =>
                    GameUtils.Subtitle("~g~Thirty tons airborne. Gohan — get on our wing.", 5000))
                .AfterCues("M20_S1_03_GUESS");
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

            _cargobob = Track(World.CreateVehicle(model,
                Ctx.Locations.Position("M18.SaltHangar") + new Vector3(18f, 0f, 0f),
                Ctx.Locations.Heading("M18.SaltHangar")));
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

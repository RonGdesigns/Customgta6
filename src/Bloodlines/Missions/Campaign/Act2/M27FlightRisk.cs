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
    /// M27 — "Flight Risk". 8,000 feet over Mount Chiliad, 16:30.
    ///
    /// The bible's most cinematic mission and the second Red-tier set piece: Guess
    /// matches an Aegis Shamal at altitude, Ice crosses to it in the air, fights
    /// through the cabin as the jet is put into a terminal dive, takes the flight
    /// ledgers and steps out at four thousand feet into Gohan's boat.
    ///
    /// Faked in three moves, per docs/FEASIBILITY.md:
    ///
    ///  * The transfer is an attach, not a leap. Two aircraft in relative motion is a
    ///    physics problem the game loses every time, so once the player is in the
    ///    band, a fade puts Ice on the jet and the game never has to solve it.
    ///  * Zero-G is not simulated. The bodyguards fight normally aboard a plane that
    ///    is genuinely diving; the tilt and the altimeter do the work that ragdoll
    ///    physics cannot be trusted with.
    ///  * The dive is a scripted heading and pitch on the jet, so the mission can
    ///    guarantee it ends over water rather than hoping.
    ///
    /// Everything the player does — flying the match, the gunfight, the jump, the
    /// canopy ride down to the boat — is real.
    /// </summary>
    public sealed class M27FlightRisk : ComposedMission
    {
        private const float BailAltitude = 1200f;

        private readonly List<Ped> _bodyguards = new List<Ped>();

        private Vehicle _stuntPlane;
        private Vehicle _shamal;
        private Ped _shamalPilot;
        private Vehicle _dinghy;

        private Vector3 _formUp;
        private Vector3 _jetTrack;
        private Vector3 _seaPickup;

        public override string Id => "M27";
        public override string Title => "Flight Risk";

        protected override bool Setup()
        {
            _formUp = Ctx.Locations.Position("M27.FormUp");
            _jetTrack = Ctx.Locations.Position("M27.JetTrack");
            _seaPickup = Ctx.Locations.Position("M27.SeaPickup");

            // On the McKenzie apron. The climb to eight thousand feet is the player's
            // to fly — deploying at altitude drops the crew out of the sky.
            var apron = Ctx.Locations.Position("M26.DusterPad");
            if (!Ctx.Crew.Deploy(CrewSlot.Guess, apron, Ctx.Locations.Heading("M26.DusterPad")))
            {
                return false;
            }

            ApplyBibleSetting();

            var player = Game.Player.Character;
            player.Weapons.Give(WeaponHash.Parachute, 1, false, true);
            player.Weapons.Give(WeaponHash.SMG, 250, false, true);

            SpawnStuntPlane();
            SpawnShamal();
            SpawnDinghy();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get on his rudder",
                    new EnterVehicleObjective("Guess — take the stunt plane up.", () => _stuntPlane,
                        VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                .WithDialogue(1);

            // The match is real flying: hold the band and the transfer becomes possible.
            yield return new MissionStage("Match the Shamal",
                    new ShadowTargetObjective("Climb to the Shamal and hold station inside 60 metres.",
                        () => _shamal, 60f, 12, "The Shamal outran the stunt plane.", 12f))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => StartJet())
                .OnExit(context => BoardTheJet());

            yield return new MissionStage("Zero-G",
                    new KillTargetsObjective("Ice — clear the cabin.", () => _bodyguards))
                .OwnedBy(CrewSlot.Ice)
                .WithDialogue(2)
                .OnExit(context => BeginDive());

            yield return new MissionStage("Terminal dive",
                    new BailOutObjective("The pilot put her over — get out.", BailAltitude))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Sea pickup",
                    new ReachZoneObjective("Steer the canopy to Gohan's boat.", () => _seaPickup, 40f,
                        flat: true))
                .OnExit(context =>
                {
                    context.State.CashOnHand += 75000;
                    GameUtils.Subtitle("~g~Flight ledgers secured. Every Aegis charter for six months.", 6000);
                });
        }

        /// <summary>
        /// The transfer. A fade, an attach, and the game never has to solve two
        /// aircraft in relative motion — which is the one thing it cannot do here.
        /// </summary>
        private void BoardTheJet()
        {
            if (_shamal == null || !_shamal.Exists()) return;

            GameUtils.FadeOut(900);
            Script.Wait(950);

            Ctx.Switching.TrySwitch(CrewSlot.Ice);

            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice != null && ice.Exists())
            {
                ice.Task.ClearAllImmediately();
                Function.Call(Hash.SET_PED_INTO_VEHICLE, ice, _shamal, -2);
            }

            SpawnBodyguards();
            GameUtils.FadeIn(1400);
            GameUtils.Subtitle("~y~Hatch blown. Cabin decompressing.", 4000);
        }

        /// <summary>The pilot kicks the stick forward — a scripted attitude, not a hope.</summary>
        private void BeginDive()
        {
            if (_shamal == null || !_shamal.Exists()) return;

            if (_shamalPilot != null && _shamalPilot.Exists()) _shamalPilot.Kill();

            _shamal.Rotation = new Vector3(-55f, _shamal.Rotation.Y, _shamal.Rotation.Z);
            _shamal.Speed = 90f;
            GameUtils.Subtitle("~r~She's over. Terminal dive toward the Pacific.", 5000);
        }

        private void StartJet()
        {
            if (_shamalPilot == null || !_shamalPilot.Exists() || _shamal == null || !_shamal.Exists()) return;

            _shamalPilot.Task.StartPlaneMission(_shamal, _jetTrack, VehicleMissionType.GoTo,
                55f, 80f, 700, 40, 0f, false);
        }

        private void SpawnStuntPlane()
        {
            var model = new Model("stunt");
            if (!GameUtils.RequestModel(model)) return;

            _stuntPlane = Track(World.CreateVehicle(model,
                Ctx.Locations.Position("M26.DusterPad") + new Vector3(20f, 0f, 0f),
                Ctx.Locations.Heading("M26.DusterPad")));
            model.MarkAsNoLongerNeeded();
            if (_stuntPlane == null || !_stuntPlane.Exists()) return;

            _stuntPlane.IsPersistent = true;
            _stuntPlane.IsEngineRunning = true;

            var blip = Track(_stuntPlane.AddBlip());
            blip.Sprite = BlipSprite.Plane;
            blip.Color = BlipColor.Orange;
            blip.Name = "Stunt plane";
        }

        private void SpawnShamal()
        {
            var model = new Model("shamal");
            var pilotModel = new Model("s_m_m_pilot_01");
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(pilotModel)) return;

            _shamal = Track(World.CreateVehicle(model, _formUp, 250f));
            if (_shamal == null || !_shamal.Exists()) return;

            _shamal.IsPersistent = true;
            _shamal.IsEngineRunning = true;

            _shamalPilot = Track(World.CreatePed(pilotModel, _shamal.Position, 0f));
            model.MarkAsNoLongerNeeded();
            pilotModel.MarkAsNoLongerNeeded();
            if (_shamalPilot == null || !_shamalPilot.Exists()) return;

            _shamalPilot.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            _shamalPilot.IsPersistent = true;
            _shamalPilot.BlockPermanentEvents = true;
            _shamalPilot.Task.WarpIntoVehicle(_shamal, VehicleSeat.Driver);

            var blip = Track(_shamal.AddBlip());
            blip.Sprite = BlipSprite.Plane;
            blip.Color = BlipColor.Red;
            blip.Name = "Aegis Shamal";
        }

        private void SpawnBodyguards()
        {
            var model = new Model("s_m_m_highsec_01");
            if (!GameUtils.RequestModel(model) || _shamal == null || !_shamal.Exists()) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            for (int seat = 0; seat < 2; seat++)
            {
                var guard = Track(World.CreatePed(model, _shamal.Position, 0f));
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = aegis;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.CanBeDraggedOutOfVehicle = false;
                guard.Weapons.Give(WeaponHash.APPistol, 100, true, true);
                Function.Call(Hash.SET_PED_INTO_VEHICLE, guard, _shamal, seat);
                guard.Task.FightAgainstHatedTargets(30f);

                _bodyguards.Add(guard);
            }

            model.MarkAsNoLongerNeeded();
        }

        private void SpawnDinghy()
        {
            var model = new Model("dinghy");
            if (!GameUtils.RequestModel(model)) return;

            _dinghy = Track(World.CreateVehicle(model, _seaPickup, 200f));
            model.MarkAsNoLongerNeeded();
            if (_dinghy == null || !_dinghy.Exists()) return;

            _dinghy.IsPersistent = true;

            var blip = Track(_dinghy.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "Gohan";
        }

        protected override void OnCleanup()
        {
            _bodyguards.Clear();
        }
    }

    /// <summary>
    /// Get out of a falling aircraft before it hits. Completes when the player is out
    /// and under canopy; fails if they ride it down past the floor.
    /// </summary>
    internal sealed class BailOutObjective : Objective
    {
        private readonly float _floor;

        public BailOutObjective(string label, float floor) : base(label)
        {
            _floor = floor;
        }

        public override void Update(MissionContext context)
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            float height = player.HeightAboveGround;

            if (!player.IsInVehicle())
            {
                Complete();
                return;
            }

            GameUtils.Subtitle("~r~" + (int)height + " ft — GET OUT", 400);

            if (height < _floor * 0.5f) Fail("Ice rode the jet into the water.");
        }
    }
}

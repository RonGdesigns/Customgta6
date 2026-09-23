using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M59 — "The Wire Cutters". The Vinewood ridge, 10:00, clear and windy.
    ///
    /// Gohan overrides the state relay at the transmitter. Ice watches him from the Vinewood
    /// letters, five hundred meters east.
    ///
    /// This is the mission where the archives handed back the authored beat almost untouched.
    ///
    /// **Ice really is on the letters, on real climbable geometry.** Twelve maintenance ladders
    /// run the length of the sign — <c>ch2_03_ladder_mesh_02</c> through <c>_13</c>, from
    /// (690.4, 1201.8, 333.7) to (781.7, 1174.5, 332.3) — with <c>prop_vinewood_sign_01</c> at
    /// (774.66, 1175.66, 331.82) between them. His roost is the middle one at
    /// (736.58, 1194.27, 335.54), which is the one place on that sign a man can demonstrably
    /// get to and stand on. Rockstar put a way up there; the mission uses it rather than
    /// inventing one.
    ///
    /// **The transmitter is real too.** <c>prop_radiomast02</c> at (217.06, 1140.44, 230.26),
    /// with <c>prop_aircon_m_08</c> and <c>prop_aircon_m_02</c> around its base at 230.3 to
    /// 235.7 — a mast with plant at the foot of it, which is where a relay override happens.
    ///
    /// **Five hundred and twenty-two meters apart.** That is the distance from the roost to the
    /// mast, both on high ground, and it is a real marksman's overwatch rather than a figure of
    /// speech. It is also why this mission stations the two brothers half a kilometer apart at
    /// the start, which looks wrong in a diff and is the entire point.
    ///
    /// One adaptation, in `data/mission_gameplay.tsv`: **Gohan does not climb the mast.** A
    /// <c>prop_radiomast02</c> is a lattice with no ladder and no collision a ped can use. He
    /// works the relay at its base, among the plant that is actually placed there.
    /// </summary>
    public sealed class M59TheWireCutters : PreparationOperation
    {
        public const string GunshipModel = "buzzard";
        public const string PilotModel = "s_m_y_blackops_01";
        /// <summary>Aegis tries to put shooters on the sign, as M59_S1_02_ICE describes.</summary>
        public const int ShooterSeats = 3;
        /// <summary>How long the relay override takes. It is the whole state, not a door.</summary>
        public const int OverrideSeconds = 16;
        /// <summary>How far above and below the roost the sign's own surface is looked for.</summary>
        public const float RoostHeadroom = 4f;
        public const float RoostFloor = 320f;
        /// <summary>How often the roost probe is tried while the sign's collision streams in.</summary>
        public const int RoostProbeMs = 1000;
        /// <summary>How many tries before the authored height is kept and reported.</summary>
        public const int RoostProbeTries = 20;
        /// <summary>How far the gunship holds off over the ridge until the relay override starts.</summary>
        public const float HoldOffRadius = 80f;
        /// <summary>Where the campaign records the broadcast is out and repeating.</summary>
        public const string BroadcastEvidence = "aegisBroadcast";

        private readonly List<Ped> _shooters = new List<Ped>();
        private Vehicle _gunship;
        private Ped _pilot;
        private Vector3 _roost;
        private bool _onTheLetters, _broadcast, _skyClear, _roostSettled;
        private int _roostTries, _roostProbeAt;

        public override string Id => "M59";
        public override string Title => "The Wire Cutters";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        /// <summary>Ice is up on the sign.</summary>
        public bool OnTheLetters => _onTheLetters;
        /// <summary>The ledgers are on every public frequency.</summary>
        public bool Broadcast => _broadcast;
        /// <summary>The Aegis helicopter and its shooters are down.</summary>
        public bool SkyClear => _skyClear;
        public Vehicle Gunship => _gunship;
        public IReadOnlyList<Ped> Shooters => _shooters;
        /// <summary>The roost, on the surface the probe found rather than the ladder's origin.</summary>
        public Vector3 Roost => _roost;

        /// <summary>
        /// The roost is the top of a ladder on a sign three hundred and thirty meters up a
        /// hillside. Ground preparation would find the hill. Its height is still probed onto
        /// the real surface rather than trusted, because a ladder prop's origin is not a floor
        /// — which is the mistake that put M52's roost off the side of a building.
        /// </summary>
        protected override string[] FixedSurfaces => new[] { "M59.Roost", "M59.IceStart" };

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Gohan)) return false;

            // Not probed here. The roost is 522 m from Gohan, whom the player starts as, and a
            // shape test only answers where collision is loaded - so this probe always came
            // back empty and Ice was left on a ladder prop's origin (Ron, September 22). It is
            // measured once the sign's collision is in around Ice (SettleRoost).
            _roost = At("M59.Roost");

            // A marksman rifle, because the shot is 522 m and the line calls it a sniper round.
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice != null && ice.Exists()) ice.Weapons.Give(WeaponHash.SniperRifle, 40, false, true);

            SpawnGunship();
            // The objective later asks for every one of these, and an empty list fails it on
            // its first update with a message about a hostile that did not load. Refuse the
            // start instead, where the log can say why.
            if (_gunship == null || _shooters.Count == 0)
            {
                Logger.Error(Id + ": the Aegis helicopter could not be put in the air; there is nothing to clear from the sky. Refusing to start.");
                GameUtils.Notify("~r~The Aegis helicopter could not be placed. See Bloodlines.log.");
                return false;
            }

            Establish("approach", "Every television in the state",
                "The relay on the ridge reaches every set in San Andreas. Gohan works it at the mast; Ice watches the approach from the letters, five hundred meters east of him.",
                _gunship);
            return true;
        }

        /// <summary>
        /// The Aegis helicopter that tries to put shooters on the sign. Created in the air, so
        /// it goes through LaunchAirborne; its shooters ride it rather than being placed on the
        /// letters, because the only standing points up there are the ladders Ice is using.
        /// </summary>
        private void SpawnGunship()
        {
            var model = new Model(GunshipModel);
            var pilotModel = new Model(PilotModel);
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(pilotModel))
            { Logger.Warn(Id + ": the Aegis helicopter could not be requested; the sign stays quiet."); return; }
            var at = At("M59.Approach");
            _gunship = Track(World.CreateVehicle(model, at, Ctx.Locations.Heading("M59.Approach")));
            if (_gunship == null || !_gunship.Exists())
            { Logger.Warn(Id + ": the Aegis helicopter could not be created at " + at + "."); return; }
            _gunship.IsPersistent = true;
            AircraftHold.LaunchAirborne(_gunship);

            _pilot = Track(World.CreatePed(pilotModel, at, 0f));
            if (_pilot == null || !_pilot.Exists()) { GameUtils.SafeDelete(_gunship); _gunship = null; return; }
            _pilot.SetIntoVehicle(_gunship, VehicleSeat.Driver);
            if (_gunship.GetPedOnSeat(VehicleSeat.Driver) != _pilot)
            {
                Logger.Error(Id + ": the Aegis pilot could not be seated; removing that aircraft.");
                GameUtils.SafeDelete(_pilot); GameUtils.SafeDelete(_gunship);
                _pilot = null; _gunship = null;
                return;
            }
            _pilot.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            _pilot.IsPersistent = true;
            _pilot.BlockPermanentEvents = true;
            Opposition.Add(_pilot);
            _shooters.Add(_pilot);
            Blips.Attach(_pilot, BlipColor.Red, "Aegis helicopter");

            for (int i = 1; i < ShooterSeats; i++)
            {
                var seat = i == 1 ? VehicleSeat.LeftRear : VehicleSeat.RightRear;
                var rider = Occupant(_gunship, seat);
                if (rider == null) continue;
                Opposition.Add(rider);
                _shooters.Add(rider);
                Blips.Attach(rider, BlipColor.Red, "Aegis shooter");
            }

            // It holds off over its approach until the override starts. Sent at the sign from
            // the first frame, its riders were shooting at Ice before he had a stage to fight in.
            _pilot.Task.StartHeliMission(_gunship, at, VehicleMissionType.Circle,
                30f, HoldOffRadius, 350, 40, 0f, 0f, HeliMissionFlags.None);
            pilotModel.MarkAsNoLongerNeeded();
            model.MarkAsNoLongerNeeded();
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Ice: get up on the letters",
                new ReachZoneObjective("Ice: take the maintenance ladder up onto the sign", () => _roost, 5f))
                .OwnedBy(CrewSlot.Ice)
                .OnExit(c => _onTheLetters = true);

            yield return new MissionStage("Override the relay",
                new MissionInteraction("Gohan: override the state relay at the mast",
                    () => At("M59.Mast"), OverrideSeconds, 3f, animation: MissionInteraction.ReachInside)
                { RequiredCharacter = CrewSlot.Gohan },
                new ProtectObjective("", () => Ctx.Crew.PedFor(CrewSlot.Gohan),
                    "Gohan was killed at the mast before the broadcast went out."))
                .OnEnter(c => { Fighting = true; SendTheGunship(); })
                .OnExit(c => Broadcasting())
                .AfterCues("M59_S1_01_GOHAN");

            yield return new MissionStage("Clear the sky over the sign",
                new KillTargetsObjective("Take the Aegis helicopter and its shooters", () => _shooters))
                .AnyBrother()
                .OnExit(c => _skyClear = true)
                .AfterCues("M59_S1_02_ICE", "M59_S1_03_GOHAN");
        }

        /// <summary>The helicopter comes for the sign. Once, when the override begins.</summary>
        private void SendTheGunship()
        {
            if (_gunship == null || !_gunship.Exists() || _pilot == null || !_pilot.Exists() || _pilot.IsDead) return;
            _pilot.Task.StartHeliMission(_gunship, _roost, VehicleMissionType.Circle,
                30f, 60f, 350, 40, 0f, 0f, HeliMissionFlags.None);
            Logger.Info(Id + ": the Aegis helicopter is coming for the sign.");
        }

        /// <summary>
        /// The roost's real height, measured once the sign's collision has streamed in around
        /// Ice rather than from half a kilometer away in Setup. Ice, if he is still standing at
        /// the ladder origin he was placed on and the player is not holding him, is moved onto
        /// the surface it finds. Nothing found after <see cref="RoostProbeTries"/> keeps the
        /// authored height and says so.
        /// </summary>
        private void SettleRoost()
        {
            if (_roostSettled || Game.GameTime < _roostProbeAt) return;
            _roostProbeAt = Game.GameTime + RoostProbeMs;
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice == null || !ice.Exists()) return;
            if (!Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, ice) && ++_roostTries < RoostProbeTries) return;
            _roostSettled = true;
            var authored = At("M59.Roost");
            float? surface = MissionSites.SurfaceHeight(authored, authored.Z + RoostHeadroom, RoostFloor);
            if (!surface.HasValue)
            {
                Logger.Warn(Id + ": nothing solid under the sign roost at " + authored + "; keeping the authored height. Survey M59.Roost.");
                return;
            }
            _roost = new Vector3(authored.X, authored.Y, surface.Value);
            Logger.Info(Id + ": the sign roost is at " + surface.Value.ToString("0.00") + ", not the authored " + authored.Z.ToString("0.00") + ".");
            if (Ctx.Crew.ActiveSlot != CrewSlot.Ice && !ice.IsInVehicle() && GameUtils.IsWithinFlat(ice.Position, At("M59.IceStart"), 2f))
                ice.Position = _roost;
        }

        protected override void OnUpdate()
        {
            SettleRoost();
            base.OnUpdate();
        }

        private void Broadcasting()
        {
            _broadcast = true;
            Ctx.State?.SetEvidence(BroadcastEvidence, EvidenceState.CopyHeld);
            Logger.Info(Id + ": the offshore ledgers are on every public frequency in the state.");
            GameUtils.Subtitle("~g~The ledgers are out. Independent copies are already spreading.", 6000);
        }

        protected override void OnPassed()
        {
            if (!_onTheLetters || !_broadcast || !_skyClear)
                throw new InvalidOperationException("The roost, the broadcast and the sky all have to be settled.");
        }
    }
}

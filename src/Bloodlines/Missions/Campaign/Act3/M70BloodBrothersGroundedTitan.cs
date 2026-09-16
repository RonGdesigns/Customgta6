using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M70 — "Blood Brothers: Grounded Titan". The last one.
    ///
    /// The nose gear is gone and the plane will not fly. Three waves come at the fuselage on
    /// the apron, and when they are done Guess takes it off the end of the runway into the
    /// water anyway.
    ///
    /// The siege is <see cref="SurviveWavesObjective"/> with three waves, the same objective
    /// M60 uses, because it is the same shape done bigger. The apron is the open ground at the
    /// south-west of LSIA, which the survey puts at a hundred and forty-five meters clear at
    /// (-1710, -3080) — the widest thing at airport level and the only place a C-130 and a
    /// three-wave fight both fit.
    ///
    /// **The plane goes into the water and that is real.** A <c>titan</c> driven off the end of
    /// the runway into the Pacific is something the engine does; this is the one place in the
    /// campaign where the authored set piece and the engine want the same thing. The water
    /// point is depth-checked like any other, because a ditching into dry land is not an
    /// ending.
    ///
    /// **What is not reproduced:** the RPG strike that disables the nose gear happens before
    /// the mission opens rather than on screen, because a scripted hit on a specific gear leg
    /// is not something that can be aimed. The plane simply starts damaged and grounded, which
    /// is the state the synopsis needs and the same state either way. Recorded in
    /// `data/mission_gameplay.tsv`.
    ///
    /// Seven authored lines, and all seven fire. It is the last mission in the campaign; it
    /// gets its ending.
    /// </summary>
    public sealed class M70BloodBrothersGroundedTitan : PreparationOperation
    {
        public const string PlaneModel = "titan";
        public const string CommandoModel = "s_m_y_blackops_01";
        public const string GunshipModel = "buzzard";
        /// <summary>Three waves, as the synopsis sets them.</summary>
        public const int Waves = 3;
        /// <summary>Commandos in each wave.</summary>
        public const int PerWave = 6;
        /// <summary>How long between waves.</summary>
        public const int WaveGapMs = 10000;
        /// <summary>How far apart a wave's men are placed.</summary>
        public const float WaveSpread = 5f;
        /// <summary>How near the water counts as away.</summary>
        public const float SplashRadius = 40f;
        /// <summary>How deep the water has to be for a C-130 to ditch in it.</summary>
        public const float DitchDepth = 8f;
        /// <summary>Where the campaign records they got out.</summary>
        public const string FreeCargo = "bloodlinesEscape";

        private readonly List<Ped> _commandos = new List<Ped>();
        private Vehicle _plane;
        private Vector3 _water;
        private int _wavesSeen;
        private bool _held, _away;

        public override string Id => "M70";
        public override string Title => "Blood Brothers: Grounded Titan";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        /// <summary>How many waves have come.</summary>
        public int WavesSeen => _wavesSeen;
        /// <summary>The fuselage held through all three.</summary>
        public bool Held => _held;
        /// <summary>The plane is in the water and they are gone.</summary>
        public bool Away => _away;
        public Vehicle Plane => _plane;
        public IReadOnlyList<Ped> Commandos => _commandos;

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Ice)) return false;

            // A ditching into dry land is not an ending.
            if (!MissionSites.Water(Ctx.Locations, "M70.Water"))
            {
                GameUtils.Notify("~r~The water off the runway did not check out. See Bloodlines.log.");
                return false;
            }
            _water = MarineSites.ResolveOrThrow(Ctx.Locations, "M70.Water", DitchDepth);

            _plane = Car(PlaneModel, At("M70.Plane"), Ctx.Locations.Heading("M70.Plane"), true);
            if (!RequireAssets(_plane)) return false;
            _plane.IsPersistent = true;
            // Grounded, not destroyed. The RPG strike happened before the mission opened: a
            // scripted hit on one gear leg is not something that can be aimed, and the state
            // the synopsis needs is a plane that will not take off, which this is.
            _plane.IsEngineRunning = false;
            _plane.EngineHealth = Math.Min(_plane.EngineHealth, 400f);
            RequireAsset(_plane, "The cargo plane burned on the apron. There is no way off this runway.");

            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan, CrewSlot.Guess })
            {
                var ped = Ctx.Crew.PedFor(slot);
                if (ped == null || !ped.Exists()) continue;
                ped.Weapons.Give(slot == CrewSlot.Ice ? WeaponHash.MG : WeaponHash.CarbineRifle, 600, false, true);
            }

            Paleto.Review(Ctx, PlacementContract.Ped("M70.Start"),
                PlacementContract.Vehicle("M70.Plane", new Model(PlaneModel)));

            Establish("approach", "Tonight we hold this ground",
                "The nose gear is gone and the plane is not flying off this runway. Aegis and everybody else is coming across the apron, and there is nowhere left to be but here.",
                _plane);
            return true;
        }

        /// <summary>
        /// One wave onto the apron, spread so six men are not asked for the same square meter.
        /// The third wave brings a gunship with it, because the synopsis does.
        /// </summary>
        private IEnumerable<Ped> Wave(int index)
        {
            _wavesSeen = Math.Max(_wavesSeen, index);
            string key = "M70.Wave" + Math.Min(Math.Max(index, 1), Waves);
            var post = At(key);
            // Sized from the key, the same as M60's block: the apron is wide enough that
            // how many commandos come across it is a dial worth having.
            int men = MissionPlacement.Count(Ctx.Locations, key, PerWave);
            var wave = new List<Ped>();
            for (int i = 0; i < men; i++)
            {
                var ped = Guard(MissionPlacement.PointFor(Ctx.Locations, key, i,
                    post + new Vector3(i * WaveSpread, i % 2 * WaveSpread, 0f)));
                if (ped == null) continue;
                Opposition.Add(ped);
                Blips.Attach(ped, BlipColor.Red, "PMC commando");
                _commandos.Add(ped);
                wave.Add(ped);
            }
            if (index >= Waves)
            {
                var rider = Gunship(post);
                if (rider != null) wave.Add(rider);
            }
            if (wave.Count == 0) Logger.Error(Id + ": wave " + index + " could not be placed at " + post + ".");
            Logger.Info(Id + ": wave " + index + " is on the apron with " + wave.Count + ".");
            return wave;
        }

        /// <summary>The attack helicopter that comes with the last wave.</summary>
        private Ped Gunship(Vector3 near)
        {
            var model = new Model(GunshipModel);
            var pilotModel = new Model(CommandoModel);
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(pilotModel)) return null;
            var at = near + new Vector3(0f, 0f, 45f);
            var heli = Track(World.CreateVehicle(model, at, 0f));
            if (heli == null || !heli.Exists()) { Logger.Warn(Id + ": the last wave's gunship could not be created."); return null; }
            heli.IsPersistent = true;
            AircraftHold.LaunchAirborne(heli);
            var pilot = Track(World.CreatePed(pilotModel, at, 0f));
            model.MarkAsNoLongerNeeded();
            pilotModel.MarkAsNoLongerNeeded();
            if (pilot == null || !pilot.Exists()) { GameUtils.SafeDelete(heli); return null; }
            pilot.SetIntoVehicle(heli, VehicleSeat.Driver);
            if (heli.GetPedOnSeat(VehicleSeat.Driver) != pilot)
            {
                Logger.Error(Id + ": the gunship pilot could not be seated; removing that aircraft.");
                GameUtils.SafeDelete(pilot); GameUtils.SafeDelete(heli);
                return null;
            }
            pilot.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            pilot.IsPersistent = true;
            pilot.BlockPermanentEvents = true;
            var target = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (target != null && target.Exists())
                pilot.Task.StartHeliMission(heli, target, VehicleMissionType.Attack, 34f, 35f,
                    (int)Math.Max(heli.Position.Z, target.Position.Z + 40f), 25, -1f, 70f, (HeliMissionFlags)0);
            Opposition.Add(pilot);
            Blips.Attach(pilot, BlipColor.Red, "Federal gunship");
            _commandos.Add(pilot);
            return pilot;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Hold the fuselage",
                new SurviveWavesObjective("Hold the plane. Three waves.", Wave, Waves, WaveGapMs),
                new ProtectObjective("", () => _plane, "The plane burned before they could start it."))
                .AnyBrother()
                .OnEnter(c => Fighting = true)
                .OnExit(c => _held = true)
                .WithCues("M70_S1_01_ICE")
                .AfterCues("M70_S1_02_GOHAN");

            yield return new MissionStage("Start all four",
                new EnterVehicleObjective("Guess: get into the C-130 and start all four turboprops",
                    () => _plane, VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(c => { if (_plane != null && _plane.Exists()) _plane.IsEngineRunning = true; })
                .AfterCues("M70_S1_03_GUESS");

            // Off the end of it. The one place in this campaign where the authored set piece
            // and the engine want exactly the same thing.
            yield return new MissionStage("Off the seawall",
                new DeliverVehicleObjective("Guess: take the plane off the end of the runway into the water",
                    () => _plane, () => _water, SplashRadius))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(c => Gone())
                .AfterCues("M70_S1_04_ICE", "M70_S1_05_GOHAN", "M70_S1_06_GUESS", "M70_S1_07_ICE");
        }

        private void Gone()
        {
            _away = true;
            Ctx.State?.SetCargo(FreeCargo, "M70.Water");
            Logger.Info(Id + ": all three are off the runway and in the water. That is the campaign.");
            GameUtils.Subtitle("~g~All three of them. Let's go home.", 8000);
        }

        protected override void OnPassed()
        {
            if (!_held || _wavesSeen < Waves || !_away)
                throw new InvalidOperationException("All three waves have to have come, and the plane has to be in the water.");
            Release(_plane);
        }
    }
}

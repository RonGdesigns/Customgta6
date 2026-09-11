using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M14 — "Airspace Blackout". Sandy Shores auxiliary field, 18:00, sunset.
    ///
    /// Guess steals an Aegis ground-attack plane with a radar-jamming pod on the
    /// pylon; Ice covers the apron from a ridge with a thermal rifle. Getting out
    /// means staying under the SAM envelope all the way to McKenzie.
    ///
    /// Seen, not told: the aircraft with the pod on the apron and where it is
    /// going, before a shot; Ice and Gohan leaving the ridge in the Granger once
    /// Ron is airborne, because they do not share a seat the aircraft lacks; the
    /// low route with radio cues, not a cinematic that hides the ground; the pod
    /// itself at McKenzie, recorded where M18 and M20 can use it. The route is a
    /// hole in the radar for one window, not a blind Aegis.
    ///
    /// The escape is the mission. Flying low is a skill the game already has and the
    /// campaign has never asked for — a ceiling turns cruising altitude into a
    /// decision instead of a default.
    /// </summary>
    public sealed class M14AirspaceBlackout : ComposedMission
    {
        private readonly List<Ped> _apronGuards = new List<Ped>();

        private Vehicle _plane;
        private Vehicle _granger;
        private RoleTracks _roles;
        private Vector3 _ridge;
        private Vector3 _hangar;
        private Vector3 _mckenzie;
        private bool _ridgeLeft, _midwayCalled, _podStored;

        public override string Id => "M14";
        public override string Title => "Airspace Blackout";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        public Vehicle Plane => _plane;
        public Vehicle Granger => _granger;
        public RoleTracks Roles => _roles;
        public bool RidgeLeft => _ridgeLeft;
        public bool PodStored => _podStored;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _ridge = Ctx.Locations.Position("M14.OverwatchRidge");
            _hangar = Ctx.Locations.Position("M14.HangarDoor");
            _mckenzie = Ctx.Locations.Position("M14.McKenzieHangar");

            if (!Ctx.Crew.Deploy(CrewSlot.Ice, _ridge, Ctx.Locations.Heading("M14.OverwatchRidge")))
            {
                return false;
            }

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.HeavySniper, 40, true, true);

            SpawnApronGuards();
            SpawnPlane();
            SpawnGranger();
            if (!RequireAssets(_plane)) return false;
            Station(CrewSlot.Guess, _hangar + new Vector3(0f, -30f, 0f));
            Station(CrewSlot.Gohan, _ridge + new Vector3(15f, 0f, 0f));
            _roles = new RoleTracks(Ctx.Crew, () => _apronGuards);
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Overwatch",
                    new KillTargetsObjective("Ice — clear the apron from the ridge.", () => _apronGuards))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("The hangar",
                    new ReachZoneObjective("Guess — get to the hangar door.", () => _hangar, 8f))
                .OwnedBy(CrewSlot.Guess);

            yield return new MissionStage("Hotwire",
                    new EnterVehicleObjective("Guess: take the marked jammer aircraft.",
                        () => _plane, VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                .WithCues("M14_S1_01_GUESS");

            // The whole flight home is the objective: climb and the SAMs get a lock.
            // Ice and Gohan leave the ridge by road; the plane has one seat that matters.
            yield return new MissionStage("Under the radar",
                    new AltitudeCeilingObjective("Hug the terrain — stay under 50 meters above the terrain.", 50f,
                        "A SAM battery locked on and took the plane down."),
                    new DeliverVehicleObjective("Guess: land the jammer aircraft at McKenzie and stop.", () => _plane, () => _mckenzie, 60f, land: true),
                    new ReactionTrigger(() => !_ridgeLeft, LeaveRidge),
                    new ReactionTrigger(() => !_midwayCalled && Midway(), () => { _midwayCalled = true; Radio("GOHAN", "Sweep ahead of you, Ron. Stay in the dirt through the pass; it can't see below the ridgeline.", "M14_RADIO_02_GOHAN"); }))
                .OnExit(context => PlayPod())
                .WithCues("M14_S1_02_ICE");
        }

        // ---------- beats ----------

        /// <summary>The aircraft with the pod on the apron, the guards, Ron below the hangar, Ice on the ridge: the theft and its destination before a shot.</summary>
        private void PlayApproach()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var blocking = new SceneBlocking();
            if (_plane != null && _plane.Exists()) blocking.Then(new ShotStep(3400, _plane, new Vector3(-9f, -6f, 2.5f), _plane, new Vector3(2.5f, 0f, 0.5f), 0.8f));
            blocking.Then(new ShotStep(3000, null, _hangar + new Vector3(-14f, -10f, 3f), null, _hangar + new Vector3(0f, 6f, 1f), 0.7f));
            if (ice != null && ice.Exists()) blocking.Then(ShotStep.Watching(3000, ice, ice));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The apron",
                Reason = "The Besra on the apron with the jammer pod under its wing, five guards on the hangar, Ron below the doors, Ice on the ridge. The pod goes to McKenzie by the low route; the plane is only how it gets there.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M14 approach scene did not play; the ridge stands on its own.");
        }

        /// <summary>Ron is airborne: Ice and Gohan leave the ridge in the Granger by road. Nobody shares a seat the plane does not have.</summary>
        private void LeaveRidge()
        {
            _ridgeLeft = true;
            if (_granger != null && _granger.Exists())
            {
                _roles.For(CrewSlot.Ice).Extract(_granger.Position + new Vector3(2f, 0f, 0f));
                _roles.For(CrewSlot.Gohan).Extract(_granger.Position + new Vector3(-2f, 0f, 0f));
            }
            Radio("ICE", "We're off the ridge in the Granger. McKenzie by road; you'll beat us there.", "M14_RADIO_01_ICE");
        }

        private bool Midway()
        {
            if (_plane == null || !_plane.Exists()) return false;
            return _plane.Position.DistanceTo(_mckenzie) < _hangar.DistanceTo(_mckenzie) * 0.5f;
        }

        /// <summary>The pod at McKenzie: the plane stopped, Ron out, the pod under the wing, recorded where the heist can use it.</summary>
        private void PlayPod()
        {
            _podStored = true;
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var blocking = new SceneBlocking();
            if (guess != null && guess.Exists() && _plane != null && _plane.Exists() && guess.IsInVehicle(_plane)) blocking.Then(new ExitVehicleStep(guess));
            if (_plane != null && _plane.Exists()) blocking.Then(new ShotStep(3800, _plane, new Vector3(-4f, -5f, 1.3f), _plane, new Vector3(2.5f, 0f, 0.4f), 0.4f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "pod", Title = "The pod",
                Reason = "The jammer pod under the wing of a plane parked at McKenzie: a hole in Aegis radar for one window, not a blind Aegis. It waits here for the heist.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M14_S1_03_GUESS");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M14 pod scene did not play; the line plays as dialogue."); blocking.Complete(); Say("M14_S1_03_GUESS"); }
            Ctx.State?.SetCargo("radarPod", "M14.McKenzieHangar");
            if (_plane != null && _plane.Exists()) { _plane.IsEngineRunning = false; Release(_plane); }
            GameUtils.Subtitle("~g~Pod at McKenzie. One radar window for the lift, not a blind Aegis.", 6000);
        }

        /// <summary>The aftermath: the plane parked at McKenzie with the pod under its wing.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_plane == null || !_plane.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _plane, new Vector3(-10f, -8f, 3f), _plane, new Vector3(0f, 0f, 1f), 1.2f));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            _roles?.Update();
        }

        // ---------- world building ----------

        private void SpawnApronGuards()
        {
            var model = new Model("s_m_y_blackops_02");
            if (!GameUtils.RequestModel(model)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            for (int i = 0; i < 5; i++)
            {
                var guard = World.CreatePed(model, _hangar + new Vector3(-10f + i * 5f, 6f, 0f), 300f);
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = aegis;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 35;
                guard.Armor = 50;
                guard.Weapons.Give(WeaponHash.CarbineRifle, 200, true, true);
                guard.Task.GuardCurrentPosition();

                _apronGuards.Add(Track(guard));
            }

            model.MarkAsNoLongerNeeded();
        }

        private void SpawnPlane()
        {
            var model = new Model("besra");
            if (!GameUtils.RequestModel(model)) return;

            _plane = Track(World.CreateVehicle(model, Ctx.Locations.Position("M14.PlaneSpawn"),
                Ctx.Locations.Heading("M14.PlaneSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_plane == null || !_plane.Exists()) return;

            _plane.IsPersistent = true;

            var blip = Track(_plane.AddBlip());
            blip.Sprite = BlipSprite.Plane;
            blip.Color = BlipColor.Orange;
            blip.Name = "Jammer aircraft";
        }

        /// <summary>The crew's Granger on the ridge road: how Ice and Gohan leave.</summary>
        private void SpawnGranger()
        {
            var spot = _ridge + new Vector3(-10f, -14f, 0f);
            Vehicle granger = Ctx.Vans != null ? Ctx.Vans.Spawn(spot, 0f) : null;
            if (granger == null)
            {
                var model = new Model("granger");
                if (!GameUtils.RequestModel(model)) return;
                granger = World.CreateVehicle(model, spot, 0f);
                model.MarkAsNoLongerNeeded();
            }
            _granger = Track(granger);
            if (_granger == null || !_granger.Exists()) return;
            _granger.IsPersistent = true;
            _granger.IsEngineRunning = false;
        }

        protected override void OnCleanup()
        {
            _roles?.Release();
            _apronGuards.Clear();
        }
    }
}

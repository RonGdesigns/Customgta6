using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M23 — "Ghost in the Sage". Grand Senora radar facility, 08:00, desert dust.
    ///
    /// Act II opens with the crew homeless. A Cold War radar installation full of
    /// cartel squatters becomes the new base — three exterior storage bays for the heavy
    /// rigs, a generator room, and nobody within twenty miles.
    ///
    /// Seen, not told: the crew arriving from the Alamo in their own Granger with what
    /// they have left; a short exterior survey before anyone moves (Ice on the occupied
    /// approach, Ron looking for a second exit, Gohan on the utility problem); the
    /// clearing, each bay inspected for what is actually in it, the generator started
    /// by hand; the limits of the place (power, fuel, tools, cash) named as the reasons
    /// for the next jobs; a quiet walk through the yard to end on. Nobody calls it home.
    ///
    /// Structurally this is the mirror of M03: the mission that gives the act its
    /// home. Where M03 built the foundry with a crane and a hauler, this one takes a
    /// bunker off people who are already living in it.
    /// </summary>
    public sealed class M23GhostInTheSage : ComposedMission
    {
        private readonly List<Ped> _squatters = new List<Ped>();
        private readonly List<Vector3> _bays = new List<Vector3>();
        private readonly List<string> _limits = new List<string>();

        private Vehicle _granger;
        private RoleTracks _roles;
        private Vector3 _approach;
        private Vector3 _door;
        private Vector3 _secondExit;
        private Vector3 _generator;
        private bool _powered, _limitsShown;

        public override string Id => "M23";
        public override string Title => "Ghost in the Sage";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SafehouseArrival;

        public Vehicle Granger => _granger;
        public RoleTracks Roles => _roles;
        public IReadOnlyList<string> Limits => _limits;
        public bool Powered => _powered;
        public bool LimitsShown => _limitsShown;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _approach = Ctx.Locations.Position("M23.DomeApproach");
            _door = Ctx.Locations.Position("M23.BunkerDoor");
            _secondExit = Ctx.Locations.Position("M23.SecondExit");
            _generator = Ctx.Locations.Position("M23.Generator");
            _bays.Add(Ctx.Locations.Position("M23.BayOne"));
            _bays.Add(Ctx.Locations.Position("M23.BayTwo"));
            _bays.Add(Ctx.Locations.Position("M23.BayThree"));

            if (!Ctx.Crew.Deploy(CrewSlot.Ice, _approach, Ctx.Locations.Heading("M23.DomeApproach")))
            {
                return false;
            }

            ApplyBibleSetting();
            SpawnSquatters();
            SpawnGranger();
            Ctx.Crew.CompanionsHoldPosition = true;
            Station(CrewSlot.Guess, _approach + new Vector3(-6f, -6f, 0f));
            Station(CrewSlot.Gohan, _approach + new Vector3(6f, -6f, 0f));
            _roles = new RoleTracks(Ctx.Crew, () => _squatters);
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Breach the dome",
                    new ReachZoneObjective("Ice — get up to the radar dome walkway.", () => _door, 10f))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context =>
                {
                    // The other two cover the approach; nobody teleports into the yard.
                    _roles.For(CrewSlot.Guess).TakeCover(_approach + new Vector3(-8f, -4f, 0f));
                    _roles.For(CrewSlot.Gohan).TakeCover(_approach + new Vector3(8f, -4f, 0f));
                })
                .WithCues("M23_S1_01_ICE");

            yield return new MissionStage("Clear the radar yard",
                    new KillTargetsObjective("Clear the cartel squatters out.", () => _squatters))
                .OnEnter(context =>
                {
                    foreach (var squatter in _squatters)
                    {
                        if (squatter != null && squatter.Exists()) squatter.Task.FightAgainstHatedTargets(100f);
                    }
                });

            // Each bay is looked into for what is there, which is mostly nothing: the
            // limits of the place are learned bay by bay, not announced.
            yield return new MissionStage("Secure the bays",
                    new MultiHoldObjective("Guess — check the three storage bays.", _bays, 5, 4f,
                        "Checking the bay") { SiteDone = BayChecked })
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context =>
                {
                    _roles.For(CrewSlot.Ice).Observe(_door, _door);
                    _roles.For(CrewSlot.Gohan).Approach(_generator + new Vector3(-4f, 0f, 0f), _generator);
                })
                .AfterCues("M23_S1_02_GUESS");

            yield return new MissionStage("Power up",
                    new MissionInteraction("Gohan — bring the marked generator online.", () => _generator, 10, 4f))
                .OwnedBy(CrewSlot.Gohan)
                .OnEnter(context =>
                {
                    _roles.For(CrewSlot.Ice).Observe(_door, _door);
                    _roles.For(CrewSlot.Guess).Observe(_bays[2], _bays[2]);
                })
                .OnExit(context => PlayPower());

            // The quiet walk: through the usable yard to the door, the three of them.
            yield return new MissionStage("Walk the yard",
                    new ReachZoneObjective("Walk the yard to the bunker door.", () => _door, 4f))
                .OnEnter(context =>
                {
                    foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Guess, CrewSlot.Gohan })
                        if (slot != Ctx.Crew.ActiveSlot) _roles.For(slot).Approach(_door + new Vector3(slot == CrewSlot.Ice ? -3f : slot == CrewSlot.Guess ? 3f : 0f, -3f, 0f), _door);
                })
                .OnExit(context =>
                {
                    /* Awarded once by CampaignState.MarkComplete after the mission passes. */
                    GameUtils.Subtitle("~g~A place to work from in the desert. Not home; a door that locks and a way out.", 6000);
                });
        }

        // ---------- beats ----------

        /// <summary>The exterior survey: the Granger they came in, Ice reading the occupied approach, Ron looking for the second exit, Gohan on the utility problem. Nobody moves in yet.</summary>
        private void PlayApproach()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var blocking = new SceneBlocking();
            if (_granger != null && _granger.Exists()) blocking.Then(new ShotStep(3000, _granger, new Vector3(-7f, 4f, 2f), _granger, new Vector3(0f, 0f, 0.8f), 0.8f));
            if (ice != null && ice.Exists()) blocking.Then(ShotStep.Watching(2800, ice, _squatters.Count > 0 && _squatters[0].Exists() ? (Entity)_squatters[0] : ice));
            if (guess != null && guess.Exists()) blocking.Then(new WalkToStep(guess, _approach + new Vector3(-14f, 2f, 0f), 1.5f)).Then(new LookAtStep(guess, guess, 1200));
            if (gohan != null && gohan.Exists()) blocking.Then(new InspectStep(gohan, _approach + new Vector3(6f, -6f, 0f), 2600, "WORLD_HUMAN_BINOCULARS"));
            blocking.Then(ShotStep.Wide(2800, _generator + new Vector3(0f, 0f, 1f), 22f, 9f, 8f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The sage",
                Reason = "The Granger from the Alamo with what they have left, parked short of the dome. Ice reads the occupied approach; Ron walks the fence line looking for a second way out; Gohan looks at the generator house and the dead lines to it. The bunker is a place to clear, not a home.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M23 approach scene did not play; the approach stands on its own.");
        }

        /// <summary>A bay looked into: what is there, said once, and what is missing, kept for the list.</summary>
        private void BayChecked(int site)
        {
            string found;
            switch (site)
            {
                case 0: found = "Bay one: racks and a workbench, no tools on it."; _limits.Add("no tools"); break;
                case 1: found = "Bay two: one fuel drum, dry. The generator's tank is what there is."; _limits.Add("no fuel reserve"); break;
                default: found = "Bay three: room for the rigs, nothing in it."; break;
            }
            GameUtils.Subtitle("~y~" + found, 4000);
            Logger.Info("M23 " + found);
        }

        /// <summary>The generator started by hand, seen; then the limits of the place named as the reasons for the next jobs.</summary>
        private void PlayPower()
        {
            _powered = true;
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var blocking = new SceneBlocking();
            if (gohan != null && gohan.Exists()) blocking.Then(new InspectStep(gohan, _generator, 3000, "WORLD_HUMAN_WELDING"));
            blocking.Then(new ShotStep(3200, null, _generator + new Vector3(-5f, -4f, 2f), null, _generator + new Vector3(0f, 0f, 0.8f), 0.6f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "power", Title = "The generator",
                Reason = "Gohan starts the generator by hand and the yard has power for as long as the tank lasts. The bunker is usable from here; what it lacks is the list of the next jobs.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M23_S1_03_GOHAN");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M23 power scene did not play; the line plays as dialogue."); blocking.Complete(); Say("M23_S1_03_GOHAN"); }
            ShowLimits();
        }

        /// <summary>The specific reasons for what comes next: power on a tank, no fuel reserve, no tools, no operating cash. Not a shopping list.</summary>
        private void ShowLimits()
        {
            _limitsShown = true;
            _limits.Add("four hours of power on the tank");
            _limits.Add("no operating cash");
            Ctx.State?.SetUpgrade("bunkerGenerator", true);
            GameUtils.Notify("~y~The bunker's limits: " + string.Join("; ", _limits) + ". Cash first, from the Alamo; fuel and tools after.");
            Logger.Info("M23 limits: " + string.Join("; ", _limits));
        }

        /// <summary>The aftermath: the three at the door, the yard behind them, the lines over it.</summary>
        public override SceneBlocking OutroBlocking()
        {
            var blocking = new SceneBlocking();
            foreach (var slot in new[] { CrewSlot.Guess, CrewSlot.Gohan })
            {
                var ped = Ctx.Crew.PedFor(slot);
                if (ped != null && ped.Exists() && ped.Handle != Game.Player.Character.Handle) blocking.Then(new WalkToStep(ped, _door + new Vector3(slot == CrewSlot.Guess ? 2.5f : -2.5f, -2.5f, 0f), 1.5f));
            }
            return blocking.Then(ShotStep.Wide(4500, _door + new Vector3(0f, 0f, 1f), 12f, 4f, 5f));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            _roles?.Update();
        }

        // ---------- world building ----------

        private void SpawnSquatters()
        {
            var models = new[] { "g_m_y_mexgoon_01", "g_m_y_mexgoon_03", "g_m_y_mexgang_01" };
            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");

            for (int i = 0; i < 9; i++)
            {
                var model = new Model(models[i % models.Length]);
                if (!GameUtils.RequestModel(model)) continue;

                var post = i < 4
                    ? _door + new Vector3(-8f + i * 5f, 8f, 0f)
                    : _bays[(i - 4) % _bays.Count] + new Vector3((i % 2) * 4f - 2f, 5f, 0f);

                var squatter = World.CreatePed(model, post, 180f);
                model.MarkAsNoLongerNeeded();
                if (squatter == null || !squatter.Exists()) continue;

                squatter.RelationshipGroup = cartel;
                squatter.IsPersistent = true;
                squatter.BlockPermanentEvents = true;
                squatter.Accuracy = 30;
                squatter.Weapons.Give(i % 3 == 0 ? WeaponHash.AssaultRifle : WeaponHash.MicroSMG, 200, true, true);
                squatter.Task.GuardCurrentPosition();

                _squatters.Add(Track(squatter));
            }
        }

        /// <summary>The Granger they came in from the Alamo, parked short of the dome: the crew and what it has left.</summary>
        private void SpawnGranger()
        {
            var spot = _approach + new Vector3(-4f, -18f, 0f);
            Vehicle granger = Ctx.Vans != null ? Ctx.Vans.Spawn(spot, Ctx.Locations.Heading("M23.DomeApproach")) : null;
            if (granger == null)
            {
                var model = new Model("granger");
                if (!GameUtils.RequestModel(model)) return;
                granger = World.CreateVehicle(model, spot, Ctx.Locations.Heading("M23.DomeApproach"));
                model.MarkAsNoLongerNeeded();
            }
            _granger = Track(granger);
            if (_granger == null || !_granger.Exists()) return;
            _granger.IsPersistent = true;
            _granger.IsEngineRunning = false;
        }

        protected override void OnPassed()
        {
            if (_granger != null && _granger.Exists()) Release(_granger);
        }

        protected override void OnCleanup()
        {
            _roles?.Release();
            Ctx.Crew.CompanionsHoldPosition = false;
            _squatters.Clear();
        }
    }
}

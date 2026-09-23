using System.Collections.Generic;
using GTA.Native;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M15 — "Crawlspace". Port Authority administration, 23:30, rain.
    ///
    /// Gohan splices an optical tap into the harbor telemetry network while Ice puts
    /// the roving watchmen down with a stun gun. What it buys is control of the lock
    /// gates — which is what makes the Port Heist possible three missions later.
    ///
    /// Seen, not told: the service access, the watchmen on their rounds, the cable
    /// point, and Ron's Granger at the exit, before anyone moves in; the splice
    /// as work at a real cable point, Ice on a lookout; one gate answering the tap
    /// as the acknowledgment, not a harbor gone dark; the access recorded for M21
    /// and said to be three copies, one per device.
    ///
    /// Non-lethal by design: these are night-shift port employees, not Aegis. The
    /// mission fails on being seen, not on a body count, and the stun gun means
    /// the subdue objective is the only way to score it.
    /// </summary>
    public sealed class M15Crawlspace : ComposedMission
    {
        private readonly List<Ped> _watchmen = new List<Ped>();

        private readonly NonlethalGuards _nonlethal = new NonlethalGuards();
        /// <summary>A stun gun in every brother's hand, and nothing else for the two the player is not holding.</summary>
        private readonly NonlethalCrew _stunOnly = new NonlethalCrew();
        /// <summary>Charges on each brother's loaned stun gun.</summary>
        public const int StunRounds = NonlethalCrew.DefaultRounds;
        private Prop _office;
        private Prop _panel;
        private Vehicle _granger;
        private Vector3 _entry;
        private Vector3 _vault;
        private Vector3 _splice;
        private Vector3 _exit;
        private bool _tapped, _acknowledged;
        private bool _withdrawing;
        private int _nextWithdrawalOrder;

        public override string Id => "M15";
        public override string Title => "Crawlspace";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        public Prop Panel => _panel;
        public Vehicle Granger => _granger;
        public bool Tapped => _tapped;
        public bool Acknowledged => _acknowledged;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _entry = Ctx.Locations.Position("M15.AdminEntry");
            _vault = Ctx.Locations.Position("M15.MaintenanceVault");
            _splice = Ctx.Locations.Position("M15.FiberSplice");
            _exit = Ctx.Locations.Position("M15.Exit");

            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, _entry, Ctx.Locations.Heading("M15.AdminEntry")))
            {
                return false;
            }

            ApplyBibleSetting();
            Ctx.Abilities.Refill();

            SpawnWatchmen();
            SpawnPanel();
            SpawnGranger();
            if (!RequireAssets(_panel, _office, _granger) || _watchmen.Count != 3) return false;
            foreach (var guard in _watchmen) RequireSurvivor(guard, "The dock watchmen must survive. Use the stun gun and leave them alive.");
            Ctx.Crew.CompanionsHoldPosition = true;
            Station(CrewSlot.Ice, _granger, VehicleSeat.LeftRear);
            Station(CrewSlot.Gohan, _granger, VehicleSeat.RightRear);
            if (_granger != null && _granger.Exists()) Station(CrewSlot.Guess, _granger, VehicleSeat.Driver);
            else Station(CrewSlot.Guess, _exit + new Vector3(10f, 0f, 0f));
            // A dead watchman fails this mission at any stage, and M50 showed what a brother the
            // player is not holding does with a carbine once something sends him into a fight
            // (Ron, September 22). Gohan had a one-charge stun gun in his pocket, Guess had none,
            // and only Ice's was in his hand. All three carry one now, in hand, on loan.
            _stunOnly.Begin(Ctx.Crew, StunRounds);
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Leave the arrival car",
                    new ConditionObjective("Gohan: get out of Guess's car. Ice exits with you; Guess waits here for extraction.", () => !Game.Player.Character.IsInVehicle()))
                .OwnedBy(CrewSlot.Gohan)
                .OnEnter(context => { var ice=Ctx.Crew.PedFor(CrewSlot.Ice); if(ice!=null&&ice.Exists())ice.Task.LeaveVehicle(); });

            yield return new MissionStage("Maintenance level",
                    new ReachZoneObjective("Gohan: reach the marked maintenance access outside the administration building.", () => _vault, 5f),
                    new AvoidDetectionObjective(() => _watchmen,
                        "A watchman raised the alarm before the tap was in.", 30f, 4));

            yield return new MissionStage("Clear the rounds",
                    new SubdueTargetsObjective("Ice — put the watchmen down without killing them.",
                        () => _watchmen))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context =>
                    GameUtils.Subtitle("~y~Stun gun only. These are night-shift dock workers.", 5000))
                .AfterCues("M15_S1_02_ICE");

            yield return new MissionStage("Splice the trunk",
                    new MissionInteraction("Gohan — splice the optical bypass.", () => _splice, 12, 1.7f, animation: MissionInteraction.Repair, face: () => _panel.Position))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(context => PlaySplice())
                .WithCues("M15_S1_01_GOHAN");

            // One gate answers: that is the acknowledgment, and the reason to go.
            yield return new MissionStage("Out clean",
                    new ReachZoneObjective("Gohan: return to Guess at the original arrival car. Ice: cover the withdrawal.", () => _exit, 8f),
                    new EnterVehicleObjective("Board Guess's car. Use F / Y or the interact button at an empty passenger door.", () => _granger, VehicleSeat.Any),
                    new ConditionObjective("Get Ice and Gohan aboard. Switch freely to help either brother return to Guess.", () => Ctx.Crew.PedFor(CrewSlot.Ice).IsInVehicle(_granger) && Ctx.Crew.PedFor(CrewSlot.Gohan).IsInVehicle(_granger)),
                    new ReactionTrigger(() => _tapped && !_acknowledged && !Ctx.Cutscenes.IsActive, Acknowledge))
                .AnyBrother()
                .OnEnter(context => _withdrawing = true)
                .OnExit(context =>
                {
                    Ctx.State?.SetUpgrade("harborGateAccess", true);
                    GameUtils.Subtitle("~g~Lock-gate access, three copies, one per device. The gates answer us; the harbor does not go dark.", 6000);
                });
        }

        // ---------- beats ----------

        /// <summary>The service access, the rounds, the cable point and Ron's Granger at the exit: the way in and the way out before either is used.</summary>
        private void PlayApproach()
        {
            var blocking = new SceneBlocking()
                .Then(new ShotStep(3200, null, _entry + new Vector3(-8f, -10f, 2.5f), null, _entry + new Vector3(0f, 0f, 1f), 0.8f));
            if (_watchmen.Count > 0 && _watchmen[0].Exists()) blocking.Then(ShotStep.Watching(2800, _watchmen[0], _watchmen[0]));
            if (_panel != null && _panel.Exists()) blocking.Then(new ShotStep(3000, _panel, new Vector3(-1.6f, -1.8f, 1.1f), _panel, new Vector3(0f, 0f, 0.4f), 0.3f));
            else blocking.Then(ShotStep.Wide(3000, _splice, 5f, 2.5f, 2f));
            if (_granger != null && _granger.Exists()) blocking.Then(new ShotStep(2800, _granger, new Vector3(-6f, 3f, 1.8f), _granger, new Vector3(0f, 0f, 0.7f), 0.7f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The trunk",
                Reason = "The service access on the side of the administration building, a watchman on his round, the cable point one level down, and Ron's Granger at the exit. The tap opens the lock gates and nothing else.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M15 approach scene did not play; the entry stands on its own.");
        }

        /// <summary>The splice, seen at a real cable point, from Gohan's own line.</summary>
        private void PlaySplice()
        {
            _tapped = true;
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var blocking = new SceneBlocking();
            if (gohan != null && gohan.Exists())
            {
                blocking.Then(new InspectStep(gohan, _panel.Position, 1800));
                blocking.Then(ShotStep.OverShoulder(3400, gohan, _panel != null && _panel.Exists() ? (Entity)_panel : gohan, 0.2f));
            }
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "splice", Title = "The splice",
                Reason = "The optical bypass goes into the harbor trunk at the cable point. It maps the lock gates to Gohan's laptop; it does not shut the harbor down.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M15_S1_03_GOHAN");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M15 splice scene did not play; the line plays as dialogue."); blocking.Complete(); Say("M15_S1_03_GOHAN"); }
        }

        /// <summary>The test acknowledgment: one gate answers over the radio. Not a harbor gone dark.</summary>
        private void Acknowledge()
        {
            _acknowledged = true;
            Radio("GOHAN", "Gate one answered the tap. That's the acknowledgment: one gate, on our word. The rest of the harbor is still theirs.", "M15_RADIO_01_GOHAN");
        }

        /// <summary>The aftermath: Gohan at the exit with the Granger, Ice coming to it.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_granger != null && _granger.Exists())
                return new SceneBlocking().Then(new ShotStep(4500, _granger, new Vector3(-6f, 3f, 1.8f), _granger, new Vector3(0f, 0f, 0.8f), 1.0f));
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (gohan == null || !gohan.Exists()) return null;
            return new SceneBlocking().Then(ShotStep.Watching(4000, gohan, gohan));
        }

        // ---------- world building ----------

        private void SpawnWatchmen()
        {
            var model = new Model("s_m_m_security_01");
            if (!GameUtils.RequestModel(model)) return;

            var group = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            var posts = new[]
            {
                Ctx.Locations.Position("M15.Watchman1"),
                Ctx.Locations.Position("M15.Watchman2"),
                Ctx.Locations.Position("M15.Watchman3")
            };

            int postIndex = 0;
            foreach (var post in posts)
            {
                var watchman = World.CreatePed(model, MissionSites.Actor(Ctx.Locations, "M15.Watchman" + (postIndex + 1), post), Ctx.Locations.Heading("M15.Watchman" + (++postIndex)));
                if (watchman == null || !watchman.Exists()) continue;

                watchman.RelationshipGroup = group;
                watchman.IsPersistent = true;
                watchman.BlockPermanentEvents = true;
                _nonlethal.Add(watchman);
                watchman.Accuracy = 20;
                watchman.Weapons.Give(WeaponHash.Nightstick, 1, true, true);
                watchman.Task.StartScenario("WORLD_HUMAN_GUARD_PATROL", watchman.Position, watchman.Heading);

                _watchmen.Add(Track(watchman));
            }

            model.MarkAsNoLongerNeeded();
        }

        /// <summary>A cable point that exists: a case at the splice key.</summary>
        private void SpawnPanel()
        {
            var officeModel = new Model("prop_portacabin01");
            if (!GameUtils.RequestModel(officeModel)) return;
            _office = Track(World.CreateProp(officeModel, Ctx.Locations.Position("M15.ServiceOffice"), false, true));
            officeModel.MarkAsNoLongerNeeded();
            if (_office != null && _office.Exists()) { _office.IsPositionFrozen = true; _office.Heading = Ctx.Locations.Heading("M15.ServiceOffice"); }
            var model = new Model("prop_elecbox_12");
            if (!GameUtils.RequestModel(model)) return;
            _panel = Track(World.CreateProp(model, Ctx.Locations.Position("M15.Cabinet"), false, true));
            model.MarkAsNoLongerNeeded();
            if (_panel != null && _panel.Exists()) { _panel.IsPositionFrozen = true; _panel.Heading = Ctx.Locations.Heading("M15.Cabinet"); }
        }

        /// <summary>Ron's Granger at the exit, placed before the infiltration.</summary>
        private void SpawnGranger()
        {
            var spot = Ctx.Locations.Position("M15.GrangerSpawn");
            Vehicle granger = Ctx.Vans != null ? Ctx.Vans.Spawn(spot, Ctx.Locations.Heading("M15.GrangerSpawn")) : null;
            if (granger == null)
            {
                var model = new Model("granger");
                if (!GameUtils.RequestModel(model)) return;
                granger = World.CreateVehicle(model, spot, Ctx.Locations.Heading("M15.GrangerSpawn"));
                model.MarkAsNoLongerNeeded();
            }
            _granger = Track(granger);
            if (_granger == null || !_granger.Exists()) return;
            _granger.IsPersistent = true; _granger.PlaceOnGround();
            _granger.IsEngineRunning = false;
        }

        protected override void OnUpdate()
        {
            _stunOnly.Update();
            foreach (var slot in new[] { CrewSlot.Guess, CrewSlot.Ice, CrewSlot.Gohan })
                if (slot != Ctx.Crew.ActiveSlot) Ctx.Crew.CompanionAI.TakeControl(slot);
            if (_withdrawing && !Ctx.Cutscenes.IsActive && Game.GameTime >= _nextWithdrawalOrder)
            {
                _nextWithdrawalOrder = Game.GameTime + 4000;
                foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan })
                {
                    var brother = Ctx.Crew.PedFor(slot);
                    if (slot == Ctx.Crew.ActiveSlot || brother == null || !brother.Exists() || brother.IsDead || brother.IsInVehicle(_granger)) continue;
                    if (brother.Position.DistanceTo(_granger.Position) > 8f) brother.Task.RunTo(_granger.Position, false, 15000);
                    else
                    {
                        var seat = slot == CrewSlot.Ice ? VehicleSeat.LeftRear : VehicleSeat.RightRear;
                        // An entry already under way is left to finish: ordering it again on
                        // the four-second clock restarts the climb into the seat.
                        if (_granger.IsSeatFree(seat) && !Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE, brother))
                            brother.Task.EnterVehicle(_granger, seat, 8000, 2f);
                    }
                }
            }
            _nonlethal.Update();
            base.OnUpdate();
        }

        protected override void OnPassed()
        {
            if (_granger != null && _granger.Exists()) Release(_granger);
        }

        protected override void OnCleanup()
        {
            try { _stunOnly.End(); } catch (System.Exception ex) { Logger.Error("M15 stun gun release", ex); }
            Ctx.Crew.CompanionsHoldPosition = false;
            foreach (var slot in new[] { CrewSlot.Guess, CrewSlot.Ice, CrewSlot.Gohan }) Ctx.Crew.CompanionAI.ReleaseControl(slot);
            _nonlethal.Dispose();
            _watchmen.Clear();
        }
    }
}

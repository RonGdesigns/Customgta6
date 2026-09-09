using System.Collections.Generic;
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
    /// Gohan splices an optical tap into the harbour telemetry network while Ice puts
    /// the roving watchmen down with a tranquilliser. What it buys is control of the
    /// lock gates — which is what makes the Port Heist possible three missions later.
    ///
    /// Non-lethal by design: these are night-shift port employees, not Aegis. The
    /// mission fails on being seen, not on a body count, and the tranquilliser means
    /// the subdue objective is the only way to score it.
    /// </summary>
    public sealed class M15Crawlspace : ComposedMission
    {
        private readonly List<Ped> _watchmen = new List<Ped>();

        private Vector3 _entry;
        private Vector3 _vault;
        private Vector3 _splice;
        private Vector3 _exit;

        public override string Id => "M15";
        public override string Title => "Crawlspace";

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

            var player = Game.Player.Character;
            player.Weapons.Give(WeaponHash.StunGun, 1, false, true);

            SpawnWatchmen();
            foreach (var guard in _watchmen) RequireSurvivor(guard, "The dock watchmen must survive. Use the stun gun and leave them alive.");
            Ctx.Crew.CompanionsHoldPosition = true;
            Station(CrewSlot.Ice, _entry + new Vector3(-12f, 0f, 0f));
            Station(CrewSlot.Guess, _exit + new Vector3(10f, 0f, 0f));
            Ctx.Crew.PedFor(CrewSlot.Ice).Weapons.Give(WeaponHash.StunGun, 100, true, true);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Maintenance level",
                    new ReachZoneObjective("Gohan: reach the marked maintenance access outside the administration building.", () => _vault, 5f),
                    new AvoidDetectionObjective(() => _watchmen,
                        "A watchman raised the alarm before the tap was in.", 30f, 4))
                ;

            yield return new MissionStage("Clear the rounds",
                    new SubdueTargetsObjective("Ice — put the watchmen down without killing them.",
                        () => _watchmen))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context =>
                    GameUtils.Subtitle("~y~Stun gun only. These are night-shift dock workers.", 5000))
                .AfterCues("M15_S1_02_ICE");

            yield return new MissionStage("Splice the trunk",
                    new MissionInteraction("Gohan — splice the optical bypass.", () => _splice, 12, 3f))
                .OwnedBy(CrewSlot.Gohan)
                
                .OnExit(context =>
                    GameUtils.Subtitle("~g~Harbour lock gates and radar feeds are ours.", 5000))
                .WithCues("M15_S1_01_GOHAN")
                .AfterCues("M15_S1_03_GOHAN");

            yield return new MissionStage("Out clean",
                    new ReachZoneObjective("Leave the way you came in.", () => _exit, 8f))
                ;
        }

        private void SpawnWatchmen()
        {
            var model = new Model("s_m_m_security_01");
            if (!GameUtils.RequestModel(model)) return;

            var group = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            var posts = new[]
            {
                _entry + new Vector3(8f, 10f, 0f),
                _vault + new Vector3(-6f, 4f, 0f),
                _splice + new Vector3(7f, -5f, 0f)
            };

            foreach (var post in posts)
            {
                var watchman = World.CreatePed(model, post, 180f);
                if (watchman == null || !watchman.Exists()) continue;

                watchman.RelationshipGroup = group;
                watchman.IsPersistent = true;
                watchman.BlockPermanentEvents = true;
                watchman.Accuracy = 20;
                watchman.Weapons.Give(WeaponHash.Nightstick, 1, true, true);
                watchman.Task.StartScenario("WORLD_HUMAN_GUARD_PATROL", watchman.Position, 180f);

                _watchmen.Add(Track(watchman));
            }

            model.MarkAsNoLongerNeeded();
        }

        protected override void OnCleanup()
        {
            _watchmen.Clear();
        }
    }
}

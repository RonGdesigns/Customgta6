using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// SM01 — "Lead &amp; Kevlar". Terminal Island warehouse 4, 23:00, drizzle.
    ///
    /// The first solo mission, and the reference for how a composed mission is
    /// written: spawn the world in <see cref="Setup"/>, then describe the mission as
    /// stages of objectives. Compare against M01, which is the same amount of
    /// gameplay written as a bespoke state machine and four times the code.
    ///
    /// Ice goes after Sergei — an arms broker who took his money and shipped him
    /// civilian brass — for the armour-piercing 7.62 the crew needs before the
    /// evidence-vault job. No crew, no switching: solo missions are where a
    /// character's discipline stands alone.
    /// </summary>
    public sealed class SM01LeadAndKevlar : ComposedMission
    {
        private static readonly string[] GuardModels = { "g_m_m_armboss_01", "g_m_y_strpunk_01", "s_m_y_dealer_01" };

        private readonly List<Ped> _guards = new List<Ped>();

        private Ped _sergei;
        private Vector3 _warehouse;
        private Vector3 _office;
        private Vector3 _trunk;

        public override string Id => "SM01";
        public override string Title => "Lead & Kevlar";

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _warehouse = Ctx.Locations.Position("SM01.WarehouseGate");
            _office = Ctx.Locations.Position("SM01.SergeiOffice");
            _trunk = Ctx.Locations.Position("SM01.CrateLoad");

            if (!Ctx.Crew.DeploySolo(CrewSlot.Ice, _warehouse, Ctx.Locations.Heading("SM01.WarehouseGate")))
            {
                return false;
            }

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.PumpShotgun, 120, true, true);

            SpawnSergei();
            SpawnGuards();
            if (!RequireAssets(_sergei)) return false;
            if (_guards.Count != 6) return false;
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Breach",
                    new ReachZoneObjective("Breach the side entrance.", () => _office, 30f, flat: true))
                .PlayedBy(CrewSlot.Ice)
                
                .OnExit(context =>
                {
                    foreach (var guard in _guards)
                    {
                        if (guard != null && guard.Exists()) guard.Task.FightAgainstHatedTargets(80f);
                    }
                })
                .WithCues("SM01_S1_01_ICE");

            yield return new MissionStage("Clear the floor",
                    new KillTargetsObjective("Clear Sergei's men.", () => _guards))
                .PlayedBy(CrewSlot.Ice)
                .WithCues("SM01_S1_02_ICE");

            yield return new MissionStage("Sergei",
                    new MissionInteraction("Ice: approach Sergei and demand the crate codes", () => _sergei.Position, 4, 4f))
                .PlayedBy(CrewSlot.Ice)
                
                .OnEnter(context =>
                {
                    if (_sergei != null && _sergei.Exists()) _sergei.Task.HandsUp(30000);
                })
                .WithCues("SM01_S2_03_ENEMY", "SM01_S2_04_ICE");

            yield return new MissionStage("The crates",
                    new MissionInteraction("Load the AP crates.", () => _trunk, 4, 3f))
                .PlayedBy(CrewSlot.Ice)
                .OnExit(context => GameUtils.Subtitle("~g~Armour-piercing tungsten-core 7.62 secured.", 4000))
                .AfterCues("SM01_S2_05_ICE");
        }

        // ---------- world building ----------

        private void SpawnSergei()
        {
            var model = new Model("g_m_m_armboss_01");
            if (!GameUtils.RequestModel(model)) return;

            _sergei = Track(World.CreatePed(model, _office, 180f));
            model.MarkAsNoLongerNeeded();
            if (_sergei == null || !_sergei.Exists()) return;

            _sergei.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_TRAFFIC");
            _sergei.IsInvincible = true;
            _sergei.IsPersistent = true;
            _sergei.BlockPermanentEvents = true;
            _sergei.Armor = 50;
            _sergei.Weapons.Give(WeaponHash.Pistol, 60, true, true);

            var blip = Track(_sergei.AddBlip());
            blip.Sprite = BlipSprite.Enemy;
            blip.Color = BlipColor.Red;
            blip.Name = "Sergei";
        }

        private void SpawnGuards()
        {
            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");

            for (int i = 0; i < 6; i++)
            {
                var model = new Model(GuardModels[i % GuardModels.Length]);
                if (!GameUtils.RequestModel(model)) continue;

                var offset = new Vector3(-6f + i * 3f, 8f + (i % 3) * 6f, 0f);
                var guard = World.CreatePed(model, _warehouse + offset, 0f);
                model.MarkAsNoLongerNeeded();
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = cartel;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 30;
                guard.Weapons.Give(i % 2 == 0 ? WeaponHash.MicroSMG : WeaponHash.Pistol, 150, true, true);
                guard.Task.GuardCurrentPosition();

                _guards.Add(Track(guard));
            }
        }

        protected override void OnCleanup()
        {
            if (_sergei != null && _sergei.Exists()) _sergei.IsInvincible = false;
            _guards.Clear();
        }
    }
}

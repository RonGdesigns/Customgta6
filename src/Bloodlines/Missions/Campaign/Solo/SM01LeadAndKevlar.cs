using System.Collections.Generic;
using System.Drawing;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// SM01 — "Lead &amp; Kevlar". Terminal Island warehouse 4, 23:00, drizzle.
    ///
    /// The first of the nine solo missions, and the pattern for the rest: one
    /// character, no crew, no switching. Ice goes after Sergei — an arms broker who
    /// took his money and shipped him civilian brass — for the armour-piercing 7.62
    /// the crew needs before the evidence-vault job.
    ///
    /// Solo missions are where a character's discipline stands alone, so this one is
    /// deliberately close-quarters: no overwatch perch, no wheelman, just rooms.
    /// </summary>
    public sealed class SM01LeadAndKevlar : Mission
    {
        private static readonly string[] GuardModels = { "g_m_m_armboss_01", "g_m_y_strpunk_01", "s_m_y_dealer_01" };

        private readonly List<Ped> _guards = new List<Ped>();

        private Ped _sergei;
        private Vector3 _warehouse;
        private Vector3 _office;
        private Vector3 _trunk;
        private Blip _objectiveBlip;
        private bool _sergeiCornered;
        private bool _cratesTaken;

        public override string Id => "SM01";
        public override string Title => "Lead & Kevlar";

        protected override bool OnStart()
        {
            _warehouse = Ctx.Locations.Position("SM01.WarehouseGate");
            _office = Ctx.Locations.Position("SM01.SergeiOffice");
            _trunk = Ctx.Locations.Position("SM01.CrateLoad");

            if (!Ctx.Crew.DeploySolo(CrewSlot.Ice, _warehouse, Ctx.Locations.Heading("SM01.WarehouseGate")))
            {
                return false;
            }

            Ctx.Switching.SetLocked("Ice is working this one alone.");
            ApplyBibleSetting();

            var player = Game.Player.Character;
            player.Weapons.Give(WeaponHash.PumpShotgun, 120, true, true);

            SpawnSergei();
            SpawnGuards();

            Say("SM01_S1_01_ICE");
            Objective("Breach the warehouse and clear Sergei's guards.");
            SetObjectiveBlip(_office, "Sergei's office");
            return true;
        }

        protected override void OnUpdate()
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            switch (Stage)
            {
                case 0: UpdateBreach(player); break;
                case 1: UpdateClear(player); break;
                case 2: UpdateSergei(player); break;
                case 3: UpdateCrates(player); break;
            }
        }

        // ---------- Stage 0: the side entrance ----------

        private void UpdateBreach(Ped player)
        {
            GameUtils.DrawObjectiveMarker(_office, Color.FromArgb(120, 66, 133, 244), 2f);

            if (player.Position.DistanceTo(_warehouse) > 25f && !player.IsInCombat) return;

            Say("SM01_S1_02_ICE");
            foreach (var guard in _guards)
            {
                if (guard != null && guard.Exists()) guard.Task.FightAgainstHatedTargets(80f);
            }

            Objective("Clear the floor.");
            Advance();
        }

        // ---------- Stage 1: the floor ----------

        private void UpdateClear(Ped player)
        {
            _guards.RemoveAll(guard => guard == null || !guard.Exists() || guard.IsDead);

            GameUtils.Subtitle("~s~Sergei's men: ~r~" + _guards.Count, 500);
            if (_guards.Count > 0) return;

            Objective("Corner Sergei in the back office.");
            SetObjectiveBlip(_office, "Sergei");
            Advance();
        }

        // ---------- Stage 2: Sergei ----------

        private void UpdateSergei(Ped player)
        {
            if (_sergei == null || !_sergei.Exists())
            {
                Fail("Sergei got away with the shipment.");
                return;
            }

            if (_sergei.IsDead)
            {
                Say("SM01_S2_04_ICE");
                Objective("Load the AP crates into the trunk.");
                SetObjectiveBlip(_trunk, "Ammunition crates");
                Advance();
                return;
            }

            GameUtils.DrawObjectiveMarker(_sergei.Position, Color.FromArgb(130, 224, 74, 62), 0.8f);

            if (_sergeiCornered) return;
            if (player.Position.DistanceTo(_sergei.Position) > 12f) return;

            // He begs before he dies — the bible gives him one line and Ice one back.
            _sergeiCornered = true;
            _sergei.Task.HandsUp(30000);
            Say("SM01_S2_03_ENEMY");
        }

        // ---------- Stage 3: the crates ----------

        private void UpdateCrates(Ped player)
        {
            GameUtils.DrawObjectiveMarker(_trunk, Color.FromArgb(120, 106, 168, 122), 2f);

            if (!GameUtils.IsWithin(player.Position, _trunk, 3f)) return;

            if (!_cratesTaken)
            {
                _cratesTaken = true;
                Say("SM01_S2_05_ICE");
                GameUtils.Subtitle("~g~Armour-piercing tungsten-core 7.62 secured.", 4000);
                return;
            }

            if (!Ctx.Dialogue.IsSpeaking) Pass();
        }

        // ---------- world building ----------

        private void SpawnSergei()
        {
            var model = new Model("g_m_m_armboss_01");
            if (!GameUtils.RequestModel(model)) return;

            _sergei = Track(World.CreatePed(model, _office, 180f));
            model.MarkAsNoLongerNeeded();
            if (_sergei == null || !_sergei.Exists()) return;

            _sergei.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_CARTEL");
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

        private void SetObjectiveBlip(Vector3 position, string name)
        {
            GameUtils.SafeDelete(_objectiveBlip);
            _objectiveBlip = Track(World.CreateBlip(position));
            if (_objectiveBlip == null) return;

            _objectiveBlip.Sprite = BlipSprite.Standard;
            _objectiveBlip.Color = BlipColor.Yellow;
            _objectiveBlip.ShowRoute = true;
            _objectiveBlip.Name = name;
        }

        protected override void OnStageEntered(int stage)
        {
            if (stage >= 1)
            {
                foreach (var guard in _guards)
                {
                    if (guard != null && guard.Exists()) guard.Task.FightAgainstHatedTargets(80f);
                }
            }
        }

        protected override void OnCleanup()
        {
            GameUtils.SafeDelete(_objectiveBlip);
            _guards.Clear();
        }
    }
}

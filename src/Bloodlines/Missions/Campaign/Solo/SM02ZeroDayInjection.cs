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
    /// SM02 — "Zero-Day Injection". Lifeinvader data annex, Rockford, 02:00, fog.
    ///
    /// Gohan's solo, and the mod's first properly stealth mission: the guards are
    /// subdued rather than killed, so the objective scores them as down however they
    /// go down — stun gun, takedown or otherwise. Thermal Pulse is the intended tool
    /// and the mission gives him a full meter to use it with.
    /// </summary>
    public sealed class SM02ZeroDayInjection : ComposedMission
    {
        private readonly List<Ped> _guards = new List<Ped>();

        private Vector3 _roof;
        private Vector3 _serverBay;
        private Vector3 _terminal;
        private Vector3 _exit;

        public override string Id => "SM02";
        public override string Title => "Zero-Day Injection";

        protected override bool Setup()
        {
            _roof = Ctx.Locations.Position("SM02.RoofAccess");
            _serverBay = Ctx.Locations.Position("SM02.ServerBay");
            _terminal = Ctx.Locations.Position("SM02.Terminal");
            _exit = Ctx.Locations.Position("SM02.Exit");

            if (!Ctx.Crew.DeploySolo(CrewSlot.Gohan, _roof + new Vector3(0f, -14f, 0f),
                    Ctx.Locations.Heading("SM02.RoofAccess")))
            {
                return false;
            }

            ApplyBibleSetting();

            var player = Game.Player.Character;
            player.Weapons.Give(WeaponHash.StunGun, 1, true, true);
            player.Weapons.Give(WeaponHash.APPistol, 60, false, true);
            Ctx.Abilities.Refill();

            SpawnGuards();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Rooftop",
                    new ReachZoneObjective("Get onto the annex roof.", () => _roof, 4f))
                .PlayedBy(CrewSlot.Gohan)
                .WithDialogue(1);

            yield return new MissionStage("Server bay",
                    new SubdueTargetsObjective("Put the two guards down quietly.", () => _guards))
                .PlayedBy(CrewSlot.Gohan)
                .OnEnter(context =>
                    GameUtils.Subtitle("~y~Thermal Pulse (" + context.Config.AbilityKey + ") tracks them through the wall.", 5000));

            yield return new MissionStage("Root terminal",
                    new HoldZoneObjective("Inject the worm at the root terminal.", () => _terminal, 8, 2.5f,
                        "Splicing zero-day"))
                .PlayedBy(CrewSlot.Gohan)
                .WithDialogue(2)
                .OnExit(context =>
                {
                    // The payoff is mechanical, not narrative: from here the crew's
                    // plates are invisible to city surveillance.
                    Function.Call(Hash.SET_POLICE_RADAR_BLIPS, false);
                    GameUtils.Subtitle("~g~City surveillance is blind to the crew's plates.", 5000);
                });

            yield return new MissionStage("Fire escape",
                    new ReachZoneObjective("Down the fire escape before IT notices.", () => _exit, 6f))
                .PlayedBy(CrewSlot.Gohan);
        }

        private void SpawnGuards()
        {
            var model = new Model("s_m_m_security_01");
            if (!GameUtils.RequestModel(model)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            for (int i = 0; i < 2; i++)
            {
                var guard = World.CreatePed(model, _serverBay + new Vector3(i * 3f - 1.5f, 0f, 0f), 90f);
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = aegis;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 25;
                guard.Weapons.Give(WeaponHash.Pistol, 40, true, true);
                guard.Task.StartScenario("WORLD_HUMAN_GUARD_STAND", guard.Position, 90f);

                _guards.Add(Track(guard));
            }

            model.MarkAsNoLongerNeeded();
        }

        protected override void OnCleanup()
        {
            Function.Call(Hash.SET_POLICE_RADAR_BLIPS, true);
            _guards.Clear();
        }
    }
}

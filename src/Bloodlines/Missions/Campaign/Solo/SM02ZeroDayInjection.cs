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
    ///
    /// Seen, not told: the roof approach, the server bay and the terminal before
    /// he goes up, with the check-in time said out loud; the work at the terminal,
    /// and the result named for what it is (municipal camera archive access) and
    /// is not (the dock recording, a human witness); IT's trace as the reason to
    /// leave, on a clock; Gohan back on the radio.
    /// </summary>
    public sealed class SM02ZeroDayInjection : ComposedMission
    {
        public const int TraceSeconds = 75;

        private readonly List<Ped> _guards = new List<Ped>();

        private Prop _desk, _terminalProp;
        private Vector3 _roof;
        private Vector3 _serverBay;
        private Vector3 _terminal;
        private Vector3 _exit;
        private bool _tapLive;

        public override string Id => "SM02";
        public override string Title => "Zero-Day Injection";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        public Prop TerminalProp => _terminalProp;
        public bool TapLive => _tapLive;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
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
            SpawnTerminal();
            foreach (var guard in _guards) RequireSurvivor(guard, "The annex guards must survive. Stun them and leave without killing them.");
            if (_guards.Count != 2) return false;
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Rooftop",
                    new ReachZoneObjective("Get onto the annex roof.", () => _roof, 4f))
                .PlayedBy(CrewSlot.Gohan)
                .WithCues("SM02_S1_01_GOHAN");

            yield return new MissionStage("Server bay",
                    new SubdueTargetsObjective("Gohan: use the stun gun on both marked guards. Keep them alive.", () => _guards))
                .PlayedBy(CrewSlot.Gohan)
                .OnEnter(context =>
                    GameUtils.Subtitle("~y~Thermal Pulse (" + context.Config.AbilityKey + ") tracks them through the wall.", 5000))
                .WithCues("SM02_S1_02_GOHAN");

            yield return new MissionStage("Root terminal",
                    new MissionInteraction("Inject the worm at the root terminal.", () => _terminal, 8, 2.5f))
                .PlayedBy(CrewSlot.Gohan)
                .OnExit(context => PlayTerminal());

            // IT's trace is the reason to leave: a clock, and the fire escape.
            yield return new MissionStage("Fire escape",
                    new ReachZoneObjective("Down the fire escape before IT traces the connection.", () => _exit, 6f),
                    new TimerObjective(TraceSeconds, "IT traced the connection before Gohan was clear of the annex."))
                .PlayedBy(CrewSlot.Gohan)
                .OnExit(context =>
                {
                    Radio("GOHAN", "Clear of the annex. Returning. The archive is open; nothing else is.", "SM02_RADIO_01_GOHAN");
                    // Awarded once by CampaignState.MarkComplete after the mission passes:
                    // the Marksman Rifle in Gohan's locker.
                    GameUtils.Subtitle("~g~Gohan's locker stocks the Marksman Rifle from the next restock.", 5000);
                })
                .WithCues("SM02_S2_04_GOHAN");
        }

        // ---------- beats ----------

        /// <summary>The roof approach, the server bay door, the terminal: the narrow goal seen, and the check-in time said.</summary>
        private void PlayApproach()
        {
            var blocking = new SceneBlocking()
                .Then(new ShotStep(3200, null, _roof + new Vector3(-8f, -18f, 2f), null, _roof + new Vector3(0f, 0f, 1f), 0.9f))
                .Then(new ShotStep(3000, null, _serverBay + new Vector3(-5f, 4f, 1.8f), null, _serverBay + new Vector3(0f, 0f, 1f), 0.6f));
            if (_terminalProp != null && _terminalProp.Exists()) blocking.Then(new ShotStep(3000, _terminalProp, new Vector3(-1.4f, -1.6f, 0.9f), _terminalProp, new Vector3(0f, 0f, 0.1f), 0.3f));
            else blocking.Then(ShotStep.Wide(3000, _terminal, 4f, 2f, 1.5f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The annex",
                Reason = "The roof access on the service side, the server bay one floor down, the terminal at the back: municipal camera access and nothing else. Check-in in twenty minutes; Ron and Ice are listening, not coming.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("SM02 approach scene did not play; the roof stands on its own.");
        }

        /// <summary>The work at the terminal, and the result named for what it is and is not.</summary>
        private void PlayTerminal()
        {
            _tapLive = true;
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var blocking = new SceneBlocking();
            if (gohan != null && gohan.Exists())
            {
                blocking.Then(new InspectStep(gohan, _terminal, 3000, "WORLD_HUMAN_STAND_MOBILE"));
                blocking.Then(ShotStep.OverShoulder(3600, gohan, _terminalProp != null && _terminalProp.Exists() ? (Entity)_terminalProp : gohan, 0.2f));
            }
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "terminal", Title = "The tap",
                Reason = "The worm goes in at the root terminal; the municipal camera archive opens to the crew. The dock recording and a human witness are not touched by this.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("SM02_S2_03_GOHAN");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("SM02 terminal scene did not play; the line plays as dialogue."); blocking.Complete(); Say("SM02_S2_03_GOHAN"); }
            Ctx.State?.SetEvidence("cameraArchive", EvidenceState.CopyHeld);
            GameUtils.Subtitle("~g~Camera archive access: municipal feeds. The dock recording and the witness are untouched. IT will trace this: " + TraceSeconds + " s.", 6000);
        }

        /// <summary>The aftermath: the annex from the street, Gohan clear of it.</summary>
        public override SceneBlocking OutroBlocking()
        {
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (gohan == null || !gohan.Exists()) return null;
            return new SceneBlocking().Then(ShotStep.Watching(4000, gohan, gohan));
        }

        // ---------- world building ----------

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

        /// <summary>A terminal that exists: a table with a laptop on it, at the terminal key.</summary>
        private void SpawnTerminal()
        {
            var table = new Model("prop_table_03");
            var laptop = new Model("prop_laptop_01a");
            if (!GameUtils.RequestModel(table)) return;
            _desk = Track(World.CreateProp(table, _terminal, false, true));
            table.MarkAsNoLongerNeeded();
            if (_desk == null || !_desk.Exists() || !GameUtils.RequestModel(laptop)) return;
            _terminalProp = Track(World.CreateProp(laptop, PropPlacement.OnTop(_desk, new Model("prop_table_03"), laptop), false, false));
            laptop.MarkAsNoLongerNeeded();
            if (_terminalProp != null && _terminalProp.Exists()) _terminalProp.IsPositionFrozen = true;
        }

        protected override void OnCleanup()
        {
            _guards.Clear();
        }
    }
}

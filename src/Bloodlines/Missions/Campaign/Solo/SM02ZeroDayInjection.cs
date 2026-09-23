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
        private readonly NonlethalGuards _nonlethal = new NonlethalGuards();
        private bool _alarm;
        /// <summary>
        /// When each living guard was sent at Gohan, by ped handle. The partner used to be
        /// cleared and re-sent every three seconds, which restarts his combat task before he
        /// can shoot; he is sent once, and again only if the order has gone stale and he is
        /// visibly not fighting (the September 22 audit).
        /// </summary>
        private readonly Dictionary<int, int> _ordered = new Dictionary<int, int>();
        /// <summary>How old a combat order may get, with the guard not fighting, before it is given again.</summary>
        public const int GuardOrderStaleMs = 6000;
        /// <summary>Whether each stair transfer has been made. The transfer is the last thing its stage waits for.</summary>
        private bool _upStairs, _downStairs;
        public IReadOnlyList<Ped> Guards => _guards;
        public bool AlarmRaised => _alarm;

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
            // The arrival is distinct from the building's service door. Both stay
            // near their authored floor rather than snapping to an unrelated street.
            _upStairs = _downStairs = false; _ordered.Clear();
            var arrival = BoundedPlacement.Ped(Ctx.Locations, "SM02.Approach");
            Ctx.Locations.Get("SM02.StairEntry").Position = BoundedPlacement.Ped(Ctx.Locations, "SM02.StairEntry");
            Ctx.Locations.Get("SM02.Exit").Position = BoundedPlacement.Ped(Ctx.Locations, "SM02.Exit");
            if (GameUtils.IsWithinFlat(arrival, Ctx.Locations.Position("SM02.StairEntry"), 10f))
                throw new System.InvalidOperationException("SM02 approach must be at least ten meters from the service door. Correct the two survey points.");
            _roof = Ctx.Locations.Position("SM02.RoofAccess");
            _serverBay = Ctx.Locations.Position("SM02.ServerBay");
            _terminal = Ctx.Locations.Position("SM02.Terminal");
            _exit = Ctx.Locations.Position("SM02.Exit");

            if (!Ctx.Crew.DeploySolo(CrewSlot.Gohan, arrival,
                    Ctx.Locations.Heading("SM02.Approach")))
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
            // The stairs are part of the stage, not its exit. The transfer used to run from
            // OnExit and throw when the roof had not streamed, which ends the attempt as a
            // "Script error"; as the stage's last objective it either lands him on the roof
            // or fails the attempt with the reason, before the next stage can open
            // (the September 22 audit).
            var serviceDoor = new MissionInteraction("Gohan: run to the building's service door, then take the maintenance stairs to the roof.", () => Ctx.Locations.Position("SM02.StairEntry"), 2, 2.5f);
            yield return new MissionStage("Rooftop", serviceDoor,
                    new ConditionObjective("Gohan: up the maintenance stairs.", () => Stairs(serviceDoor, _roof, ref _upStairs,
                        "The maintenance stairs to the roof did not stream in. Retry the annex.")))
                .PlayedBy(CrewSlot.Gohan)
                .WithCues("SM02_S1_01_GOHAN");

            yield return new MissionStage("Server bay",
                    new SubdueTargetsObjective("Gohan: use the stun gun on both marked guards. Keep them alive.", () => _guards))
                .PlayedBy(CrewSlot.Gohan)
                // Gohan's ability is Blackout now; the sight through walls is Ice's. The
                // hint named the old one (the September 22 audit; the adaptation is in
                // data/mission_gameplay.tsv).
                .OnEnter(context =>
                    GameUtils.Subtitle("~y~Blackout (" + context.Config.AbilityKey + ") kills the lights and the biometrics, so they cannot make out what they see.", 5000))
                .WithCues("SM02_S1_02_GOHAN");

            yield return new MissionStage("Root terminal",
                    new MissionInteraction("Inject the worm at the root terminal.", () => _terminal, 8, 2.5f))
                .PlayedBy(CrewSlot.Gohan)
                .OnExit(context => PlayTerminal());

            // IT's trace is the reason to leave: a clock, and the fire escape.
            var roofAccess = new MissionInteraction("Gohan: return to the roof access and take the maintenance stairs down before IT traces you.", () => _roof, 2, 2.5f);
            yield return new MissionStage("Fire escape", roofAccess,
                    new ConditionObjective("Gohan: down the maintenance stairs.", () => Stairs(roofAccess, _exit, ref _downStairs,
                        "The maintenance stairs down to the street did not stream in. Retry the annex.")),
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

        /// <summary>
        /// The stair transfer once the door interaction is done: made once, and a transfer
        /// that did not land fails the attempt from inside the stage, where a failure stops
        /// the stage from completing.
        /// </summary>
        private bool Stairs(Missions.Objectives.Objective door, Vector3 destination, ref bool done, string failure)
        {
            if (done) return true;
            if (!door.IsFinished) return false;
            if (!TakeStairs(destination)) { Fail(failure); return false; }
            done = true;
            return true;
        }

        /// <summary>How long the stair transfer waits for collision at the far end before putting Gohan back.</summary>
        public const int StairStreamMs = 5000;

        /// <summary>
        /// The faded stair transfer. It used to throw after two seconds without collision,
        /// from a stage exit, which ends the attempt as a "Script error" - and two seconds
        /// is short for a roof that has not streamed. It waits longer, puts him back where
        /// he was if the far end never arrives, and says so (the September 22 audit).
        /// </summary>
        private bool TakeStairs(Vector3 destination)
        {
            var ped=Game.Player.Character;var origin=ped.Position;bool frozen=ped.IsPositionFrozen;
            bool moved=false;
            try
            {
                GameUtils.FadeOut(200);Script.Wait(250);ped.IsPositionFrozen=true;
                Function.Call(Hash.SET_FOCUS_POS_AND_VEL,destination.X,destination.Y,destination.Z,0f,0f,0f);
                ped.Position=destination;moved=true;
                for(int i=0;i<StairStreamMs/50;i++)
                {
                    Function.Call(Hash.REQUEST_COLLISION_AT_COORD,destination.X,destination.Y,destination.Z);
                    if(Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY,ped))
                    {Logger.Info("SM02: service stairs reached "+destination);return true;}
                    Script.Wait(50);
                }
                Logger.Warn("SM02: collision at the stair exit "+destination+" never streamed; Gohan stays where he was.");
                ped.Position=origin;moved=false;
                return false;
            }
            catch(System.Exception ex) { Logger.Error("SM02 stair transfer",ex); if(moved)ped.Position=origin; return false; }
            finally { ped.IsPositionFrozen=frozen;Function.Call(Hash.CLEAR_FOCUS);GameUtils.FadeIn(250); }
        }

        private void SpawnGuards()
        {
            var model = new Model("s_m_m_security_01");
            if (!GameUtils.RequestModel(model)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            for (int i = 0; i < 2; i++)
            {
                string key = "SM02.Guard" + (i + 1);
                var point = BoundedPlacement.Ped(Ctx.Locations, key);
                float facing = Ctx.Locations.Get(key).Status == LocationStatus.Surveyed ? Ctx.Locations.Heading(key) : DriveUpStep.HeadingBetween(_roof, point);
                var guard = World.CreatePed(model, point, facing);
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = aegis;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 25;
                guard.Weapons.Give(WeaponHash.Pistol, 40, true, true);
                _nonlethal.Add(guard);
                guard.Task.StartScenario("WORLD_HUMAN_GUARD_STAND", guard.Position, facing);

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

        protected override void OnUpdate()
        {
            _nonlethal.Update();
            // One guard dropping is an audible alarm to his partner. Only the
            // living, not-yet-subdued guard receives a combat task. Never revive or
            // retask the downed guard when the terminal stage starts.
            if (!_alarm && _nonlethal.DownCount > 0) { _alarm = true; _ordered.Clear(); }
            if (_alarm && !Ctx.Cutscenes.IsActive)
            {
                var player = Game.Player.Character;
                int now = Game.GameTime;
                foreach (var guard in _guards)
                {
                    if (guard == null || !guard.Exists() || guard.IsDead || _nonlethal.IsDown(guard))
                    { if (guard != null) _ordered.Remove(guard.Handle); continue; }
                    if (_ordered.TryGetValue(guard.Handle, out int at))
                    {
                        bool stale = now - at > GuardOrderStaleMs && !Function.Call<bool>(Hash.IS_PED_IN_COMBAT, guard, player);
                        if (!stale) continue;
                        guard.Task.FightAgainst(player);
                    }
                    else { guard.Task.ClearAll(); guard.Task.FightAgainst(player); }
                    _ordered[guard.Handle] = now;
                }
            }
            base.OnUpdate();
        }

        protected override void OnCleanup()
        {
            _nonlethal.Dispose();
            _guards.Clear();
        }
    }
}

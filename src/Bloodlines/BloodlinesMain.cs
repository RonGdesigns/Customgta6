using System;
using System.IO;
using System.Windows.Forms;
using Bloodlines.Abilities;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;

namespace Bloodlines
{
    /// <summary>
    /// Mod entry point — the bible's BloodlinesCore. SHVDN constructs this on load
    /// and again after every reload, so everything it owns must be re-creatable and
    /// every world change it makes must be undone in <see cref="OnAborted"/>.
    /// </summary>
    public sealed class BloodlinesMain : Script
    {
        private readonly ModConfig _config;
        private readonly CampaignData _data;
        private readonly MissionCatalog _catalog;
        private readonly CrewRoster _crew;
        private readonly SwitchController _switching;
        private readonly AbilityController _abilities;
        private readonly DialogueDirector _dialogue;
        private readonly CheckpointManager _checkpoints;
        private readonly MissionManager _missions;
        private readonly DeathController _death;
        private readonly FleetGarage _garage;
        private readonly DevMenu _menu;
        private readonly SurveyMode _survey;
        private readonly LocationBook _locations;
        private readonly CampaignState _state;

        private int _abortHeldSince;

        public BloodlinesMain()
        {
            string root = Path.Combine(BaseDirectory, "Bloodlines");
            Directory.CreateDirectory(root);

            _config = ModConfig.Load(Path.Combine(root, "Bloodlines.ini"));
            Logger.Configure(Path.Combine(root, "Bloodlines.log"), _config.VerboseLogging);
            Logger.Info("Los Santos: Bloodlines loading. Build " + typeof(BloodlinesMain).Assembly.ManifestModule.ModuleVersionId);

            string dataDirectory = Path.Combine(root, "data");
            _data = CampaignData.Load(dataDirectory);
            _locations = LocationBook.Load(dataDirectory, Path.Combine(root, "Bloodlines.Locations.ini"),
                Path.Combine(root, "Bloodlines.Surveyed.ini"), _data);
            _survey = new SurveyMode(_locations, Path.Combine(root, "Bloodlines.Surveyed.ini"),
                _config.DevCaptureKey.ToString(), _config.SurveyTeleportKey.ToString());
            _catalog = new MissionCatalog(_data, Path.Combine(root, "missions"));
            _state = CampaignState.Load(Path.Combine(dataDirectory, "savegame.json"));

            _crew = new CrewRoster(_config);
            _switching = new SwitchController(_crew);
            _abilities = new AbilityController(_config, _crew);
            _switching.BeforeSwitch = _abilities.Stop;
            _dialogue = new DialogueDirector(_data, root);
            _checkpoints = new CheckpointManager(_crew);
            _garage = new FleetGarage(_state);

            var context = new MissionContext(_config, _locations, _data, _crew, _switching,
                _abilities, _dialogue, _checkpoints, _state);
            _missions = new MissionManager(context, _state, _catalog);
            _death = new DeathController(_config, _crew, _missions, _abilities, _switching, _dialogue);
            _menu = new DevMenu(_config, _crew, _switching, _abilities, _missions, _catalog,
                _state, _dialogue, _data, _survey, _death);

            Interval = 0;
            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            Aborted += OnAborted;

            Logger.Info("Ready. " + _state.CompletedCount + "/" + _catalog.All.Count +
                        " complete. Deploy crew with " + _config.DeployCrewKey +
                        ", start a mission with " + _config.MissionStartKey + ".");
        }

        private void OnTick(object sender, EventArgs e)
        {
            // Each subsystem is stepped separately. Wrapping the whole tick in one
            // try/catch meant a fault in the first line stopped every line after it:
            // a crew-controller bug took missions, dialogue and the dev menu with it,
            // and the mod looked frozen rather than broken. A failing subsystem now
            // costs only itself.
            // Death runs first, and blocks the rest of the tick while it does. A
            // corpse must not be fed to the companion AI or ticked through a
            // mission objective for the frames it takes to stand back up.
            Step("death", _death.Update);
            if (_death.IsHandling) return;
            Step("crew", _crew.Update);
            Step("abilities", _abilities.Update);
            Step("garage", _garage.Update);
            Step("dialogue", _dialogue.Update);
            Step("missions", _missions.Update);
            Step("menu", _menu.Update);
            Step("survey", _survey.Update);
            Step("controller switch", HandleControllerSwitch);
            Step("abort hold", HandleAbortHold);
        }

        private static void Step(string name, Action step)
        {
            try
            {
                step();
            }
            catch (Exception ex)
            {
                Logger.Error("Tick failed in " + name, ex);
            }
        }

        /// <summary>
        /// Step through the crew in order. The direct binds default to the numpad,
        /// which a tenkeyless keyboard does not have; this needs one key and works
        /// on any board.
        /// </summary>
        private void CycleCrew(int direction)
        {
            var count = Enum.GetValues(typeof(CrewSlot)).Length;
            var next = (((int)_crew.ActiveSlot + direction) % count + count) % count;
            _switching.TrySwitch((CrewSlot)next);
        }

        /// <summary>
        /// Switching on a controller, using the gesture the game already has.
        ///
        /// GTA's own trio switch is: hold the character wheel (D-pad down on a pad,
        /// Left Alt on a keyboard), then pick a character. Both halves of that are
        /// reused here rather than invented -- SelectCharacterMichael/Franklin/Trevor
        /// are the wheel's own selection controls, already bound on every pad, and
        /// they map to Ice/Gohan/Guess in roster order. Holding the wheel and tapping
        /// left or right steps through the crew, for anyone who does not want to
        /// remember which brother is in which slot.
        ///
        /// The vanilla wheel is taken over rather than shared: left live it would
        /// swap the player to Michael and strand every ped this mod is tracking.
        /// </summary>
        private void HandleControllerSwitch()
        {
            if (!_config.ControllerSwitchEnabled || !_crew.IsDeployed) return;

            if (_config.SuppressVanillaSwitch)
            {
                Game.DisableControlThisFrame(GTA.Control.CharacterWheel);
                Game.DisableControlThisFrame(GTA.Control.SelectCharacterMichael);
                Game.DisableControlThisFrame(GTA.Control.SelectCharacterFranklin);
                Game.DisableControlThisFrame(GTA.Control.SelectCharacterTrevor);
                Game.DisableControlThisFrame(GTA.Control.SelectCharacterMultiplayer);
            }

            if (Game.IsControlJustPressed(GTA.Control.SelectCharacterMichael)) { _switching.TrySwitch(CrewSlot.Ice); return; }
            if (Game.IsControlJustPressed(GTA.Control.SelectCharacterFranklin)) { _switching.TrySwitch(CrewSlot.Gohan); return; }
            if (Game.IsControlJustPressed(GTA.Control.SelectCharacterTrevor)) { _switching.TrySwitch(CrewSlot.Guess); return; }

            if (!Game.IsControlPressed(GTA.Control.CharacterWheel)) return;
            if (Game.IsControlJustPressed(GTA.Control.ScriptPadRight)) CycleCrew(1);
            else if (Game.IsControlJustPressed(GTA.Control.ScriptPadLeft)) CycleCrew(-1);
        }

        private void HandleAbortHold()
        {
            if (_abortHeldSince == 0) return;

            if (Game.GameTime - _abortHeldSince > 1200)
            {
                _abortHeldSince = 0;
                if (_missions.IsRunning) _missions.Abort();
                else if (_crew.IsDeployed) StandDown();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_death.IsHandling) return;
            try
            {
                // The menu takes keys first while it is open, so its navigation never
                // doubles as a gameplay bind.
                if (_config.DevToolsEnabled)
                {
                    if (e.KeyCode == _config.DevMenuKey)
                    {
                        _menu.Toggle();
                        return;
                    }

                    if (_menu.HandleKey(e.KeyCode)) return;
                }

                if (HandleGameplayKey(e.KeyCode)) return;
                if (_config.DevToolsEnabled) HandleQaKey(e.KeyCode);
            }
            catch (Exception ex)
            {
                Logger.Error("Key handling failed", ex);
            }
        }

        private bool HandleGameplayKey(Keys key)
        {
            if (key == _config.SwitchIceKey) { _switching.TrySwitch(CrewSlot.Ice); return true; }
            if (key == _config.SwitchGohanKey) { _switching.TrySwitch(CrewSlot.Gohan); return true; }
            if (key == _config.SwitchGuessKey) { _switching.TrySwitch(CrewSlot.Guess); return true; }
            if (key == _config.SwitchNextKey) { CycleCrew(1); return true; }
            if (key == _config.SwitchPrevKey) { CycleCrew(-1); return true; }
            if (key == _config.AbilityKey) { _abilities.Toggle(); return true; }
            if (key == _config.MissionStartKey) { StartMission(); return true; }
            if (key == _config.DeployCrewKey) { ToggleDeployment(); return true; }
            if (key == _config.AbortKey && _abortHeldSince == 0) { _abortHeldSince = Game.GameTime; return true; }
            if (key == _config.DevCaptureKey && _survey.IsActive) { _survey.Capture(); return true; }
            if (key == _config.SurveyTeleportKey && _survey.IsActive) { _survey.TeleportToCurrent(); return true; }
            return false;
        }

        /// <summary>
        /// Track 4 of the bible: the in-engine debugging harness. Off unless
        /// [Dev] Enabled is set, because a stray Delete key mid-mission would
        /// otherwise rewind a player who never asked for a QA build. The toolkit's
        /// switch and ability binds are the gameplay defaults now, so only the
        /// stage-warp and checkpoint keys live here.
        /// </summary>
        private void HandleQaKey(Keys key)
        {
            switch (key)
            {
                case Keys.PageUp: _missions.WarpStage(1); break;
                case Keys.PageDown: _missions.WarpStage(-1); break;
                case Keys.Insert: _missions.CommitCheckpoint(); break;
                case Keys.Delete: _missions.RestoreCheckpoint(); break;
                case Keys.End: _survey.Skip(); break;
                case Keys.Home: _survey.Previous(); break;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == _config.AbortKey) _abortHeldSince = 0;
        }

        private void StartMission()
        {
            if (_missions.IsRunning) return;

            if (!_data.IsLoaded)
            {
                GameUtils.Notify("~r~No campaign data.~s~ Copy data/ into scripts/Bloodlines/data/.");
                return;
            }

            var next = _state.NextPlayable(_catalog);
            if (next == null)
            {
                GameUtils.Notify("~y~No scripted missions available.");
                return;
            }

            // Missions own their own deployment; a free-roam crew would fight the
            // mission's spawn, so stand it down first.
            if (_crew.IsDeployed) StandDown();

            if (_missions.Start(next))
            {
                GameUtils.Notify("~b~" + next.Id + "~s~ — " + next.Title + "~n~" + next.Info.Location +
                                 " · " + next.Info.Time);
            }
        }

        private void ToggleDeployment()
        {
            if (_survey.IsActive)
            {
                GameUtils.Subtitle("~y~Stop the survey before deploying the crew.", 3000);
                return;
            }
            if (_missions.IsRunning)
            {
                GameUtils.Subtitle("~r~Not during a mission.", 2000);
                return;
            }

            if (_crew.IsDeployed)
            {
                StandDown();
                return;
            }

            var player = Game.Player.Character;
            if (!_crew.Deploy(CrewSlot.Ice, player.Position, player.Heading))
            {
                GameUtils.Notify("~r~Crew failed to deploy — see Bloodlines.log.");
                return;
            }

            GameUtils.Notify("~b~Crew up.~s~ " +
                             _config.SwitchIceKey + "/" + _config.SwitchGohanKey + "/" + _config.SwitchGuessKey +
                             " to switch, " + _config.AbilityKey + " for ability.");
        }

        private void StandDown()
        {
            // The save's last-known-location is what lets a session resume in place.
            var player = Game.Player.Character;
            if (_crew.IsDeployed && player != null && player.Exists())
            {
                _state.RecordPosition(_crew.ActiveSlot, player.Position);
                Step("save position", _state.Save);
            }

            Step("stop ability", _abilities.Stop);
            Step("stop switching", _switching.Cancel);
            Step("clear dialogue", _dialogue.Clear);
            Step("reset garage", _garage.Reset);
            Step("dismiss crew", _crew.Dismiss);
            Step("release recovery", _death.Cancel);
            GameUtils.Notify("~y~Crew stood down.");
        }

        private void OnAborted(object sender, EventArgs e)
        {
            Logger.Info("Script aborting - tearing down.");
            Step("stop survey", _survey.Stop);
            Step("stop switching", _switching.Cancel);
            Step("mission shutdown", _missions.Shutdown);
            Step("stop ability", _abilities.Stop);
            Step("clear dialogue", _dialogue.Clear);
            Step("dismiss crew", _crew.Dismiss);
            Step("release recovery", _death.Cancel);
            Step("restore time", () => Game.TimeScale = 1f);
        }
    }
}

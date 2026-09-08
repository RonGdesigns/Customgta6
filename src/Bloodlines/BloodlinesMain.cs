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
        private readonly FleetGarage _garage;
        private readonly CampaignState _state;

        private int _abortHeldSince;

        public BloodlinesMain()
        {
            string root = Path.Combine(BaseDirectory, "Bloodlines");
            Directory.CreateDirectory(root);

            _config = ModConfig.Load(Path.Combine(root, "Bloodlines.ini"));
            Logger.Configure(Path.Combine(root, "Bloodlines.log"), _config.VerboseLogging);
            Logger.Info("Los Santos: Bloodlines loading.");

            var locations = LocationBook.Load(Path.Combine(root, "Bloodlines.Locations.ini"));
            string dataDirectory = Path.Combine(root, "data");
            _data = CampaignData.Load(dataDirectory);
            _catalog = new MissionCatalog(_data, Path.Combine(root, "missions"));
            _state = CampaignState.Load(Path.Combine(dataDirectory, "savegame.json"));

            _crew = new CrewRoster(_config);
            _switching = new SwitchController(_crew);
            _abilities = new AbilityController(_config, _crew);
            _dialogue = new DialogueDirector(_data, root);
            _checkpoints = new CheckpointManager(_crew);
            _garage = new FleetGarage(_state);

            var context = new MissionContext(_config, locations, _data, _crew, _switching,
                _abilities, _dialogue, _checkpoints, _state);
            _missions = new MissionManager(context, _state, _catalog);

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
            try
            {
                _crew.Update();
                _abilities.Update();
                _garage.Update();
                _dialogue.Update();
                _missions.Update();
                HandleAbortHold();
            }
            catch (Exception ex)
            {
                Logger.Error("Tick failed", ex);
            }
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
            try
            {
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
            if (key == _config.AbilityKey) { _abilities.Toggle(); return true; }
            if (key == _config.MissionStartKey) { StartMission(); return true; }
            if (key == _config.DeployCrewKey) { ToggleDeployment(); return true; }
            if (key == _config.AbortKey && _abortHeldSince == 0) { _abortHeldSince = Game.GameTime; return true; }
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
                _state.Save();
            }

            _abilities.Stop();
            _dialogue.Clear();
            _garage.Reset();
            _crew.Dismiss();
            GameUtils.Notify("~y~Crew stood down.");
        }

        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                Logger.Info("Script aborting — tearing down.");
                _missions.Shutdown();
                _abilities.Stop();
                _dialogue.Clear();
                _crew.Dismiss();
                Game.TimeScale = 1.0f;
            }
            catch (Exception ex)
            {
                Logger.Error("Teardown failed", ex);
            }
        }
    }
}

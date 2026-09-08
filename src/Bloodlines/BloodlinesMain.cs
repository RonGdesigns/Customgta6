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
    /// Mod entry point. SHVDN constructs this once when the script loads and again
    /// after every reload, so everything it owns must be re-creatable and every
    /// world change it makes must be undone in <see cref="OnAborted"/>.
    /// </summary>
    public sealed class BloodlinesMain : Script
    {
        private readonly ModConfig _config;
        private readonly CrewRoster _crew;
        private readonly SwitchController _switching;
        private readonly AbilityController _abilities;
        private readonly MissionManager _missions;
        private readonly CampaignProgress _progress;
        private readonly LocationBook _locations;

        private int _abortHeldSince;

        public BloodlinesMain()
        {
            string root = Path.Combine(BaseDirectory, "Bloodlines");
            Directory.CreateDirectory(root);

            _config = ModConfig.Load(Path.Combine(root, "Bloodlines.ini"));
            Logger.Configure(Path.Combine(root, "Bloodlines.log"), _config.VerboseLogging);
            Logger.Info("Los Santos: Bloodlines loading.");

            _locations = LocationBook.Load(Path.Combine(root, "Bloodlines.Locations.ini"));
            _progress = CampaignProgress.Load(Path.Combine(root, "Bloodlines.Progress.ini"));

            _crew = new CrewRoster(_config);
            _switching = new SwitchController(_crew);
            _abilities = new AbilityController(_config, _crew);

            var context = new MissionContext(_config, _locations, _crew, _switching, _abilities);
            _missions = new MissionManager(context, _progress);

            Interval = 0;
            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            Aborted += OnAborted;

            Logger.Info("Ready. " + _progress.CompletedCount + "/70 missions complete. " +
                        "Deploy crew with " + _config.DeployCrewKey + ", start a mission with " + _config.MissionStartKey + ".");
        }

        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                _crew.Update();
                _abilities.Update();
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
                if (e.KeyCode == _config.SwitchIceKey) _switching.TrySwitch(CrewSlot.Ice);
                else if (e.KeyCode == _config.SwitchGohanKey) _switching.TrySwitch(CrewSlot.Gohan);
                else if (e.KeyCode == _config.SwitchGuessKey) _switching.TrySwitch(CrewSlot.Guess);
                else if (e.KeyCode == _config.AbilityKey) _abilities.Toggle();
                else if (e.KeyCode == _config.MissionStartKey) StartMission();
                else if (e.KeyCode == _config.DeployCrewKey) ToggleDeployment();
                else if (e.KeyCode == _config.AbortKey && _abortHeldSince == 0) _abortHeldSince = Game.GameTime;
            }
            catch (Exception ex)
            {
                Logger.Error("Key handling failed", ex);
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == _config.AbortKey) _abortHeldSince = 0;
        }

        private void StartMission()
        {
            if (_missions.IsRunning) return;

            var next = _progress.NextPlayable();
            if (next == null)
            {
                GameUtils.Notify("~y~No scripted missions available.");
                return;
            }

            // Missions own their own deployment; a free-roam crew would fight the
            // mission's split spawn, so stand it down first.
            if (_crew.IsDeployed) StandDown();

            _missions.Start(next);
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
            _abilities.Stop();
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

using System;
using System.IO;
using System.Linq;
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
        private readonly CharacterWheel _characterWheel;
        private readonly CampaignData _data;
        private readonly MissionCatalog _catalog;
        private readonly CrewRoster _crew;
        private readonly SwitchController _switching;
        private readonly AbilityController _abilities;
        private readonly DialogueDirector _dialogue;
        private readonly CheckpointManager _checkpoints;
        private readonly MissionManager _missions;
        private readonly CutsceneDirector _cutscenes;
        private readonly MissionMarkers _missionMarkers;
        private readonly DeathController _death;
        private readonly FleetGarage _garage;
        private readonly WorldTuning _worldTuning = new WorldTuning();
        private readonly TacticalResponse _tactics = new TacticalResponse();
        private readonly MissionHandoff _handoff = new MissionHandoff();
        private readonly ShopService _shops;
        private readonly CrewHomes _homes;
        private readonly CampaignDispatches _dispatches;
        private readonly WeaponProgression _weapons;
        private readonly CrewMemory _memory;
        private readonly DevMenu _menu;
        private readonly SurveyMode _survey;
        private readonly LocationBook _locations;
        private readonly CampaignState _state;
        private readonly PrologueSequence _prologue;

        private int _abortHeldSince;
        private CrewSlot? _controllerSelection;
        private bool _controllerWheelHeld;

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
            _dispatches = new CampaignDispatches(_state);

            CrewAppearance.Load(Path.Combine(root, "Bloodlines.Appearance.ini"));
            _crew = new CrewRoster(_config);
            _memory = new CrewMemory(_state);
            _weapons = new WeaponProgression(_state); _crew.Arsenal = _weapons;
            _homes = new CrewHomes(_crew, _state, _locations, _weapons);
            _crew.CompanionAI.Life.HomeDestination = _homes.Position;
            _crew.CompanionAI.Driver.MissionDestination = ObjectiveMarkers.DestinationFor;
            _characterWheel = new CharacterWheel(Path.Combine(root, "ui"));
            _switching = new SwitchController(_crew);
            _switching.ExternalBlockReason = () => _death != null && _death.IsHandling ? "Move a few steps to finish recovering before switching." : _homes.Apartment.Inside || _homes.Apartment.Busy ? "Exit the apartment before switching characters." : null;
            _abilities = new AbilityController(_config, _crew);
            _switching.BeforeSwitch = () => { _abilities.Stop(); CrewAppearance.Leave(_crew.ActiveSlot); };
            _switching.OnDistantHandover = (slot, ped) => CrewAppearance.ChangeAfterAbsence(ped, slot,
                !_missions.IsRunning && !_cutscenes.IsActive && !_survey.IsActive &&
                Game.Player.Character != null && !Game.Player.Character.IsInCombat);
            _dialogue = new DialogueDirector(_data, root);
            _checkpoints = new CheckpointManager(_crew);
            _garage = new FleetGarage(_state);

            var context = new MissionContext(_config, _locations, _data, _crew, _switching,
                _abilities, _dialogue, _checkpoints, _state);
            context.Cutscenes = _cutscenes = new CutsceneDirector(_crew, _dialogue, _locations, dataDirectory);
            _missions = new MissionManager(context, _state, _catalog);
            _missionMarkers = new MissionMarkers(_catalog, _state, _missions, _locations, dataDirectory, _config.MissionStartKey.ToString());
            _death = new DeathController(_config, _crew, _missions, _abilities, _switching, _dialogue);
            _menu = new DevMenu(_config, _crew, _switching, _abilities, _missions, _catalog,
                _state, _dialogue, _data, _survey, _death, _homes, _dispatches);
            _shops = new ShopService(_crew, _state, _weapons, _memory);
            _shops.Allowed = () => !_missions.IsRunning && !_cutscenes.IsActive && !_death.IsHandling && !_survey.IsActive && !_homes.Apartment.Inside && !_homes.Apartment.Busy;
            _shops.OpenMenu = _menu.OpenShop; _menu.Shops = _shops;
            _homes.OpenMenu = _menu.OpenHomePage;
            _prologue = new PrologueSequence(_crew, _cutscenes, _locations, _state, () => _homes.Position(CrewSlot.Guess));
            _prologue.Finished = StartAfterPrologue;
            _homes.RouteNextLead = _missionMarkers.RouteNextAvailable;
            _homes.ApplyFleetUpgrade = vehicle =>
            {
                _garage.Update();
                string fitted = _garage.FitChopBay(vehicle);
                if (fitted != null) GameUtils.Notify("~o~" + fitted);
            };
            _homes.Allowed = () => !_missions.IsRunning && !_cutscenes.IsActive && !_death.IsHandling && !_survey.IsActive;

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
            Step("controller ability", () => _abilities.HandleController(_missions.RequiredSwitch.HasValue || _homes.Apartment.Inside || _homes.Apartment.Busy || _menu.IsOpen || _characterWheel.IsOpen ||
                _cutscenes.IsActive || _death.IsHandling || ControllerInput.Pressed(GTA.Control.CharacterWheel)));
            // Each subsystem is stepped separately. Wrapping the whole tick in one
            // try/catch meant a fault in the first line stopped every line after it:
            // a crew-controller bug took missions, dialogue and the dev menu with it,
            // and the mod looked frozen rather than broken. A failing subsystem now
            // costs only itself.
            // Death runs first, and blocks the rest of the tick while it does. A
            // corpse must not be fed to the companion AI or ticked through a
            // mission objective for the frames it takes to stand back up.
            if (_cutscenes.IsActive && (Game.Player.Character == null || Game.Player.Character.IsDead))
            {
                Step("close character wheel", _characterWheel.Close);
                Step("stop scene", _cutscenes.Stop);
            }
            if (_characterWheel.IsOpen && (Game.Player.Character == null || Game.Player.Character.IsDead ||
                !_crew.IsDeployed || _cutscenes.IsActive || _menu.IsOpen)) _characterWheel.Close();
            if (Game.Player.Character == null || Game.Player.Character.IsDead) Step("cancel apartment", _homes.StopApartment);
            Step("death", _death.Update);
            if (_death.IsHandling) { _menu.Close(); _survey.Stop(); _characterWheel.Close(); _controllerWheelHeld = false; _controllerSelection = null; Game.TimeScale = 1f; ObjectiveMarkers.Clear(); _missionMarkers.Clear(); return; }
            if (_homes.Apartment.Busy) { Step("apartment loading", _homes.UpdateTransition); return; }
            if (_cutscenes.IsActive)
            {
                ObjectiveMarkers.Clear();
                _missionMarkers.Clear();
                _homes.Clear();
                Step("cutscene", _cutscenes.Update);
                Step("abort hold", HandleAbortHold);
            Step("mission handoff", () => _handoff.Update(_crew, _missions.IsRunning ? _missions.RequiredSwitch : null));
            if (_handoff.IsWaiting) Step("stop ability during handoff", _abilities.Stop);
                return;
            }
            _crew.CompanionAI.MissionActive = _missions.IsRunning;
            Step("free-roam character memory", () => _memory.Update(_crew, !_missions.IsRunning && !_prologue.IsActive));
            Step("crew", _crew.Update);
            Step("military response", () => _crew.CompanionAI.Military.Update(_crew.ActiveSlot, _crew.IsDeployed && !_missions.IsRunning && !_survey.IsActive, _crew.CrewGroup, _menu.IsOpen));
            Step("abilities", _abilities.Update);
            Step("garage", _garage.Update);
            Step("world speed", () => _worldTuning.Update(_crew));
            Step("tactical response", () => _tactics.Update(_crew));
            Step("shops", () => _shops.Update(!_menu.IsOpen && !_characterWheel.IsOpen && !_missions.IsRunning && !_survey.IsActive && !_prologue.IsActive));
            Step("weapon ownership", () => _weapons.Update(_crew, !_missions.IsRunning && !_prologue.IsActive));
            Step("homes", () => _homes.Update(!_missions.IsRunning && !_prologue.IsActive && !_menu.IsOpen && !_survey.IsActive && !_characterWheel.IsOpen));
            ObjectiveMarkers.ActiveSlot = _crew.ActiveSlot;
            ObjectiveMarkers.BeginFrame(_missions.IsRunning || _prologue.IsActive);
            // Every producer of a navigation route runs between BeginFrame and
            // EndFrame; the prologue's drive-home route is a producer like any
            // mission objective, so it lives here and nowhere earlier in the tick.
            Step("prologue", _prologue.Update);
            Step("missions", _missions.Update);
            ObjectiveMarkers.EndFrame();
            if (_cutscenes.IsActive) { _characterWheel.Close(); ObjectiveMarkers.Clear(); _missionMarkers.Clear(); return; }
            Step("dialogue", _dialogue.Update);
            Step("crew dispatches", () => _dispatches.Update(_crew.IsDeployed && !_missions.IsRunning && !_prologue.IsActive && !_menu.IsOpen && !_survey.IsActive && !_characterWheel.IsOpen && !_dialogue.HasPending));
            Step("controller menu", _menu.HandleControllerToggle);
            Step("menu", _menu.Update);
            Step("survey", _survey.Update);
            Step("mission markers", () => _missionMarkers.Update(_menu.IsOpen || _prologue.IsActive));
            if (!_menu.IsOpen && _missionMarkers.Nearby != null && Game.IsControlJustPressed(GTA.Control.Context))
                StartMission(_missionMarkers.Nearby);
            if (_missions.IsRunning && !_menu.IsOpen)
                new GTA.UI.TextElement(_missions.LastAttempted.Id + " | " + _missions.CurrentTitle,
                    new System.Drawing.PointF(24, 74), .28f, System.Drawing.Color.White).Draw();
            if (_missions.IsRunning && !_menu.IsOpen) MissionObjectiveHud.Draw(_missions.CurrentObjective);
            Step("controller switch", HandleControllerSwitch);
            Step("abort hold", HandleAbortHold);
            Step("mission handoff", () => _handoff.Update(_crew, _missions.IsRunning ? _missions.RequiredSwitch : null));
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
            if (!_config.ControllerSwitchEnabled || !_crew.IsDeployed || _menu.IsOpen || _homes.Apartment.Inside || _homes.Apartment.Busy)
            {
                _characterWheel.Close(); _controllerWheelHeld = false; _controllerSelection = null; return;
            }
            if (_config.SuppressVanillaSwitch)
            {
                Game.DisableControlThisFrame(GTA.Control.CharacterWheel);
                Game.DisableControlThisFrame(GTA.Control.SelectCharacterMichael);
                Game.DisableControlThisFrame(GTA.Control.SelectCharacterFranklin);
                Game.DisableControlThisFrame(GTA.Control.SelectCharacterTrevor);
                Game.DisableControlThisFrame(GTA.Control.SelectCharacterMultiplayer);
            }
            bool held = ControllerInput.Pressed(GTA.Control.CharacterWheel);
            if (!held)
            {
                _characterWheel.Close();
                var selected = _controllerSelection;
                _controllerSelection = null;
                if (_controllerWheelHeld && selected.HasValue) _switching.TrySwitch(selected.Value);
                _controllerWheelHeld = false;
                return;
            }
            if (!_characterWheel.IsOpen)
            {
                _abilities.Stop();
                _characterWheel.Open(_crew.ActiveSlot);
            }
            _controllerWheelHeld = true;
            Game.DisableControlThisFrame(GTA.Control.LookLeftRight);
            Game.DisableControlThisFrame(GTA.Control.LookUpDown);
            Game.DisableControlThisFrame(GTA.Control.MeleeAttack1);
            Game.DisableControlThisFrame(GTA.Control.VehicleExit);
            float x = ControllerInput.Axis(GTA.Control.LookLeftRight);
            float y = ControllerInput.Axis(GTA.Control.LookUpDown);
            if (y < -0.55f && Math.Abs(y) >= Math.Abs(x)) _controllerSelection = CrewSlot.Gohan;
            else if (x < -0.55f) _controllerSelection = CrewSlot.Ice;
            else if (x > 0.55f) _controllerSelection = CrewSlot.Guess;
            if (ControllerInput.JustPressed(GTA.Control.SelectCharacterMichael)) _controllerSelection = CrewSlot.Ice;
            if (ControllerInput.JustPressed(GTA.Control.SelectCharacterFranklin)) _controllerSelection = CrewSlot.Gohan;
            if (ControllerInput.JustPressed(GTA.Control.SelectCharacterTrevor)) _controllerSelection = CrewSlot.Guess;
            if (_controllerSelection.HasValue) _characterWheel.Selected = _controllerSelection.Value;
            _characterWheel.Draw(_crew);
            GameUtils.Subtitle("Release D-pad Down to switch." + (_config.DevToolsEnabled ? "  B: debug menu" : ""), 500);
        }

        private void HandleAbortHold()
        {
            if (_abortHeldSince == 0) return;

            if (Game.GameTime - _abortHeldSince > 1200)
            {
                _abortHeldSince = 0;
                if (_prologue.IsActive) _prologue.Skip();
                else if (_missions.IsRunning) _missions.Abort();
                else if (_crew.IsDeployed) StandDown();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_homes.Apartment.Busy) return;
            if (_death.IsHandling) { _menu.Close(); _survey.Stop(); _characterWheel.Close(); _controllerWheelHeld = false; _controllerSelection = null; Game.TimeScale = 1f; ObjectiveMarkers.Clear(); _missionMarkers.Clear(); return; }
            if (_cutscenes.IsActive)
            {
                if (e.KeyCode == Keys.Enter) _cutscenes.Skip();
                else if (e.KeyCode == _config.AbortKey && _abortHeldSince == 0) _abortHeldSince = Game.GameTime;
                return;
            }
            try
            {
                if (_menu.IsOpen && e.KeyCode != _config.DevMenuKey) { _menu.HandleKey(e.KeyCode); return; }
                // The menu takes keys first while it is open, so its navigation never
                // doubles as a gameplay bind.
                if (_config.DevToolsEnabled)
                {
                    if (e.KeyCode == _config.DevMenuKey)
                    {
                        _menu.Toggle();
                        return;
                    }

                    if (_menu.IsOpen) { _menu.HandleKey(e.KeyCode); return; }
                }

                if (_characterWheel.IsOpen) return;
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
            if (_homes.Apartment.Inside || _homes.Apartment.Busy) return true;
            if (key == _config.SwitchIceKey) { _switching.TrySwitch(CrewSlot.Ice); return true; }
            if (key == _config.SwitchGohanKey) { _switching.TrySwitch(CrewSlot.Gohan); return true; }
            if (key == _config.SwitchGuessKey) { _switching.TrySwitch(CrewSlot.Guess); return true; }
            if (key == _config.SwitchNextKey) { CycleCrew(1); return true; }
            if (key == _config.SwitchPrevKey) { CycleCrew(-1); return true; }
            if (key == _config.AbilityKey) { if (!_missions.RequiredSwitch.HasValue) _abilities.Toggle(); return true; }
            if (key == _config.MissionStartKey) { StartMission(_missionMarkers.Nearby); return true; }
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
                // Not Insert/Delete: Insert is ScriptHookVDotNet's reload-all-scripts
                // key, which tears the mod down mid-mission.
                case Keys.OemOpenBrackets: _missions.CommitCheckpoint(); break;
                case Keys.OemCloseBrackets: _missions.RestoreCheckpoint(); break;
                case Keys.End: _survey.Skip(); break;
                case Keys.Home: _survey.Previous(); break;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == _config.AbortKey) _abortHeldSince = 0;
        }

        private void StartMission(MissionDefinition requested = null)
        {
            if (_homes.Apartment.Inside || _homes.Apartment.Busy) { GameUtils.Notify("~y~Exit the apartment before starting a job."); return; }
            if (_missions.IsRunning || _prologue.IsActive) return;

            if (!_data.IsLoaded)
            {
                GameUtils.Notify("~r~No campaign data.~s~ Copy data/ into scripts/Bloodlines/data/.");
                return;
            }

            var next = requested ?? (_missions.RetryAvailable ? _missions.LastAttempted : _state.NextPlayable(_catalog));
            if (next == null)
            {
                // Never M01 by default: the end of the scripted content is a state of
                // its own and the player is told which one they are in.
                GameUtils.Notify((_state.Progress(_catalog) == CampaignProgress.ImplementedContentComplete ? "~g~" : "~y~") + _state.DescribeProgress(_catalog));
                return;
            }
            // Eligibility first, with no side effects: a refused start must not cost
            // the player a deployed crew, their position, the weather or a wanted
            // level. The manager repeats the check when it actually starts.
            if (!_missions.CanStart(next, out string refusal))
            {
                GameUtils.Notify("~y~" + refusal);
                return;
            }
            if (requested == null && _missions.RetryAvailable) GameUtils.Notify("~y~Retrying " + next.Id + "~s~ from the beginning. Walk to another marker to choose a different job.");

            // Missions own their own deployment; a free-roam crew would fight the
            // mission's spawn, so stand it down first.
            if (_crew.IsDeployed) StandDown();

            // A fresh campaign opens on Ron's arrival, not on the dockyard. The
            // prologue hands off to M01 itself when it ends.
            if (next.Id == "M01" && _state.PrologueDue && _prologue.Begin())
            {
                GameUtils.Notify("~o~Los Santos International.~s~ Ron is home.");
                return;
            }

            if (_missions.Start(next))
            {
                GameUtils.Notify("~b~" + next.Id + "~s~ — " + next.Title + "~n~" + next.Info.Location +
                                 " · " + next.Info.Time);
            }
        }

        /// <summary>
        /// The arrival scene has ended at the apartment door. Cut to Terminal Island:
        /// the dockyard is across the city and M01's placement check wants its
        /// surfaces streamed, so Ron is moved there under a fade before the cold
        /// open, which then frames its own three approaches.
        /// </summary>
        private void StartAfterPrologue()
        {
            var m01 = _catalog.All.FirstOrDefault(m => m.Id == "M01");
            if (m01 == null) return;
            var player = Game.Player.Character;
            var dock = _locations.Position("M01.RegroupPoint");
            GameUtils.FadeOut(900);
            Script.Wait(950);
            try
            {
                // Out of the car first, deterministically, then across the city. If
                // either half cannot be done, nothing is moved: the arrival is already
                // saved, Ron keeps his state, and M01 waits for the mission key.
                var placement = PrologueSequence.PlaceForColdOpen(player, dock);
                if (placement != PrologueSequence.Placement.Placed)
                {
                    Logger.Warn("Prologue hand-off did not place Ron at the dock (" + placement + "); M01 waits for the mission key.");
                    GameUtils.Notify(placement == PrologueSequence.Placement.StillSeated
                        ? "~y~Get out of the vehicle, then press " + _config.MissionStartKey + " to start the dockyard job."
                        : "~y~Terminal Island did not load. Press " + _config.MissionStartKey + " near its marker to start the dockyard job.");
                    return;
                }
                GameUtils.Subtitle("~o~Terminal Island. Later that night.", 3500);
                StartMission(m01);
            }
            finally { GameUtils.FadeIn(1200); }
        }

        private void ToggleDeployment()
        {
            if (_survey.IsActive)
            {
                GameUtils.Subtitle("~y~Stop the survey before deploying the crew.", 3000);
                return;
            }
            if (_prologue.IsActive)
            {
                GameUtils.Subtitle("~y~Get home first.", 2000);
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
            // Before the dockyard, Ron has not found his brothers. Free roam is his
            // alone; the QA build keeps the full sandbox behind [Dev] Enabled.
            bool preReunion = !_state.IsComplete("M01") && !_config.DevToolsEnabled;
            bool deployed = preReunion
                ? _crew.DeploySolo(Protagonist.StartingSlot, player.Position, player.Heading)
                : _crew.Deploy(Protagonist.StartingSlot, player.Position, player.Heading);
            if (!deployed)
            {
                GameUtils.Notify("~r~Crew failed to deploy — see Bloodlines.log.");
                return;
            }

            _memory.Restore(_crew);
            if (preReunion)
            {
                GameUtils.Notify("~o~Guess is back in Los Santos.~s~ Ice and Gohan are not in his life yet. Finish Ghost in the Dockyard to bring the crew together.");
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
                if (!_missions.IsRunning && !_death.IsHandling) _memory.Capture(_crew);
                _state.RecordPosition(_crew.ActiveSlot, _homes.SavePosition);
                Step("save position", _state.Save);
            }

            Step("close character wheel", _characterWheel.Close);
            Step("cancel prologue", _prologue.Cancel);
            Step("stop scene", _cutscenes.Stop);
            Step("stop ability", _abilities.Stop);
            Step("stop switching", _switching.Cancel);
            Step("clear dialogue", _dialogue.Clear);
            Step("reset garage", _garage.Reset);
            Step("leave apartment", _homes.StopApartment);
            Step("release DLC cars", _menu.ReleaseVehicles);
            Step("clear shops", _shops.Clear);
            Step("restore world tuning", _worldTuning.Reset);
            Step("reset pursuit tuning", _tactics.Reset);
            Step("dismiss crew", _crew.Dismiss);
            Step("release recovery", _death.Cancel);
            GameUtils.Notify("~y~Crew stood down.");
        }

        private void OnAborted(object sender, EventArgs e)
        {
            Logger.Info("Script aborting - tearing down.");
            Step("save free-roam crew memory", () => { if (!_missions.IsRunning && !_death.IsHandling) { _memory.Capture(_crew); _state.Save(); } });
            Step("cancel prologue", _prologue.Cancel);
            Step("close character wheel", _characterWheel.Close);
            Step("stop scene", _cutscenes.Stop);
            Step("stop home markers", _homes.Clear);
            Step("stop mission markers", _missionMarkers.Clear);
            Step("stop objective markers", ObjectiveMarkers.Clear);
            Step("stop survey", _survey.Stop);
            Step("stop switching", _switching.Cancel);
            Step("mission shutdown", _missions.Shutdown);
            Step("close character wheel", _characterWheel.Close);
            Step("stop scene", _cutscenes.Stop);
            Step("stop ability", _abilities.Stop);
            Step("clear dialogue", _dialogue.Clear);
            Step("leave apartment", _homes.StopApartment);
            Step("release DLC cars", _menu.ReleaseVehicles);
            Step("clear shops", _shops.Clear);
            Step("restore world tuning", _worldTuning.Reset);
            Step("reset pursuit tuning", _tactics.Reset);
            Step("dismiss crew", _crew.Dismiss);
            Step("release recovery", _death.Cancel);
            Step("restore time", () => Game.TimeScale = 1f);
        }
    }
}

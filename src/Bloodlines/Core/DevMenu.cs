using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Bloodlines.Abilities;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;

namespace Bloodlines.Core
{
    /// <summary>
    /// In-game developer menu.
    ///
    /// Built before the rest of the campaign on purpose: when you are debugging stage
    /// six of a fifteen-minute mission, replaying it from the top for every attempt is
    /// what makes a project this size stop being worked on. Everything here exists to
    /// get you to the exact state you need to reproduce a bug — mission, stage,
    /// character, wanted level, weather, hostiles, dialogue, save state.
    ///
    /// Off unless [Dev] Enabled = True.
    /// </summary>
    public sealed class DevMenu
    {
        private const int VisibleRows = 11;
        private readonly ControllerNavigation _stick = new ControllerNavigation();

        private readonly ModConfig _config;
        private readonly CrewRoster _crew;
        private readonly SwitchController _switching;
        private readonly AbilityController _abilities;
        private readonly MissionManager _missions;
        private readonly MissionCatalog _catalog;
        private readonly CampaignState _state;
        private readonly DialogueDirector _dialogue;
        private readonly CampaignData _data;
        private readonly SurveyMode _survey;
        private readonly DeathController _death;
        private readonly CrewHomes _homes;
        private readonly CampaignDispatches _dispatches;
        private readonly StoryVehicles _vehicles = new StoryVehicles();
        public void ReleaseVehicles() { _vehicles.Clear(); }

        private readonly Stack<Page> _stack = new Stack<Page>();

        public DevMenu(ModConfig config, CrewRoster crew, SwitchController switching,
            AbilityController abilities, MissionManager missions, MissionCatalog catalog,
            CampaignState state, DialogueDirector dialogue, CampaignData data, SurveyMode survey,
            DeathController death, CrewHomes homes, CampaignDispatches dispatches)
        {
            _config = config;
            _crew = crew;
            _switching = switching;
            _abilities = abilities;
            _missions = missions;
            _catalog = catalog;
            _state = state;
            _dialogue = dialogue;
            _data = data;
            _survey = survey;
            _death = death; _homes = homes; _dispatches = dispatches;
        }

        public bool IsOpen { get; private set; }

        public void Close()
        {
            IsOpen = false;
            _cameraHeld = false;
            _waitForOpeningDownRelease = false;
            _stack.Clear();
            _stick.Reset();
        }

        public void Toggle()
        {
            _stick.Reset();
            IsOpen = !IsOpen;
            if (!IsOpen)
            {
                _cameraHeld = false;
                _stack.Clear();
                return;
            }

            _stack.Clear();
            _stack.Push(BuildRoot());
        }

        /// <summary>Returns true when the key was consumed by the menu.</summary>
        public bool HandleKey(Keys key)
        {
            if (!IsOpen || _stack.Count == 0) return false;

            var page = _stack.Peek();

            switch (key)
            {
                case Keys.Up:
                    page.Move(-1);
                    return true;
                case Keys.Down:
                    page.Move(1);
                    return true;
                case Keys.PageUp:
                    page.Move(-VisibleRows);
                    return true;
                case Keys.PageDown:
                    page.Move(VisibleRows);
                    return true;
                case Keys.Left:
                    page.Selected?.Adjust?.Invoke(-1);
                    return true;
                case Keys.Right:
                    page.Selected?.Adjust?.Invoke(1);
                    return true;
                case Keys.Enter:
                    Activate(page.Selected);
                    return true;
                case Keys.Back:
                    if (_stack.Count > 1) _stack.Pop();
                    else Toggle();
                    return true;
                default:
                    return false;
            }
        }

        private void Activate(Item item)
        {
            if (item?.Action == null) return;

            try
            {
                item.Action();
            }
            catch (Exception ex)
            {
                Logger.Error("Dev menu action failed: " + item.Label, ex);
                GameUtils.Subtitle("~r~Action failed — see the log.", 3000);
            }
        }

        public void HandleControllerToggle()
        {
            if (!_config.DevToolsEnabled || IsOpen || _homes.Apartment.Busy) return;
            if (ControllerInput.Pressed(GTA.Control.CharacterWheel) && ControllerInput.JustPressed(GTA.Control.FrontendCancel))
            {
                Toggle();
                // Consume this opening B press; it must not immediately go back.
                _openedAt = Game.GameTime;
                _waitForOpeningDownRelease = true;
            }
        }

        private int _openedAt;
        private bool _waitForOpeningDownRelease;
        private bool _cameraHeld;
        private int _pedView, _vehicleView;
        private void HoldGameplayCamera()
        {
            if (!_cameraHeld)
            {
                _pedView = Function.Call<int>(Hash.GET_FOLLOW_PED_CAM_VIEW_MODE);
                _vehicleView = Function.Call<int>(Hash.GET_FOLLOW_VEHICLE_CAM_VIEW_MODE);
                _cameraHeld = true;
            }
            // Menu input must not cycle the gameplay view or start an idle cinematic.
            // Remember the user's chosen perspective instead of changing their setting.
            Function.Call(Hash.INVALIDATE_IDLE_CAM);
            // Native name is absent from the pinned SHVDN 3.6 enum.
            Function.Call((Hash)0x9E4CFFF989258472UL);
            for (int group = 0; group < 3; group++)
                Function.Call(Hash.DISABLE_CONTROL_ACTION, group, (int)GTA.Control.NextCamera, true);
            if (Function.Call<int>(Hash.GET_FOLLOW_PED_CAM_VIEW_MODE) != _pedView)
                Function.Call(Hash.SET_FOLLOW_PED_CAM_VIEW_MODE, _pedView);
            if (Function.Call<int>(Hash.GET_FOLLOW_VEHICLE_CAM_VIEW_MODE) != _vehicleView)
                Function.Call(Hash.SET_FOLLOW_VEHICLE_CAM_VIEW_MODE, _vehicleView);
        }
        public void Update()
        {
            if (!IsOpen || _stack.Count == 0) return;
            HoldGameplayCamera();

            // Stop the player shooting or swinging while the menu has focus.
            Game.DisableControlThisFrame(GTA.Control.Attack);
            Game.DisableControlThisFrame(GTA.Control.Attack2);
            Game.DisableControlThisFrame(GTA.Control.Aim);
            Game.DisableControlThisFrame(GTA.Control.MeleeAttack1);

            Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0);
            // Movement and camera stay on the sticks; D-pad and A/B operate the menu.
            // Keep sprint (A), melee (B), and camera-mode cycling blocked.
            foreach (var control in new[] { GTA.Control.MoveLeftRight, GTA.Control.MoveUpDown,
                GTA.Control.MoveUpOnly, GTA.Control.MoveDownOnly, GTA.Control.MoveLeftOnly, GTA.Control.MoveRightOnly,
                GTA.Control.LookLeftRight, GTA.Control.LookUpDown })
                Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)control, true);
            if (!ControllerInput.Pressed(GTA.Control.CharacterWheel)) _waitForOpeningDownRelease = false;
            bool padReady = !_waitForOpeningDownRelease;
            var direction = _stick.Update(0f, 0f, Game.GameTime,
                padReady && ControllerInput.Pressed(GTA.Control.FrontendUp),
                padReady && ControllerInput.Pressed(GTA.Control.FrontendDown),
                padReady && ControllerInput.Pressed(GTA.Control.FrontendLeft),
                padReady && ControllerInput.Pressed(GTA.Control.FrontendRight));
            if (direction == MenuDirection.Up) HandleKey(Keys.Up);
            else if (direction == MenuDirection.Down) HandleKey(Keys.Down);
            else if (direction == MenuDirection.Left) HandleKey(Keys.Left);
            else if (direction == MenuDirection.Right) HandleKey(Keys.Right);
            if (Game.GameTime != _openedAt)
            {
                if (ControllerInput.JustPressed(GTA.Control.FrontendCancel)) HandleKey(Keys.Back);
                else if (ControllerInput.JustPressed(GTA.Control.FrontendAccept)) HandleKey(Keys.Enter);
            }
            if (IsOpen && _stack.Count > 0) Draw(_stack.Peek());
        }

        // ---------- pages ----------

        private Page BuildRoot()
        {
            if (_homes.Apartment.Inside) return BuildHomePage();
            var page = new Page("Bloodlines — dev menu");

            page.Add("Replay M01: Ghost in the Dockyard", () => "three separate assignments",
                () => { Close(); _survey.Stop(); _missions.Abort(); _missions.Start(_catalog.All.First(m => m.Id == "M01")); });
            page.Add("Missions", () => _catalog.Playable.Count() + " playable",
                () => _stack.Push(BuildMissionList()));
            page.Add("Current objective", () => _missions.IsRunning ? _missions.LastAttempted.Id : "none", () => _stack.Push(BuildObjectiveDetails()));
            page.Add("Running mission", () => _missions.IsRunning ? _missions.LastAttempted.Id + " | " + _missions.CurrentTitle : "none",
                () => _stack.Push(BuildMissionControl()));
            page.Add("Crew", () => _crew.IsDeployed ? _crew.Active.DisplayName : "not deployed",
                () => _stack.Push(BuildCrew()));
            page.Add("Crew messages / news", () => _dispatches.Inbox.Count() + " messages", () => _stack.Push(BuildInbox()));
            page.Add("Home workbench", () => _homes.WorkbenchName, () => { Close(); if (!_missions.IsRunning) _homes.UseWorkbench(); });
            page.Add("Route to my home", () => _crew.IsDeployed ? _crew.Active.DisplayName : "deploy crew first", () => { if (_crew.IsDeployed) { _homes.RouteHome(); Close(); } });
            page.Add("DLC weapons", () => "personal locker additions", () => _stack.Push(BuildDlcWeapons()));
            page.Add("Vehicles", () => "DLC and specialty vehicles", () => _stack.Push(BuildVehicles()));
            page.Add("World", () => "wanted " + Game.Player.WantedLevel,
                () => _stack.Push(BuildWorld()));
            page.Add("Dialogue", () => _dialogue.IsSpeaking ? "speaking" : "idle",
                () => _stack.Push(BuildDialogue()));
            page.Add("Campaign save", () => _state.CompletedCount + "/" + _catalog.All.Count,
                () => _stack.Push(BuildSave()));
            page.Add("Survey coordinates", () => _survey.IsActive ? "running" : "",
                () => _stack.Push(BuildSurvey()));
            return page;
        }

        private Page BuildMissionList()
        {
            var page = new Page("Missions — Enter starts, prerequisites ignored");

            foreach (var mission in _catalog.All)
            {
                var captured = mission;
                page.Add(mission.Id + "  " + Title(mission),
                    () => Status(captured),
                    () =>
                    {
                        if (!captured.IsPlayable)
                        {
                            GameUtils.Subtitle("~y~" + captured.Id + " has no script yet.", 2500);
                            return;
                        }

                        if (_missions.IsRunning) _missions.Abort();
                        if (_crew.IsDeployed) _crew.Dismiss();
                        _missions.Start(captured);
                        Toggle();
                    });
            }

            return page;
        }

        private Page BuildObjectiveDetails()
        {
            var page = new Page("Current objective");
            string remaining = _missions.CurrentObjective;
            while (remaining.Length > 0)
            {
                int length = System.Math.Min(52, remaining.Length);
                if (length < remaining.Length) { int space = remaining.LastIndexOf(' ', length - 1, length); if (space > 0) length = space; }
                page.Add(remaining.Substring(0, length), () => "", null);
                remaining = remaining.Substring(length).TrimStart();
            }
            return page;
        }

        private Page BuildMissionControl()
        {
            var page = new Page("Running mission");

            page.Add("Read current objective", () => "", () => _stack.Push(BuildObjectiveDetails()));
            page.Add("Stage", () => _missions.IsRunning ? _missions.CurrentStage.ToString() : "—",
                null, delta => _missions.WarpStage(delta));
            page.Add("Commit checkpoint", () => "", () => _missions.CommitCheckpoint());
            page.Add("Restore checkpoint", () => "", () => _missions.RestoreCheckpoint());
            page.Add("Force pass", () => "", () =>
            {
                _missions.ForcePass();
                Toggle();
            });
            page.Add("Force fail", () => "", () => _missions.ForceFail("Failed from the dev menu."));
            page.Add("Abort", () => "", () => _missions.Abort());
            page.Add("Teleport to mission start", () => "", TeleportToMissionStart);
            return page;
        }

        private Page BuildCrew()
        {
            var page = new Page("Crew");

            page.Add("Deploy / stand down", () => _crew.IsDeployed ? "deployed" : "off", () =>
            {
                if (_missions.IsRunning) { GameUtils.Subtitle("~y~Abort the mission before changing deployment.", 3000); return; }
                if (_crew.IsDeployed) _crew.Dismiss();
                else
                {
                    var player = Game.Player.Character;
                    _crew.Deploy(Protagonist.StartingSlot, player.Position, player.Heading);
                }
            });

            foreach (var protagonist in Protagonist.All)
            {
                var slot = protagonist.Slot;
                page.Add("Switch to " + protagonist.Handle, () => _crew.ActiveSlot == slot ? "active" : "",
                    () => _switching.TrySwitch(slot));
            }

            foreach (var protagonist in Protagonist.All)
            {
                var slot = protagonist.Slot;
                page.Add("Deploy solo as " + protagonist.Handle, () => "", () =>
                {
                    var player = Game.Player.Character;
                    if (_missions.IsRunning) { GameUtils.Subtitle("~y~Abort the mission before changing deployment.", 3000); return; }
                    _crew.DeploySolo(slot, player.Position, player.Heading);
                });
            }

            page.Add("Free roam crew", () => _crew.CompanionAI.IndependentFreeRoam ? "independent" : "travel together", () =>
            {
                if (_missions.IsRunning) { GameUtils.Subtitle("~y~Mission assignments control the crew during a job.", 3000); return; }
                _crew.CompanionAI.IndependentFreeRoam = !_crew.CompanionAI.IndependentFreeRoam;
                foreach (var hero in Protagonist.All) _crew.CompanionAI.Refresh(hero.Slot);
            });

            page.Add("Off-duty crime encounters", () => _crew.CompanionAI.Life.CrimesEnabled ? "on" : "off", () => _crew.CompanionAI.Life.CrimesEnabled = !_crew.CompanionAI.Life.CrimesEnabled);
            page.Add("Appearance", () => "hair / face / outfits", () => _stack.Push(BuildAppearance()));

            page.Add("Heal everyone", () => "", () =>
            {
                foreach (var protagonist in Protagonist.All)
                {
                    var ped = _crew.PedFor(protagonist.Slot);
                    if (ped == null) continue;
                    ped.Health = ped.MaxHealth;
                    ped.Armor = 100;
                }
                var player = Game.Player.Character;
                player.Health = player.MaxHealth;
                player.Armor = 100;
            });

            // The only reliable way to exercise the respawn path on demand. Getting
            // yourself shot to test it is neither quick nor repeatable, and the bug
            // this proves absent — the black screen that never lifts — is the kind
            // you only find by dying on purpose, at a moment you chose.
            page.Add("Kill me (test respawn)", () => _death.DeathCount + " so far", () =>
            {
                if (!_crew.IsDeployed)
                {
                    GameUtils.Subtitle("~r~Deploy the crew first — death handling is only armed then.", 3000);
                    return;
                }

                Toggle();
                Game.Player.Character.Kill();
            });

            page.Add("Refill ability meter", () => Math.Round(_abilities.Meter * 100) + "%",
                () => _abilities.Refill());
            page.Add("Toggle ability", () => _abilities.IsActive ? "on" : "off", () => _abilities.Toggle());
            return page;
        }

        public void OpenHomePage()
        {
            _stick.Reset(); IsOpen = true; _stack.Clear(); _openedAt = Game.GameTime;
            _waitForOpeningDownRelease = false;
            _stack.Push(BuildHomePage());
        }
        private Page BuildHomePage()
        {
            var page = new Page(_homes.ResidenceName);
            if (_homes.Apartment.Inside)
                page.Add("Exit apartment", () => "return outside", () => { Close(); _homes.ExitApartment(); });
            else
            {
                page.Add("Enter apartment", () => _homes.Progression, () => { Close(); _homes.EnterApartment(); });
                if (_config.DevToolsEnabled && !_homes.LuxuryUnlocked)
                    page.Add("Preview luxury apartment", () => "QA only - does not unlock", () => { Close(); _homes.EnterApartment(true); });
            }
            page.Add("Wardrobe", () => "clothes / facial hair", () => _stack.Push(BuildWardrobe(_crew.ActiveSlot)));
            page.Add("Rest and save", () => "six hours", () => { Close(); _homes.Rest(); });
            page.Add("Personal weapon locker", () => "restock owned weapons", () => { Close(); _homes.RestockLocker(); });
            if (!_homes.Apartment.Inside || _crew.ActiveSlot != CrewSlot.Guess)
                page.Add(_homes.WorkbenchName, () => "personal workbench", () => { Close(); _homes.UseWorkbench(); });
            page.Add("Crew messages / news", () => _dispatches.Inbox.Count() + " messages", () => _stack.Push(BuildInbox()));
            return page;
        }
        private Page BuildInbox()
        {
            var page = new Page("Crew messages and news");
            foreach (var message in _dispatches.Inbox)
            {
                var selected = message;
                page.Add(message.Sender + ": " + message.Title, () => selected.Mission,
                    () => { Close(); GameUtils.Subtitle(selected.Sender + ": " + selected.Text, 12000); });
            }
            if (!_dispatches.Inbox.Any()) page.Add("No messages yet", () => "complete a job", null);
            return page;
        }
        private Page BuildVehicles()
        {
            var page = new Page("Vehicles — select a category");
            page.Add("Release parked vehicles", () => "keeps occupied vehicles", () => _vehicles.ReleaseParked());
            foreach (string category in StoryVehicles.Catalog.Select(v => v.Category).Distinct())
            {
                string selected = category;
                page.Add(selected, () => StoryVehicles.Catalog.Count(v => v.Category == selected && StoryVehicles.Available(v)) + " available",
                    () => _stack.Push(BuildVehicleCategory(selected)));
            }
            return page;
        }
        private Page BuildDlcWeapons(string category = null)
        {
            var page = new Page(category ?? "DLC weapons - active hero");
            if (category == null)
                foreach (string group in WeaponProgression.DlcCatalog.Select(w => w.Category).Distinct())
                { string selected = group; page.Add(group, () => "browse", () => _stack.Push(BuildDlcWeapons(selected))); }
            else foreach (var weapon in WeaponProgression.DlcCatalog.Where(w => w.Category == category && WeaponProgression.Available(w)))
            {
                var selected = weapon;
                page.Add(weapon.Name, () => "add to personal locker", () =>
                {
                    if (!_crew.IsDeployed || _missions.IsRunning) { GameUtils.Notify("~y~Deploy the crew in free roam first."); return; }
                    if (new WeaponProgression(_state).GiveDlc(_crew.ActiveSlot, _crew.PedFor(_crew.ActiveSlot), selected))
                        GameUtils.Notify("~g~" + selected.Name + " added to " + _crew.Active.DisplayName + "'s locker.");
                    else GameUtils.Notify("~y~That weapon could not be equipped on this build.");
                });
            }
            return page;
        }
        private Page BuildVehicleCategory(string category)
        {
            var page = new Page(category + " — request at a suitable location");
            foreach (var choice in StoryVehicles.Catalog.Where(v => v.Category == category && StoryVehicles.Available(v)))
            {
                var selected = choice;
                page.Add(selected.Name, () => selected.Model, () =>
                {
                    if (_missions.IsRunning) { GameUtils.Notify("~y~Finish or leave the mission before requesting a vehicle."); return; }
                    if (_vehicles.Spawn(selected)) Close();
                });
            }
            return page;
        }

        private Page BuildWorld()
        {
            var page = new Page("World");

            page.Add("Military response (sixth tier)", () => "free roam test", () => { if (!_missions.IsRunning && _crew.IsDeployed) _crew.CompanionAI.Military.Trigger(_crew.ActiveSlot); });
            page.Add("Wanted level", () => (_crew.IsDeployed ? _crew.CompanionAI.Military.Level(_crew.ActiveSlot) : Game.Player.WantedLevel).ToString(), null,
                delta =>
                {
                    if (_crew.IsDeployed && !_missions.IsRunning)
                        _crew.CompanionAI.Military.SetLevel(_crew.ActiveSlot, _crew.CompanionAI.Military.Level(_crew.ActiveSlot) + delta);
                    else Game.Player.WantedLevel = Math.Max(0, Math.Min(5, Game.Player.WantedLevel + delta));
                });
            page.Add("Clock hour", () => Function.Call<int>(Hash.GET_CLOCK_HOURS).ToString("00") + ":00", null,
                delta =>
                {
                    int hour = (Function.Call<int>(Hash.GET_CLOCK_HOURS) + delta + 24) % 24;
                    GameUtils.SetClock(hour, 0);
                });

            string[] weathers = { "EXTRASUNNY", "CLEAR", "CLEARING", "OVERCAST", "RAIN", "THUNDER", "FOGGY", "SMOG" };
            int weatherIndex = 0;
            page.Add("Weather", () => weathers[weatherIndex], null, delta =>
            {
                weatherIndex = (weatherIndex + delta + weathers.Length) % weathers.Length;
                Function.Call(Hash.SET_WEATHER_TYPE_NOW, weathers[weatherIndex]);
            });

            page.Add("Spawn 3 hostiles", () => "", () => SpawnHostiles(3));
            page.Add("Spawn 8 hostiles", () => "", () => SpawnHostiles(8));
            page.Add("Kill nearby hostiles", () => "", KillNearbyHostiles);
            page.Add("Repair current vehicle", () => "", () =>
            {
                var vehicle = Game.Player.Character.CurrentVehicle;
                if (vehicle == null || !vehicle.Exists()) return;
                vehicle.Repair();
                vehicle.Wash();
            });
            page.Add("Clear area of wrecks", () => "", () =>
            {
                foreach (var vehicle in World.GetNearbyVehicles(Game.Player.Character.Position, 150f))
                {
                    if (vehicle.IsDead || !vehicle.IsDriveable) GameUtils.SafeDelete(vehicle);
                }
            });
            return page;
        }

        private Page BuildDialogue()
        {
            var page = new Page("Dialogue");

            var mission = _missions.LastAttempted;
            string missionId = mission?.Id ?? "M01";

            for (int stage = 1; stage <= 4; stage++)
            {
                int captured = stage;
                var cues = _data.Stage(missionId, captured).ToList();
                page.Add("Play " + missionId + " stage " + captured, () => cues.Count + " lines",
                    () => _dialogue.PlayStage(missionId, captured));
            }

            page.Add("Clear the queue", () => "", () => _dialogue.Clear());
            return page;
        }

        private Page BuildSave()
        {
            var page = new Page("Campaign save");

            page.Add("Completed", () => _state.CompletedCount + "/" + _catalog.All.Count, null);
            page.Add("Next playable", () => _state.NextPlayable(_catalog)?.Id ?? "none", null);
            page.Add("Cash", () => "$" + _state.CashOnHand, null, delta =>
            {
                _state.CashOnHand = Math.Max(0, _state.CashOnHand + delta * 10000);
                _state.Save();
            });
            page.Add("Mark last mission complete", () => "", () =>
            {
                var last = _missions.LastAttempted;
                if (last == null) return;
                _state.MarkComplete(last.Id, _catalog);
                GameUtils.Subtitle("~g~" + last.Id + " marked complete.", 2500);
            });
            page.Add("Unlock all safehouses", () => "", () =>
            {
                foreach (var key in _state.Safehouses.Keys.ToList()) _state.Safehouses[key] = true;
                _state.Save();
            });
            page.Add("Install all fleet upgrades", () => "", () =>
            {
                foreach (var key in _state.FleetUpgrades.Keys.ToList()) _state.FleetUpgrades[key] = true;
                _state.Save();
            });
            page.Add("Reset campaign", () => "", () =>
            {
                _state.Reset();
                GameUtils.Subtitle("~r~Campaign progress reset.", 3000);
            });
            page.Add("Save now", () => "", () => _state.Save());
            return page;
        }

        private Page BuildSurvey()
        {
            var page = new Page("Survey - GPS routes and optional teleport");
            page.Add("Teleport to current survey spot", () => _config.SurveyTeleportKey.ToString(), () =>
            {
                _survey.TeleportToCurrent();
                Toggle();
            });
            page.Add("Capture current survey spot", () => _config.DevCaptureKey.ToString(), () => _survey.Capture());
            page.Add("Next survey spot", () => "End", () => _survey.Skip());
            page.Add("Previous survey spot", () => "Home", () => _survey.Previous());

            page.Add("Survey everything", () => _survey.IsActive ? "running" : "", () =>
            {
                StartSurvey(null);
            });

            foreach (var mission in _catalog.Playable)
            {
                var captured = mission;
                page.Add("Survey " + captured.Id + " only", () => "", () =>
                {
                    StartSurvey(captured.Id);
                });
            }

            page.Add("Stop and write the ini", () => "", () => _survey.Stop());
            return page;
        }

        private Page BuildAppearance()
        {
            var page = new Page("Wardrobe - choose a character");
            foreach (var hero in Protagonist.All)
            {
                var slot = hero.Slot;
                page.Add(hero.DisplayName, () => "clothes / face / facial hair", () => _stack.Push(BuildWardrobe(slot)));
            }
            return page;
        }
        private bool CanChangeLook(CrewSlot slot)
        {
            if (_missions.IsRunning) { GameUtils.Notify("~y~Change clothes between missions."); return false; }
            var ped = _crew.PedFor(slot);
            if (ped == null || !ped.Exists()) { GameUtils.Notify("~y~Deploy this character first."); return false; }
            return true;
        }
        private Page BuildWardrobe(CrewSlot slot)
        {
            var page = new Page(Protagonist.Of(slot).DisplayName + " - wardrobe");
            page.Add("Save looks", () => "kept across restarts", () => { CrewAppearance.Save(); GameUtils.Notify("~g~Crew appearance saved."); });
            page.Add("Automatic outfit changes", () => CrewAppearance.For(slot).AutoOutfits ? "on" : "off", () =>
            { CrewAppearance.For(slot).AutoOutfits = !CrewAppearance.For(slot).AutoOutfits; });
            foreach (string field in new[] { "Hair", "HairColor", "Beard", "BeardColor", "Face", "Skin", "Outfit" })
            {
                string selected = field;
                page.Add(field == "Outfit" ? "Reset clothing preset" : field == "Beard" ? "Facial hair" : field, () =>
                {
                    var l = CrewAppearance.For(slot);
                    int n = selected == "Hair" ? l.Hair : selected == "HairColor" ? l.HairColor : selected == "Beard" ? l.Beard :
                        selected == "BeardColor" ? l.BeardColor : selected == "Face" ? l.Face : selected == "Skin" ? l.Skin : l.Outfit;
                    return n < 0 ? "none" : n.ToString();
                }, adjust: direction => { if (CanChangeLook(slot)) CrewAppearance.Adjust(_crew.PedFor(slot), slot, selected, direction); });
            }
            foreach (var fields in new[] { CrewAppearance.Clothing, CrewAppearance.Props }) foreach (string field in fields)
            {
                string selected = field;
                page.Add(field, () => "item / color", () => _stack.Push(BuildClothing(slot, selected)));
            }
            return page;
        }
        private Page BuildClothing(CrewSlot slot, string field)
        {
            var page = new Page(Protagonist.Of(slot).DisplayName + " - " + field);
            page.Add("Item", () => { int n = CrewAppearance.Drawable(slot, field); return n < 0 ? "default / none" : n.ToString(); },
                adjust: d => { if (CanChangeLook(slot)) CrewAppearance.AdjustClothing(_crew.PedFor(slot), slot, field, d, false); });
            page.Add("Color / texture", () => CrewAppearance.Texture(slot, field).ToString(),
                adjust: d => { if (CanChangeLook(slot)) CrewAppearance.AdjustClothing(_crew.PedFor(slot), slot, field, d, true); });
            page.Add("Save looks", () => "kept across restarts", () => { CrewAppearance.Save(); GameUtils.Notify("~g~Crew appearance saved."); });
            return page;
        }

        private void StartSurvey(string missionId)
        {
            if (_missions.IsRunning)
            {
                GameUtils.Subtitle("~y~Abort the current mission before surveying.", 3500);
                return;
            }
            _abilities.Stop();
            _switching.Cancel();
            if (_crew.IsDeployed) _crew.Dismiss();
            _survey.Start(missionId);
            Toggle();
        }

        // ---------- actions ----------

        private void TeleportToMissionStart()
        {
            var mission = _missions.LastAttempted ?? _state.NextPlayable(_catalog);
            if (mission == null) return;

            // The bible's surveyed anchor for the prologue, otherwise the mission's own
            // estimated start from the locations file.
            if (mission.Id == "M01")
            {
                GameUtils.Subtitle("~y~Use Survey M01 to mark or teleport to the actual mission locations.", 4000);

                Toggle();
            }
            else
            {
                GameUtils.Subtitle("~y~No surveyed start for " + mission.Id + ".", 2500);
            }
        }

        private static void SpawnHostiles(int count)
        {
            var player = Game.Player.Character;
            var group = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            var model = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(model)) return;

            for (int i = 0; i < count; i++)
            {
                var offset = new Vector3(-8f + i * 3f, 14f, 0f);
                var ped = World.CreatePed(model, player.Position + offset, 0f);
                if (ped == null || !ped.Exists()) continue;

                ped.RelationshipGroup = group;
                ped.Weapons.Give(WeaponHash.CarbineRifle, 150, true, true);
                ped.Task.FightAgainstHatedTargets(80f);
                ped.MarkAsNoLongerNeeded();
            }

            model.MarkAsNoLongerNeeded();
        }

        private static void KillNearbyHostiles()
        {
            var player = Game.Player.Character;
            foreach (var ped in World.GetNearbyPeds(player, 120f))
            {
                if (ped == null || !ped.Exists() || ped.IsDead) continue;
                if (ped.GetRelationshipWithPed(player) != Relationship.Hate) continue;
                ped.Kill();
            }
        }

        // ---------- drawing ----------

        private void Draw(Page page)
        {
            const float x = 40f;
            const float width = 420f;
            float y = 60f;

            new ContainerElement(new PointF(x, y), new SizeF(width, 30f),
                Color.FromArgb(230, 18, 20, 24)).Draw();
            new TextElement(page.Title, new PointF(x + 10f, y + 6f), 0.34f,
                Color.FromArgb(235, 232, 168, 56)).Draw();

            y += 32f;

            int first = Math.Max(0, Math.Min(page.Index - VisibleRows / 2, page.Items.Count - VisibleRows));
            if (first < 0) first = 0;

            for (int i = first; i < Math.Min(first + VisibleRows, page.Items.Count); i++)
            {
                var item = page.Items[i];
                bool selected = i == page.Index;

                new ContainerElement(new PointF(x, y), new SizeF(width, 26f),
                    selected ? Color.FromArgb(235, 56, 62, 74) : Color.FromArgb(200, 24, 26, 30)).Draw();

                new TextElement(item.Label, new PointF(x + 10f, y + 4f), 0.30f,
                    selected ? Color.FromArgb(245, 245, 245, 245) : Color.FromArgb(215, 190, 194, 200)).Draw();

                string value = item.Value?.Invoke() ?? "";
                if (!string.IsNullOrEmpty(value))
                {
                    new TextElement(value, new PointF(x + width - 10f, y + 4f), 0.28f,
                        Color.FromArgb(220, 150, 200, 160))
                    {
                        Alignment = Alignment.Right
                    }.Draw();
                }

                y += 27f;
            }

            new TextElement(
                (page.Index + 1) + "/" + page.Items.Count +
                "   Right stick: move/adjust | A: select | B: back | " + _config.DevMenuKey + " close",
                new PointF(x, y + 4f), 0.26f, Color.FromArgb(190, 150, 156, 166)).Draw();
        }

        private static string Title(MissionDefinition mission)
        {
            string title = mission.Title ?? "";
            return title.Length > 28 ? title.Substring(0, 27) + "…" : title;
        }

        private string Status(MissionDefinition mission)
        {
            if (_state.IsComplete(mission.Id)) return "done";
            return mission.IsPlayable ? "playable" : "written";
        }

        // ---------- menu model ----------

        private sealed class Page
        {
            public Page(string title)
            {
                Title = title;
            }

            public string Title { get; }
            public List<Item> Items { get; } = new List<Item>();
            public int Index { get; private set; }

            public Item Selected => Items.Count > 0 ? Items[Index] : null;

            public void Add(string label, Func<string> value, Action action = null, Action<int> adjust = null)
            {
                Items.Add(new Item { Label = label, Value = value, Action = action, Adjust = adjust });
            }

            public void Move(int delta)
            {
                if (Items.Count == 0) return;
                Index = (Index + delta) % Items.Count;
                if (Index < 0) Index += Items.Count;
            }
        }

        private sealed class Item
        {
            public string Label;
            public Func<string> Value;
            public Action Action;
            public Action<int> Adjust;
        }
    }
}

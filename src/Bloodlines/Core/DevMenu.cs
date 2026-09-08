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

        private readonly Stack<Page> _stack = new Stack<Page>();

        public DevMenu(ModConfig config, CrewRoster crew, SwitchController switching,
            AbilityController abilities, MissionManager missions, MissionCatalog catalog,
            CampaignState state, DialogueDirector dialogue, CampaignData data, SurveyMode survey)
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
        }

        public bool IsOpen { get; private set; }

        public void Toggle()
        {
            IsOpen = !IsOpen;
            if (!IsOpen)
            {
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

        public void Update()
        {
            if (!IsOpen || _stack.Count == 0) return;

            // Stop the player shooting or swinging while the menu has focus.
            Game.DisableControlThisFrame(GTA.Control.Attack);
            Game.DisableControlThisFrame(GTA.Control.Attack2);
            Game.DisableControlThisFrame(GTA.Control.Aim);
            Game.DisableControlThisFrame(GTA.Control.MeleeAttack1);

            Draw(_stack.Peek());
        }

        // ---------- pages ----------

        private Page BuildRoot()
        {
            var page = new Page("Bloodlines — dev menu");

            page.Add("Missions", () => _catalog.Playable.Count() + " playable",
                () => _stack.Push(BuildMissionList()));
            page.Add("Running mission", () => _missions.IsRunning ? _missions.CurrentTitle : "none",
                () => _stack.Push(BuildMissionControl()));
            page.Add("Crew", () => _crew.IsDeployed ? _crew.Active.DisplayName : "not deployed",
                () => _stack.Push(BuildCrew()));
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

        private Page BuildMissionControl()
        {
            var page = new Page("Running mission");

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
                if (_crew.IsDeployed) _crew.Dismiss();
                else
                {
                    var player = Game.Player.Character;
                    _crew.Deploy(CrewSlot.Ice, player.Position, player.Heading);
                }
            });

            foreach (var protagonist in Protagonist.All)
            {
                var slot = protagonist.Slot;
                page.Add("Switch to " + protagonist.FirstName, () => _crew.ActiveSlot == slot ? "active" : "",
                    () => _switching.TrySwitch(slot));
            }

            foreach (var protagonist in Protagonist.All)
            {
                var slot = protagonist.Slot;
                page.Add("Deploy solo as " + protagonist.FirstName, () => "", () =>
                {
                    var player = Game.Player.Character;
                    _crew.DeploySolo(slot, player.Position, player.Heading);
                });
            }

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

            page.Add("Refill ability meter", () => Math.Round(_abilities.Meter * 100) + "%",
                () => _abilities.Refill());
            page.Add("Toggle ability", () => _abilities.IsActive ? "on" : "off", () => _abilities.Toggle());
            return page;
        }

        private Page BuildWorld()
        {
            var page = new Page("World");

            page.Add("Wanted level", () => Game.Player.WantedLevel.ToString(), null,
                delta => Game.Player.WantedLevel = Math.Max(0, Math.Min(5, Game.Player.WantedLevel + delta)));
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
            var page = new Page("Survey — Enter starts, then capture with the capture key");

            page.Add("Survey everything", () => _survey.IsActive ? "running" : "", () =>
            {
                _survey.Start();
                Toggle();
            });

            foreach (var mission in _catalog.Playable)
            {
                var captured = mission;
                page.Add("Survey " + captured.Id + " only", () => "", () =>
                {
                    _survey.Start(captured.Id);
                    Toggle();
                });
            }

            page.Add("Stop and write the ini", () => "", () => _survey.Stop());
            return page;
        }

        // ---------- actions ----------

        private void TeleportToMissionStart()
        {
            var mission = _missions.LastAttempted ?? _state.NextPlayable(_catalog);
            if (mission == null) return;

            // The bible's surveyed anchor for the prologue, otherwise the mission's own
            // estimated start from the locations file.
            if (_data.TryAnchor("Ice: Roost 4", out var anchor, out float heading) && mission.Id == "M01")
            {
                Game.Player.Character.Position = anchor;
                Game.Player.Character.Heading = heading;
                GameUtils.Subtitle("~g~Warped to " + mission.Id + " start.", 2500);
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
                "   ↑↓ move · ←→ adjust · Enter select · Backspace back · " + _config.DevMenuKey + " close",
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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Campaign overlay. Owns UI input only, never actor tasks, cameras or time.</summary>
    public sealed class CampaignPhone
    {
        public enum App { Home, Messages, Crew, Job, Wallet, News, Help, Garage, Commands, Journal, Progression, Alerts, Planning, Vehicles, Properties, Settings }
        private static CampaignPhone _focused;
        private static int _releaseUntil;
        public static bool BlocksGameplayInput => _focused != null || Game.GameTime < _releaseUntil;
        private readonly CampaignState _state;
        private readonly CampaignDispatches _dispatches;
        private readonly Func<string> _job;
        private readonly Func<string> _route;
        private readonly Func<CrewSlot, string> _status;
        private readonly ControllerNavigation _navigation = new ControllerNavigation();
        private readonly string _key;
        private readonly string _artDirectory;
        private readonly PhoneOverlay _overlay;
        private readonly Dictionary<string, GTA.UI.CustomSprite> _art = new Dictionary<string, GTA.UI.CustomSprite>();
        private readonly HashSet<string> _missingArt = new HashSet<string>();
        private CrewSlot _owner;
        private int _selection, _scroll;
        private bool _detail, _waitRelease, _routeRequested;
        private string _notice = "";
        private string _selectedMessage;
        private static readonly string[] Apps = { "Messages", "Crew", "Current job", "Wallet", "Weazel News", "Help", "Garage", "Crew orders", "Journal", "Progression", "Alerts", "Planning", "Vehicles", "Properties", "Settings" };
        private static readonly App[] DefaultOrder = { App.Garage, App.Commands, App.Journal, App.Progression, App.Alerts, App.Planning, App.Vehicles, App.Properties, App.Messages, App.Crew, App.Job, App.Wallet, App.News, App.Help, App.Settings };
        private readonly List<App> _order = new List<App>();
        private App[] _beforeArrange;
        private App _arrangedApp;
        public bool IsArranging => _beforeArrange != null;
        public App HomeAppAt(int index) => _order[index];
        public int HomeIndexOf(App app) => _order.IndexOf(app);
        private void LoadOrder()
        {
            _order.Clear();
            foreach (string id in _state.PhoneAppOrder)
                if (Enum.TryParse(id, out App app) && DefaultOrder.Contains(app) && !_order.Contains(app)) _order.Add(app);
            foreach (var app in DefaultOrder) if (!_order.Contains(app)) _order.Add(app);
        }
        public void Arrange()
        {
            if (!IsOpen || Page != App.Home || IsArranging) return;
            _beforeArrange = _order.ToArray(); _arrangedApp = _order[_selection]; _notice = "";
        }
        private void CancelArrange()
        {
            if (!IsArranging) return;
            _order.Clear(); _order.AddRange(_beforeArrange); _beforeArrange = null;
            _selection = _order.IndexOf(_arrangedApp);
        }
        public CampaignHub Hub { get; set; }
        private List<PhoneEntry> _entries = new List<PhoneEntry>();
        private string _selectedEntry, _queuedEntry, _quote;
        private bool _confirming;
        private bool HubPage => Page >= App.Garage || Page == App.Crew && Hub != null;
        private PhoneEntry Entry => _entries.FirstOrDefault(e => e.Id == _selectedEntry);
        private sealed class Folder { public Func<List<PhoneEntry>> Rows; public string Title; public int ParentSelection; }
        private readonly Stack<Folder> _folders = new Stack<Folder>();
        public const int AppsPerPage = 8;
        private void RefreshEntries() { if (HubPage) _entries = _folders.Count > 0 ? _folders.Peek().Rows() : Hub?.Entries(Page) ?? new List<PhoneEntry>(); }
        public bool IsOpen { get; private set; }
        public App Page { get; private set; }
        public int Selection => _selection;
        public int Scroll => _scroll;
        public string Notice => _notice;

        public CampaignPhone(CampaignState state, CampaignDispatches dispatches, Func<string> job,
            Func<string> route, Func<CrewSlot, string> status, string key, string artDirectory = null)
        { _state = state; _dispatches = dispatches; _job = job; _route = route; _status = status; _key = key; _artDirectory = artDirectory; _overlay = new PhoneOverlay(artDirectory); }

        public void Open(CrewSlot owner)
        {
            if (IsOpen) return;
            _focused?.Close(); _focused = this; _owner = owner;
            LoadOrder(); _folders.Clear();
            IsOpen = true; Page = App.Home; _selection = 0; _scroll = 0;
            _detail = false; _confirming = false; _queuedEntry = null; _notice = ""; _waitRelease = true; _routeRequested = false;
            _navigation.Reset();
        }

        public void Close()
        {
            if (!IsOpen) return;
            CancelArrange(); _folders.Clear();
            IsOpen = false; _routeRequested = false; _queuedEntry = null; _confirming = false;
            if (_focused == this) _focused = null;
            _releaseUntil = Game.GameTime + 180;
            _navigation.Reset();
        }

        public void Shutdown() { Close(); _releaseUntil = 0; }

        // D-pad / A / B aliases in the game's player, phone and scripted input maps.
        // https://docs.fivem.net/docs/game-references/controls/
        private static readonly HashSet<Control> ReservedControls = new HashSet<Control> {
            Control.WeaponWheelNext, Control.WeaponWheelPrev, Control.SkipCutscene, Control.CharacterWheel, Control.MultiplayerInfo,
            Control.Sprint, Control.Phone, Control.SniperZoomInSecondary, Control.SniperZoomOutSecondary, Control.Reload,
            Control.Talk, Control.Detonate, Control.HUDSpecial, Control.Context, Control.ContextSecondary,
            Control.WeaponSpecial2, Control.DropAmmo, Control.ThrowGrenade, Control.VehicleAttack2, Control.VehicleDuck,
            Control.VehicleHeadlight, Control.VehicleCinCam, Control.VehicleRadioWheel, Control.VehicleRoof, Control.VehicleGrapplingHook,
            Control.VehicleShuffle, Control.VehicleDropProjectile, Control.VehicleFlyAttack, Control.VehicleFlySelectNextWeapon, Control.VehicleFlyVerticalFlightMode,
            Control.VehicleFlyDuck, Control.VehicleSubDescend, Control.VehiclePushbikePedal, Control.VehiclePushbikeSprint, Control.MeleeAttackLight,
            Control.MeleeAttackHeavy, Control.ParachuteSmoke, Control.SaveReplayClip, Control.PhoneUp, Control.PhoneDown,
            Control.PhoneLeft, Control.PhoneRight, Control.PhoneSelect, Control.PhoneCancel, Control.FrontendDown,
            Control.FrontendUp, Control.FrontendLeft, Control.FrontendRight, Control.FrontendRdown, Control.FrontendRright,
            Control.FrontendAccept, Control.FrontendCancel, Control.FrontendEndscreenAccept, Control.ScriptRDown, Control.ScriptRRight,
            Control.ScriptPadUp, Control.ScriptPadDown, Control.ScriptPadLeft, Control.ScriptPadRight, Control.CreatorAccept,
            Control.RappelJump, Control.PrevWeapon, Control.NextWeapon, Control.MeleeAttack1, Control.MeleeAttack2,
            Control.ReplayStartStopRecording, Control.ReplayPause, Control.ReplayNewmarker, Control.ReplayScreenshot, Control.ReplayAdvance,
            Control.ReplayBack, Control.ReplayTools, Control.ReplayShowhotkey, Control.ReplayCycleMarkerLeft, Control.ReplayCycleMarkerRight,
            Control.VehicleHydraulicsControlToggle, Control.SwitchVisor, Control.VehicleMeleeHold, Control.VehicleParachute, Control.VehicleBikeWings,
            Control.VehicleFlyBombBay, Control.VehicleFlyCounter, Control.VehicleFlyTransform };
        public static bool Consumes(Control control) => BlocksGameplayInput && ReservedControls.Contains(control);

        private static bool Held(Control control) => Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, (int)control) || Game.IsControlPressed(control);
        private static bool Hit(Control control) => Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, (int)control) || Game.IsControlJustPressed(control);

        /// <summary>
        /// A scoped sniper zooms on the d-pad, and the pad's up is the same button the
        /// phone opens on. While the scope camera is up the button belongs to the scope:
        /// opening a phone in the middle of a shot is never what the press meant. The
        /// keyboard key still opens it, and so does the pad the moment the scope drops.
        /// </summary>
        public static bool ScopeOwnsTheDpad
        {
            get
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists() || !player.IsAiming) return false;
                uint weapon = Function.Call<uint>(Hash.GET_SELECTED_PED_WEAPON, player);
                return Function.Call<uint>(Hash.GET_WEAPONTYPE_GROUP, weapon) ==
                       unchecked((uint)Game.GenerateHash("GROUP_SNIPER"));
            }
        }

        // Run before mission/shop input. Drawing and route actions run after mission updates.
        public void Input(bool enabled, bool deployed, bool available, CrewSlot owner)
        {
            if (!enabled || !deployed || !available || (IsOpen && _owner != owner)) Close();
            if (enabled && deployed) Game.DisableControlThisFrame(Control.Phone);
            if (enabled && deployed && available && !IsOpen && !BlocksGameplayInput && !ScopeOwnsTheDpad && Hit(Control.Phone))
                Open(owner);
            if (BlocksGameplayInput)
                foreach (var control in ReservedControls)
                {
                    Game.DisableControlThisFrame(control);
                    Function.Call(Hash.DISABLE_CONTROL_ACTION, 1, (int)control, true);
                    Function.Call(Hash.DISABLE_CONTROL_ACTION, 2, (int)control, true);
                }
            if (!IsOpen) return;
            RefreshEntries();
            if (_waitRelease)
            {
                _waitRelease = Held(Control.Phone) || Held(Control.FrontendUp) || Held(Control.FrontendAccept) || Held(Control.FrontendCancel);
                return;
            }
            if (Hit(Control.FrontendCancel)) { Back(); return; }
            var direction = _navigation.Update(0, 0, Game.GameTime, Held(Control.FrontendUp),
                Held(Control.FrontendDown), Held(Control.FrontendLeft), Held(Control.FrontendRight));
            Move(direction);
            if (Hit(Control.FrontendAccept)) Select();
        }

        public CampaignDispatches.Message[] Messages => _dispatches.Inbox.Where(m => (m.Sender == "Weazel News") == (Page == App.News)).ToArray();
        private bool IsList => (Page == App.Messages || Page == App.News || Page == App.Crew || HubPage) && !_detail;
        private int ContactCount => _state.IsComplete("SM03") ? 4 : 3;
        private int RowCount => HubPage ? _entries.Count : Page == App.Crew ? ContactCount : Messages.Length;

        public void Move(MenuDirection direction)
        {
            if (!IsOpen || direction == MenuDirection.None) return;
            int delta = direction == MenuDirection.Up || direction == MenuDirection.Left ? -1 : 1;
            if (Page == App.Home)
            {
                if (direction == MenuDirection.Up || direction == MenuDirection.Down) delta *= 2;
                int previous = _selection;
                _selection = (_selection + delta + Apps.Length) % Apps.Length;
                if (IsArranging) { var app = _order[previous]; _order[previous] = _order[_selection]; _order[_selection] = app; }
            }
            else if (IsList) _selection = Math.Max(0, Math.Min(RowCount - 1, _selection + delta));
            else _scroll = Math.Max(0, Math.Min(Math.Max(0, Lines().Count - 12), _scroll + delta));
        }

        public void Select()
        {
            if (!IsOpen) return;
            if (Page == App.Home)
            {
                if (IsArranging)
                {
                    _state.PhoneAppOrder.Clear(); _state.PhoneAppOrder.AddRange(_order.Select(app => app.ToString()));
                    _state.Save(); _beforeArrange = null; return;
                }
                Page = _order[_selection]; _selection = 0; _scroll = 0; _notice = ""; RefreshEntries(); return;
            }
            if (IsList && RowCount > 0)
            {
                _selection = Math.Min(_selection, RowCount - 1);
                if (HubPage)
                {
                    var selected = _entries[_selection];
                    if (selected.Children != null) { _folders.Push(new Folder { Rows = selected.Children, Title = selected.Title, ParentSelection = _selection }); _selection = 0; _scroll = 0; _notice = ""; RefreshEntries(); return; }
                    _selectedEntry = selected.Id; Entry?.Read?.Invoke();
                }
                else if (Page != App.Crew) _selectedMessage = Messages[_selection].Mission;
                _detail = true; _scroll = 0;
            }
            else if (HubPage && Entry?.ArrangeHome == true)
            {
                Page = App.Home; _folders.Clear(); _detail = false; _selection = 0; Arrange();
            }
            else if (HubPage && Entry?.Action != null)
            {
                if (Entry.Quote != null && !_confirming) { _confirming = true; _quote = Entry.Quote; _scroll = 0; return; }
                _queuedEntry = Entry.Id;
            }
            else if (Page == App.Job) _routeRequested = true;
        }

        public void Back()
        {
            if (!IsOpen) return;
            if (IsArranging) { CancelArrange(); return; }
            _queuedEntry = null; _routeRequested = false;
            if (_confirming) { _confirming = false; _quote = null; return; }
            if (_detail) { _detail = false; _scroll = 0; _notice = ""; return; }
            if (_folders.Count > 0) { var folder = _folders.Pop(); _selection = folder.ParentSelection; _scroll = 0; RefreshEntries(); return; }
            if (Page != App.Home) { _selection = HomeIndexOf(Page); Page = App.Home; _scroll = 0; return; }
            Close();
        }

        public void FinishFrame(bool available)
        {
            if (!available) { Close(); return; }
            if (!IsOpen) return;
            RefreshEntries();
            if (_queuedEntry != null)
            {
                var entry = _entries.FirstOrDefault(e => e.Id == _queuedEntry); _queuedEntry = null;
                if (entry?.Action == null) _notice = "This action is no longer available.";
                else if (_confirming && entry.Quote != _quote) _notice = "The quote changed. Review and confirm again.";
                else _notice = entry.Action();
                _confirming = false; _quote = null; _scroll = 0; RefreshEntries();
            }
            if (_routeRequested) { _routeRequested = false; _notice = _route(); }
            if (_overlay.TryBegin())
                Render(_overlay.Box, _overlay.Text, World.CurrentTimeOfDay.ToString(@"hh\:mm"), DrawArt);
            else Render((x, y, w, h, color) => new GTA.UI.ContainerElement(new PointF(x, y), new SizeF(w, h), color).Draw(),
                (text, x, y, size, color) => new GTA.UI.TextElement(text, new PointF(x, y), size, color)
                    { Font = GTA.UI.Font.ChaletLondon }.Draw(),
                World.CurrentTimeOfDay.ToString(@"hh\:mm"));
        }

        public string Body()
        {
            if (HubPage)
            {
                var entry = Entry;
                string fullPreview = entry != null && (WrapMeasured(entry.Title, 188, .27f).Count > 2 || WrapMeasured(entry.Subtitle, 188, .22f).Count > 2)
                    ? entry.Title + "\n" + entry.Subtitle + "\n\n" : "";
                return (_confirming ? "CONFIRM REQUEST\n\n" : "") + (_notice.Length > 0 ? _notice + "\n\n" : "") + fullPreview + (entry?.Body ?? "This entry is no longer available.");
            }
            if (Page == App.Job) return _job();
            if (Page == App.Wallet) return "CREW FUNDS\n$" + _state.CashOnHand.ToString("N0") +
                "\n\nShared spending balance for weapons, upgrades, clothes and vehicles.\n\nCompleted jobs: " + _state.CompletedCount +
                "\n\nOwned vehicles: " + _state.Vehicles.Count + "\nOwned garages: " + _state.Garages.Count;
            if (Page == App.Help) return "D-pad Up or " + _key + ": open phone.\n\nD-pad / arrow keys: navigate or scroll. Continue past the eighth app to reach page two. Garage and crew tools start on page one.\nA / Enter: select.\nB / Backspace: back; from the home screen, close.\n" + _key + ": close immediately.\n\nThe world keeps moving. D-pad and A/B belong to the phone; other controller buttons, movement and camera stay available. Close the phone to use D-pad interactions or switch brothers.\n\nOpen Settings, choose Arrange home icons and press A to begin. R on keyboard also picks up the highlighted home app. Move it with the D-pad / arrows, including across pages. A / Enter saves the order; B / Backspace cancels. The saved layout is shared by the three brothers.\n\nCalls in story scenes remain scripted. Crew contacts show roles and current status.";
            if (Page == App.Crew && _detail)
            {
                if (_selection == 3) return "KJ\nGuess's friend and racing contact.\n\nHis delivery service remains available at the garage. Story appearances unlock through campaign progress.";
                var hero = Protagonist.Of((CrewSlot)_selection);
                return hero.Handle + "\n" + hero.Role + "\n\n" + _status(hero.Slot) + "\n\nAbility: " + hero.AbilityName + "\n\nClose the phone and hold D-pad Down to use the character wheel. Mission handoffs still follow the current objective.";
            }
            if ((Page == App.Messages || Page == App.News) && _detail)
            {
                var message = Messages.FirstOrDefault(m => m.Mission == _selectedMessage);
                return message == null ? "This message is no longer available." : message.Sender + "\n" + message.Title + "\n\n" + message.Text;
            }
            return "";
        }

        // Remove GTA formatting tokens before measuring. Preserve paragraph breaks and long words.
        public static List<string> Wrap(string text, int width = 34)
        {
            width = Math.Max(1, width);
            var lines = new List<string>();
            text = System.Text.RegularExpressions.Regex.Replace(text ?? "", "~[^~]*~", "");
            foreach (string paragraph in text.Replace("\r", "").Split('\n'))
            {
                string rest = paragraph;
                while (rest.Length > width)
                {
                    int cut = rest.LastIndexOf(' ', width); if (cut < 1) cut = width;
                    lines.Add(rest.Substring(0, cut)); rest = rest.Substring(cut).TrimStart();
                }
                lines.Add(rest);
            }
            return lines;
        }
        public static List<string> WrapMeasured(string text, float width, float size)
        {
            var lines = new List<string>();
            text = System.Text.RegularExpressions.Regex.Replace(text ?? "", "~[^~]*~", "");
            foreach (string paragraph in text.Replace("\r", "").Split('\n'))
            {
                string rest = paragraph;
                while (PhoneOverlay.MeasureText(rest, size) > width && rest.Length > 0)
                {
                    int cut = 0;
                    while (cut < rest.Length && PhoneOverlay.MeasureText(rest.Substring(0, cut + 1), size) <= width) cut++;
                    cut = Math.Max(1, cut);
                    int space = rest.LastIndexOf(' ', Math.Min(cut, rest.Length - 1));
                    if (space > 0) cut = space;
                    lines.Add(rest.Substring(0, cut)); rest = rest.Substring(cut).TrimStart();
                }
                lines.Add(rest);
            }
            return lines;
        }
        private static List<string> PreviewLines(string text, float size)
        {
            var lines = WrapMeasured(text, 188, size);
            if (lines.Count <= 2) return lines;
            string last = lines[1];
            while (last.Length > 0 && PhoneOverlay.MeasureText(last + "...", size) > 188) last = last.Substring(0, last.Length - 1);
            return new List<string> { lines[0], last.TrimEnd() + "..." };
        }
        private List<string> Lines() => WrapMeasured(Body(), 228, .27f);
        private static string Short(string text, int length) => text.Length <= length ? text : text.Substring(0, length - 3) + "...";

        private bool DrawArt(string name, float x, float y, float width, float height)
        {
            if (string.IsNullOrEmpty(_artDirectory) || _missingArt.Contains(name)) return false;
            try
            {
                if (!_art.TryGetValue(name, out var sprite))
                {
                    string path = Path.Combine(_artDirectory, "phone-" + name + ".png");
                    if (!File.Exists(path)) { _missingArt.Add(name); return false; }
                    _art[name] = sprite = new GTA.UI.CustomSprite(path, new SizeF(width, height), new PointF(x, y));
                }
                sprite.Position = new PointF(x, y); sprite.Size = new SizeF(width, height); sprite.Draw(); return true;
            }
            catch (Exception ex) { _missingArt.Add(name); Logger.Warn("Phone art unavailable: " + name + " / " + ex.Message); return false; }
        }

        /// <summary>Shared layout; baked textures keep rounded surfaces and wallpaper out of the tick's drawing workload.</summary>
        public void Render(Action<float, float, float, float, Color> box, Action<string, float, float, float, Color> label,
            string clock, Func<string, float, float, float, float, bool> sprite = null)
        {
            string hero = _owner.ToString().ToLowerInvariant();
            Color accent = _owner == CrewSlot.Guess ? Color.FromArgb(242, 180, 110) : _owner == CrewSlot.Ice ? Color.FromArgb(120, 191, 249) : Color.FromArgb(110, 220, 185);
            Color ink = Color.FromArgb(17, 24, 34), muted = Color.FromArgb(178, 195, 211), white = Color.FromArgb(241, 247, 253);
            void Surface(string name, float x, float y, float width, float height, Color fallback)
            { if (sprite == null || !sprite(name, x, y, width, height)) box(x, y, width, height, fallback); }
            Surface(hero, 944, 126, 296, 554, ink);
            label("B L O O D L I N E S", 966, 153, .18f, muted);
            if (Page == App.Home)
            {
                label(clock, 965, 189, .66f, white);
                label(IsArranging ? "Move " + Apps[(int)_order[_selection]-1] : Protagonist.Of(_owner).Handle, 968, 240, IsArranging ? .28f : .33f, accent);
                label("APPS " + (_selection / AppsPerPage + 1) + " / 2", 1120, 251, .18f, muted);
                var icons = new[] { "messages", "crew", "job", "wallet", "news", "help", "garage", "orders", "journal", "progression", "alerts", "planning", "vehicles", "properties", "settings" };
                for (int i = _selection / AppsPerPage * AppsPerPage; i < Math.Min(Apps.Length, (_selection / AppsPerPage + 1) * AppsPerPage); i++)
                {
                    int appIndex = (int)_order[i] - 1;
                    float x = 972 + i % 2 * 134, y = 272 + (i % AppsPerPage) / 2 * 78;
                    bool selected = i == _selection;
                    Surface(selected ? "tile-" + hero : "tile", x, y, 108, 70, Color.FromArgb(37, 47, 61));
                    if (sprite == null || !sprite("icon-" + icons[appIndex], x + 37, y + 5, 34, 34))
                        label(new[] { "...", "3", ">", "$", "N", "?", "G", "C", "J", "+", "!", "P", "V", "H", "S" }[appIndex], x + 40, y + 10, .32f, white);
                    label(Apps[appIndex], x + 10, y + 46, .245f, selected ? white : muted);
                    if (_order[i] == App.Alerts && Hub?.Unread > 0) label(Hub.Unread.ToString(), x + 83, y + 7, .23f, accent);
                }
                Surface("balance", 964, 586, 256, 40, ink);
                label("CREW FUNDS", 976, 590, .18f, muted);
                label("$" + _state.CashOnHand.ToString("N0"), 976, 603, .29f, white);
            }
            else
            {
                label(Apps[(int)Page - 1], 966, 187, .45f, white);
                label(_folders.Count > 0 ? Short(_folders.Peek().Title,  30) : Protagonist.Of(_owner).Handle + " / Crew network", 968, 224, .22f, accent);
                if (IsList)
                {
                    var messages = Messages;
                    _selection = Math.Max(0, Math.Min(_selection, RowCount - 1));
                    if (RowCount == 0)
                    {
                        Surface("reading", 964, 260, 256, 304, ink);
                        label("All caught up", 978, 285, .32f, white);
                        label(HubPage ? "Nothing recorded here yet." : "Updates arrive after", 978, 323, .26f, muted);
                        if (!HubPage) label("completed jobs.", 978, 347, .26f, muted);
                    }
                    int top = Math.Max(0, _selection - 3);
                    for (int i = top; i < Math.Min(RowCount, top + 4); i++)
                    {
                        float y = 260 + (i - top) * 80;
                        Surface(i == _selection ? "row-" + hero : "row", 964, y, 256, 76, ink);
                        string title = HubPage ? _entries[i].Title : Page == App.Crew ? (i == 3 ? "KJ" : Protagonist.Of((CrewSlot)i).Handle) : messages[i].Sender;
                        string subtitle = HubPage ? _entries[i].Subtitle : Page == App.Crew ? (i == 3 ? "Racing / delivery" : _status((CrewSlot)i)) : messages[i].Title;
                        Surface("avatar", 974, y + 10, 30, 30, ink);
                        label(title.Substring(0, 1), 981, y + 13, .29f, accent);
                        int lineY = 4;
                        foreach (string line in PreviewLines(title, .27f)) { label(line, 1015, y + lineY, .27f, white); lineY += 18; }
                        lineY = 42;
                        foreach (string line in PreviewLines(subtitle, .22f)) { label(line, 1015, y + lineY, .22f, muted); lineY += 14; }
                    }
                    if (RowCount > 0) label((_selection + 1) + " of " + RowCount, 970, 607, .22f, muted);
                }
                else
                {
                    Surface("reading", 964, 260, 256, 304, ink);
                    var lines = Lines(); _scroll = Math.Min(_scroll, Math.Max(0, lines.Count - 12));
                    for (int i = _scroll; i < Math.Min(lines.Count, _scroll + 12); i++)
                        label(lines[i], 978, 272 + (i - _scroll) * 23, .27f, white);
                    if (lines.Count > 12) label("D-PAD Scroll  " + (_scroll + 1) + "-" + Math.Min(lines.Count, _scroll + 12) + " / " + lines.Count, 978, 545, .19f, muted);
                    if (Page == App.Job || (HubPage && (Entry?.Action != null || Entry?.ArrangeHome == true)) || (HubPage && _notice.Length > 0))
                    {
                        Surface("button-" + hero, 966, 575, 250, 36, accent);
                        label(Page == App.Job ? "A / ENTER   Show destination" : "A   " + (_confirming ? "Confirm request" : Entry?.Button ?? "Unavailable"), 977, 581, .245f, ink);
                        if (_notice.Length > 0) label("Result shown above", 977, 619, .19f, muted);
                    }
                }
            }
            if (Page == App.Home)
            {
                label(IsArranging ? "D-PAD  Move icon   A Save   B Cancel" : "D-PAD  Move    A  Open    B  Back", 970, 633, .19f, muted);
                label(IsArranging ? "Move past an edge to change pages" : "Settings: Arrange apps  /  R", 970, 649, .18f, muted);
            }
            else label(IsList ? "D-PAD Move    A Details    B Back" : "D-PAD Scroll    A Select    B Back", 970, 650, .19f, muted);
        }
    }
}

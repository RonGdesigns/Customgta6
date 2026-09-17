using System;
using System.Collections.Generic;
using System.Drawing;
using Bloodlines.Crew;
using GTA;
using GTA.UI;

namespace Bloodlines.Core
{
    /// <summary>
    /// The quick order strip: hold one input, pick a brother left and right and an order up
    /// and down, release to send. The phone's crew page is where a plan is made; this is
    /// for the middle of a fight, when Ron is in the gun bed and needs somebody at the
    /// wheel now. Time slows while it is up, the way the character wheel slows it.
    ///
    /// The strip decides nothing about what an order means. It asks
    /// <see cref="CrewOrders.Available"/> what makes sense for the selected brother this
    /// frame - which changes as he moves, so it is asked every frame - and hands the pick
    /// to <see cref="CompanionController.Order"/> on release. What comes back is echoed on
    /// the HUD in the brother's color for a moment, so the player knows it landed without
    /// opening anything.
    /// </summary>
    public sealed class CrewOrderStrip
    {
        /// <summary>A press shorter than this is a tap for whatever else the button does.</summary>
        public const int HoldToOpenMs = 350;
        public const int EchoMs = 2500;
        public const float Left = 470f, Top = 546f, Width = 340f;
        private const float SlowScale = .2f;

        private static readonly Color Panel = Color.FromArgb(170, 12, 14, 18);
        private static readonly Color Muted = Color.FromArgb(255, 178, 195, 211);
        private static readonly Color Pick = Color.FromArgb(255, 245, 222, 90);

        private readonly List<CrewSlot> _brothers = new List<CrewSlot>();
        private readonly List<CrewOrder> _orders = new List<CrewOrder>();
        private int _brother, _order;
        private float _previousScale;
        private int _padSince;
        private string _echo = "";
        private CrewSlot _echoSlot;
        private int _echoUntil;
        private bool _axisLatched;

        /// <summary>Whether a map waypoint is set. Injected so the strip compiles without the world.</summary>
        public Func<bool> HasWaypoint { get; set; }
        /// <summary>Where a sent order is also recorded (the phone's alert log).</summary>
        public Action<CrewSlot, string> Sent { get; set; }

        public bool IsOpen { get; private set; }
        public IReadOnlyList<CrewSlot> Brothers => _brothers;
        public IReadOnlyList<CrewOrder> Orders => _orders;
        public CrewSlot? SelectedBrother => _brothers.Count == 0 ? (CrewSlot?)null : _brothers[_brother];
        public CrewOrder SelectedOrder => _orders.Count == 0 ? CrewOrder.None : _orders[_order];
        public string EchoText => Game.GameTime < _echoUntil ? _echo : "";
        public bool EchoShowing => Game.GameTime < _echoUntil;

        /// <summary>Open the strip for the brothers who can take an order right now.</summary>
        public bool Open(CrewRoster crew)
        {
            if (IsOpen) return true;
            _brothers.Clear();
            foreach (var hero in Protagonist.All)
            {
                if (hero.Slot == crew.ActiveSlot) continue;
                var ped = crew.PedFor(hero.Slot);
                if (ped != null && ped.Exists() && !ped.IsDead) _brothers.Add(hero.Slot);
            }
            if (_brothers.Count == 0) return false;
            _brother = 0; _order = 0; _axisLatched = false;
            Refresh(crew);
            _previousScale = Game.TimeScale;
            Game.TimeScale = Math.Min(_previousScale, SlowScale);
            IsOpen = true;
            return true;
        }

        /// <summary>Close without sending. Safe when nothing is open.</summary>
        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Game.TimeScale = _previousScale;
        }

        /// <summary>Re-read what the selected brother can be told, keeping the pick where it was.</summary>
        public void Refresh(CrewRoster crew)
        {
            var slot = SelectedBrother;
            var was = SelectedOrder;
            _orders.Clear();
            if (!slot.HasValue) return;
            var leader = crew.PedFor(crew.ActiveSlot);
            _orders.AddRange(CrewOrders.Available(crew.PedFor(slot.Value), leader, CrewOrders.Subject(leader), HasWaypoint?.Invoke() == true));
            int at = _orders.IndexOf(was);
            _order = at >= 0 ? at : Math.Min(_order, Math.Max(0, _orders.Count - 1));
        }

        /// <summary>Move the pick: left and right change the brother, up and down the order.</summary>
        public void Move(MenuDirection direction, CrewRoster crew)
        {
            if (!IsOpen) return;
            switch (direction)
            {
                case MenuDirection.Left: if (_brothers.Count > 0) { _brother = (_brother + _brothers.Count - 1) % _brothers.Count; Refresh(crew); } break;
                case MenuDirection.Right: if (_brothers.Count > 0) { _brother = (_brother + 1) % _brothers.Count; Refresh(crew); } break;
                case MenuDirection.Up: if (_orders.Count > 0) _order = (_order + _orders.Count - 1) % _orders.Count; break;
                case MenuDirection.Down: if (_orders.Count > 0) _order = (_order + 1) % _orders.Count; break;
            }
        }

        /// <summary>Send the pick and close. False when nothing was sendable or the controller refused it.</summary>
        public bool Send(CrewRoster crew)
        {
            if (!IsOpen) return false;
            var slot = SelectedBrother; var order = SelectedOrder;
            Close();
            if (!slot.HasValue || order == CrewOrder.None) return false;
            var leader = crew.PedFor(crew.ActiveSlot);
            if (!crew.CompanionAI.Order(slot.Value, order, CrewOrders.Subject(leader)))
            {
                Echo(slot.Value, Protagonist.Of(slot.Value).Handle + ": can't right now");
                return false;
            }
            string text = CrewOrders.Echo(order, Protagonist.Of(slot.Value).Handle);
            Echo(slot.Value, text);
            Sent?.Invoke(slot.Value, text);
            return true;
        }

        private void Echo(CrewSlot slot, string text) { _echo = text; _echoSlot = slot; _echoUntil = Game.GameTime + EchoMs; }

        /// <summary>
        /// The controller gesture, one call per frame: held past <see cref="HoldToOpenMs"/> opens,
        /// release sends. A shorter press never opens, so a tap still does whatever the button
        /// does for the game. Returns true while the button is owned by the strip.
        /// </summary>
        public bool HandlePad(bool held, float x, float y, CrewRoster crew)
        {
            if (!held)
            {
                bool owned = IsOpen;
                if (IsOpen) Send(crew);
                _padSince = 0;
                return owned;
            }
            if (_padSince == 0) _padSince = Game.GameTime;
            if (!IsOpen)
            {
                if (Game.GameTime - _padSince < HoldToOpenMs) return false;
                if (!Open(crew)) { return false; }
            }
            // One step per deflection of the stick; it has to come back to center between steps.
            float magnitude = Math.Max(Math.Abs(x), Math.Abs(y));
            if (magnitude < .3f) _axisLatched = false;
            else if (!_axisLatched && magnitude >= .6f)
            {
                _axisLatched = true;
                Move(Math.Abs(x) >= Math.Abs(y) ? (x < 0 ? MenuDirection.Left : MenuDirection.Right) : (y < 0 ? MenuDirection.Up : MenuDirection.Down), crew);
            }
            return true;
        }

        /// <summary>Draw the strip while it is up, and the echo after an order lands.</summary>
        public void Draw(CrewRoster crew)
        {
            if (!IsOpen)
            {
                if (EchoShowing) DrawEcho();
                return;
            }
            Refresh(crew);
            float height = 30f + 22f + _orders.Count * 19f + 8f;
            new ContainerElement(new PointF(Left - 10f, Top - 8f), new SizeF(Width, height), Panel).Draw();
            new TextElement("CREW ORDER", new PointF(Left, Top), .2f, Muted).Draw();
            float x = Left + 110f;
            for (int i = 0; i < _brothers.Count; i++)
            {
                var slot = _brothers[i];
                var color = CrewColors.Of(slot);
                bool picked = i == _brother;
                new ContainerElement(new PointF(x, Top + 4f), new SizeF(10f, 10f), color).Draw();
                new TextElement(Protagonist.Of(slot).Handle, new PointF(x + 14f, Top), .24f, picked ? Color.White : Muted).Draw();
                if (picked) new ContainerElement(new PointF(x, Top + 19f), new SizeF(76f, 2f), color).Draw();
                x += 90f;
            }
            float y = Top + 30f;
            for (int i = 0; i < _orders.Count; i++)
            {
                bool picked = i == _order;
                new TextElement((picked ? "> " : "   ") + CrewOrders.Label(_orders[i]), new PointF(Left, y), .24f, picked ? Pick : Muted).Draw();
                y += 19f;
            }
            new TextElement("release to send", new PointF(Left + Width - 20f, Top), .18f, Muted) { Alignment = Alignment.Right }.Draw();
        }

        private void DrawEcho()
        {
            var color = CrewColors.Of(_echoSlot);
            new ContainerElement(new PointF(Left - 10f, Top + 4f), new SizeF(Width, 26f), Panel).Draw();
            new ContainerElement(new PointF(Left, Top + 11f), new SizeF(10f, 10f), color).Draw();
            new TextElement(_echo, new PointF(Left + 16f, Top + 6f), .24f, color).Draw();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Bloodlines.Crew;
using GTA;
using GTA.UI;

namespace Bloodlines.Core
{
    /// <summary>Owns only the wheel UI and its temporary time scale.</summary>
    public sealed class CharacterWheel
    {
        private readonly string _directory;
        private readonly Dictionary<CrewSlot, CustomSprite> _sprites = new Dictionary<CrewSlot, CustomSprite>();
        private float _previousScale;
        public bool IsOpen { get; private set; }
        public CrewSlot Selected { get; set; }
        public CharacterWheel(string directory) { _directory = directory; }
        public void Open(CrewSlot current)
        {
            if (IsOpen) return;
            Selected = current;
            _previousScale = Game.TimeScale;
            IsOpen = true;
            Game.TimeScale = Math.Min(_previousScale, .2f);
        }
        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Game.TimeScale = _previousScale;
        }
        public void Draw(CrewRoster crew)
        {
            if (!IsOpen) return;
            if (!_sprites.TryGetValue(Selected, out var sprite))
            {
                string path = Path.Combine(_directory, "wheel-" + Selected.ToString().ToLowerInvariant() + ".png");
                if (File.Exists(path))
                    _sprites[Selected] = sprite = new CustomSprite(path, new SizeF(190, 190), new PointF(1058, 496));
            }
            sprite?.Draw();
            Label("ICE", 1093, 624, crew.PedFor(CrewSlot.Ice));
            Label("GOHAN", 1153, 526, crew.PedFor(CrewSlot.Gohan));
            Label("GUESS", 1213, 624, crew.PedFor(CrewSlot.Guess));
            new TextElement(Protagonist.Of(Selected).Handle, new PointF(1153, 578), .26f, Color.White)
                { Alignment = Alignment.Center }.Draw();
            new TextElement("RELEASE TO SWITCH", new PointF(1153, 600), .14f, Color.LightGray)
                { Alignment = Alignment.Center }.Draw();
        }
        private static void Label(string name, float x, float y, Ped ped)
        {
            bool available = ped != null && ped.Exists() && !ped.IsDead;
            new TextElement(available ? name : name + " - DOWN", new PointF(x, y), .21f,
                available ? Color.White : Color.Gray) { Alignment = Alignment.Center }.Draw();
        }
    }
}

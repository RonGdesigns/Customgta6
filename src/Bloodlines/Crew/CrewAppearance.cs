using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Bloodlines.Core;
using GTA;
using GTA.Native;

namespace Bloodlines.Crew
{
    /// <summary>Fixed identity; clothes can change independently while off screen.</summary>
    public static class CrewAppearance
    {
        public sealed class Look
        {
            public int Face, Skin, Hair, Outfit, HairColor, BeardColor;
            public int Beard = -1;
            public bool AutoOutfits = true;
            public readonly Dictionary<string, int> Drawables = new Dictionary<string, int>();
            public readonly Dictionary<string, int> Textures = new Dictionary<string, int>();
        }
        public static readonly string[] Clothing = { "Shirt", "Undershirt", "Arms", "Pants", "Shoes", "Accessories", "Mask", "Bag", "Armor", "Decals" };
        private static readonly int[] Components = { 11, 8, 3, 4, 6, 7, 1, 5, 9, 10 };
        public static readonly string[] Props = { "Hat", "Glasses", "Earrings", "Watch", "Bracelet" };
        private static readonly int[] PropSlots = { 0, 1, 2, 6, 7 };
        private static int Wrap(int value, int count) => count > 0 ? ((value % count) + count) % count : 0;
        private static int Bounded(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
        public static int Drawable(CrewSlot slot, string field) => For(slot).Drawables.TryGetValue(field, out var n) ? n : -1;
        public static int Texture(CrewSlot slot, string field) => For(slot).Textures.TryGetValue(field, out var n) ? n : 0;
        private static readonly Dictionary<CrewSlot, Look> Looks = new Dictionary<CrewSlot, Look>();
        private static readonly Dictionary<CrewSlot, int> LastSeen = new Dictionary<CrewSlot, int>();
        private static string _savePath;
        static CrewAppearance() { ResetDefaults(); }

        private static void ResetDefaults()
        {
            Looks[CrewSlot.Ice] = new Look { Face = 19, Skin = 19, Hair = 14 };
            Looks[CrewSlot.Gohan] = new Look { Face = 6, Skin = 19, Hair = 1 };
            Looks[CrewSlot.Guess] = new Look { Face = 1, Skin = 19, Hair = 0 };
            LastSeen.Clear();
        }
        public static Look For(CrewSlot slot) => Looks[slot];
        public static void Load(string path)
        {
            ResetDefaults(); _savePath = path;
            var settings = ScriptSettings.Load(path);
            foreach (var hero in Protagonist.All)
            {
                var look = For(hero.Slot);
                look.Face = Math.Max(0, Math.Min(20, settings.GetValue<int>(hero.Handle, "Face", look.Face)));
                look.Skin = Math.Max(0, Math.Min(20, settings.GetValue<int>(hero.Handle, "Skin", look.Skin)));
                look.Hair = Math.Max(0, Math.Min(255, settings.GetValue<int>(hero.Handle, "Hair", look.Hair)));
                look.Outfit = Math.Max(0, Math.Min(2, settings.GetValue<int>(hero.Handle, "Outfit", 0)));
                look.HairColor = Bounded(settings.GetValue<int>(hero.Handle, "HairColor", 0), 0, 255);
                look.Beard = Bounded(settings.GetValue<int>(hero.Handle, "Beard", -1), -1, 254);
                look.BeardColor = Bounded(settings.GetValue<int>(hero.Handle, "BeardColor", 0), 0, 255);
                look.AutoOutfits = settings.GetValue<bool>(hero.Handle, "AutoOutfits", true);
                foreach (var fields in new[] { Clothing, Props }) foreach (var field in fields)
                {
                    look.Drawables[field] = Bounded(settings.GetValue<int>(hero.Handle, field, -1), -1, 10000);
                    look.Textures[field] = Bounded(settings.GetValue<int>(hero.Handle, field + "Texture", 0), 0, 10000);
                }
            }
        }
        public static void Apply(Ped ped, CrewSlot slot)
        {
            if (ped == null || !ped.Exists()) return;
            var look = For(slot);
            Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, ped);
            Function.Call(Hash.CLEAR_ALL_PED_PROPS, ped);
            Function.Call(Hash.SET_PED_HEAD_BLEND_DATA, ped, look.Face, look.Face, 0,
                look.Skin, look.Skin, 0, 0f, 0f, 0f, false);
            // Base-game freemode male hair: 14 dreads, 1 buzzcut, 0 bald.
            SetComponent(ped, 2, look.Hair, 0);
            int colors = Function.Call<int>(Hash.GET_NUM_PED_HAIR_TINTS);
            Function.Call(Hash.SET_PED_HAIR_TINT, ped, Wrap(look.HairColor, colors), Wrap(look.HairColor, colors));
            int beards = Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY_NUM, 1);
            int beard = look.Beard >= 0 && look.Beard < beards ? look.Beard : 255;
            Function.Call(Hash.SET_PED_HEAD_OVERLAY, ped, 1, beard, 1f);
            Function.Call(Hash.SET_PED_HEAD_OVERLAY_TINT, ped, 1, 1, Wrap(look.BeardColor, colors), Wrap(look.BeardColor, colors));
            ApplyOutfit(ped, slot);
            LastSeen[slot] = Game.GameTime;
        }

        private static void SetComponent(Ped ped, int component, int drawable, int texture)
        {
            int count = Function.Call<int>(Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS, ped, component);
            if (count <= 0) return;
            if (drawable < 0 || drawable >= count)
            {
                Logger.Warn("Unsupported appearance drawable " + component + ":" + drawable + "; using base variant.");
                drawable = 0;
            }
            int textures = Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS, ped, component, drawable);
            texture = textures > 0 ? Math.Abs(texture) % textures : 0;
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, ped, component, drawable, texture, 0);
        }

        private static void ApplyOutfit(Ped ped, CrewSlot slot)
        {
            var look = For(slot);
            // Base-game T-shirt/jeans/sneakers, matched arms and empty undershirt.
            // Palette variants preserve the silhouette and do not randomize heads.
            SetComponent(ped, 3, 0, 0);
            SetComponent(ped, 8, 15, 0);
            SetComponent(ped, 11, 0, (int)slot * 3 + look.Outfit);
            SetComponent(ped, 4, 0, look.Outfit);
            SetComponent(ped, 6, 1, look.Outfit);
            for (int i = 0; i < Clothing.Length; i++)
                if (Drawable(slot, Clothing[i]) >= 0)
                    SetComponent(ped, Components[i], Drawable(slot, Clothing[i]), Texture(slot, Clothing[i]));
            for (int i = 0; i < Props.Length; i++)
            {
                int drawable = Drawable(slot, Props[i]);
                int count = Function.Call<int>(Hash.GET_NUMBER_OF_PED_PROP_DRAWABLE_VARIATIONS, ped, PropSlots[i]);
                if (drawable < 0 || drawable >= count) { Function.Call(Hash.CLEAR_PED_PROP, ped, PropSlots[i]); continue; }
                int textures = Function.Call<int>(Hash.GET_NUMBER_OF_PED_PROP_TEXTURE_VARIATIONS, ped, PropSlots[i], drawable);
                Function.Call(Hash.SET_PED_PROP_INDEX, ped, PropSlots[i], drawable, Wrap(Texture(slot, Props[i]), textures), true);
            }
        }

        public static void Leave(CrewSlot slot) { LastSeen[slot] = Game.GameTime; }
        public static bool ChangeAfterAbsence(Ped ped, CrewSlot slot, bool freeRoam)
        {
            if (!For(slot).AutoOutfits || !freeRoam || ped == null || !ped.Exists() || ped.IsDead || ped.IsInCombat || ped.IsInVehicle() ||
                Game.Player.WantedLevel > 0 || (LastSeen.TryGetValue(slot, out var seen) && Game.GameTime - seen < 120000))
                return false;
            For(slot).Outfit = (For(slot).Outfit + 1) % 3;
            ApplyOutfit(ped, slot);
            LastSeen[slot] = Game.GameTime;
            return true;
        }

        public static void Adjust(Ped ped, CrewSlot slot, string field, int direction)
        {
            if (ped == null || !ped.Exists()) return;
            var look = For(slot);
            // Head hair is part of each hero's identity, including debug/home paths.
            if (field == "Hair" || field == "HairColor") return;
            if (field == "Face") look.Face = (look.Face + direction + 21) % 21;
            else if (field == "Skin") look.Skin = (look.Skin + direction + 21) % 21;
            else if (field == "Outfit")
            { look.Outfit = Wrap(look.Outfit + direction, 3); look.Drawables.Clear(); look.Textures.Clear(); }
            else if (field == "Beard")
            { int count = Function.Call<int>(Hash.GET_PED_HEAD_OVERLAY_NUM, 1); look.Beard = Wrap(look.Beard + 1 + direction, count + 1) - 1; }
            else if (field == "BeardColor") look.BeardColor = Wrap(look.BeardColor + direction, Function.Call<int>(Hash.GET_NUM_PED_HAIR_TINTS));
            Apply(ped, slot);
        }

        public static void AdjustClothing(Ped ped, CrewSlot slot, string field, int direction, bool texture)
        {
            if (ped == null || !ped.Exists()) { GameUtils.Notify("~y~Deploy this character first."); return; }
            var look = For(slot);
            int index = Array.IndexOf(Clothing, field);
            bool prop = index < 0;
            if (prop) index = Array.IndexOf(Props, field);
            if (index < 0) return;
            int part = prop ? PropSlots[index] : Components[index];
            int drawable = Drawable(slot, field);
            // The preset is visible before the first edit; begin cycling from that item.
            if (!prop && drawable < 0) drawable = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, ped, part);
            if (texture)
            {
                if (drawable < 0) return;
                int count = Function.Call<int>(prop ? Hash.GET_NUMBER_OF_PED_PROP_TEXTURE_VARIATIONS : Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS, ped, part, drawable);
                if (count <= 0) return;
                look.Drawables[field] = drawable;
                look.Textures[field] = Wrap(Texture(slot, field) + direction, count);
            }
            else
            {
                int count = Function.Call<int>(prop ? Hash.GET_NUMBER_OF_PED_PROP_DRAWABLE_VARIATIONS : Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS, ped, part);
                if (count <= 0) return;
                look.Drawables[field] = prop ? Wrap(drawable + 1 + direction, count + 1) - 1 : Wrap(drawable + direction, count);
                look.Textures[field] = 0;
            }
            look.AutoOutfits = false;
            Apply(ped, slot);
        }

        public static void Save()
        {
            if (string.IsNullOrEmpty(_savePath)) return;
            var lines = new StringBuilder("; Personal Bloodlines looks. Survive mod updates.\r\n");
            foreach (var hero in Protagonist.All)
            {
                var l = For(hero.Slot);
                lines.AppendFormat(CultureInfo.InvariantCulture, "[{0}]\r\nFace={1}\r\nSkin={2}\r\nHair={3}\r\nOutfit={4}\r\n\r\n",
                    hero.Handle, l.Face, l.Skin, l.Hair, l.Outfit);
                lines.AppendFormat(CultureInfo.InvariantCulture, "HairColor={0}\r\nBeard={1}\r\nBeardColor={2}\r\nAutoOutfits={3}\r\n", l.HairColor, l.Beard, l.BeardColor, l.AutoOutfits);
                foreach (var fields in new[] { Clothing, Props }) foreach (var field in fields)
                    lines.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}\r\n{0}Texture={2}\r\n", field, Drawable(hero.Slot, field), Texture(hero.Slot, field));
                lines.AppendLine();
            }
            string temporary = _savePath + ".tmp";
            File.WriteAllText(temporary, lines.ToString(), new UTF8Encoding(false));
            if (File.Exists(_savePath)) File.Replace(temporary, _savePath, _savePath + ".bak");
            else File.Move(temporary, _savePath);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Bloodlines.Core
{
    /// <summary>
    /// An owned vehicle in the save: its model, its build, the garage it is kept in
    /// and whose it is by tag. The build fields mirror <see cref="CrewVanRecord"/>;
    /// plate and tint are read and written through natives so a car looks the same
    /// coming out as it did going in. Garages and cars are the crew's (Ron,
    /// September 12); the owner tag only sorts the list and colors the icon.
    /// </summary>
    public sealed class OwnedVehicle
    {
        public int Id;
        /// <summary>The catalog's model name when bought; empty for a car taken off the street, which is created from its hash.</summary>
        public string ModelName = "";
        public uint ModelHash;
        /// <summary>What the menus call it.</summary>
        public string Label = "";
        /// <summary>The garage site id it is kept in.</summary>
        public string Garage = "";
        /// <summary>"" for the crew, else a CrewSlot name.</summary>
        public string Owner = "";
        /// <summary>What it is worth: paid at the dealer, or the street value.</summary>
        public int Price;
        public bool Stolen, InShop;
        public int PrimaryColor = -1, SecondaryColor = -1, Livery = -1, WheelType = -1, WindowTint = -1;
        public bool TiresReinforced;
        /// <summary>
        /// Tuning past the last part the game sells, one level each. Explicit fields rather
        /// than entries in <see cref="Finish"/>, because VehicleFinish.Capture clears that
        /// dictionary every time the build is read off the car and would take these with it.
        /// </summary>
        public int StageDrive, StagePower;
        public string Plate = "";
        public readonly Dictionary<string, int> Finish = new Dictionary<string, int>();
        public readonly Dictionary<int, int> Mods = new Dictionary<int, int>();

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "id", Id }, { "model", ModelName }, { "hash", (long)ModelHash }, { "label", Label }, { "garage", Garage },
            { "owner", Owner }, { "price", Price }, { "stolen", Stolen }, { "inShop", InShop },
            { "primary", PrimaryColor }, { "secondary", SecondaryColor }, { "livery", Livery },
            { "wheelType", WheelType }, { "tint", WindowTint }, { "reinforcedTires", TiresReinforced }, { "plate", Plate },
            { "stageDrive", StageDrive }, { "stagePower", StagePower },
            { "finish", Finish.ToDictionary(p => p.Key, p => (object)p.Value) },
            { "mods", Mods.ToDictionary(p => p.Key.ToString(), p => (object)p.Value) }
        };

        public static OwnedVehicle FromJson(Dictionary<string, object> map)
        {
            if (map == null || map.Count == 0) return null;
            var car = new OwnedVehicle
            {
                Id = Json.Int(map, "id"), ModelName = Json.String(map, "model"), Label = Json.String(map, "label"),
                Garage = Json.String(map, "garage"), Owner = Json.String(map, "owner"), Price = Json.Int(map, "price"),
                Stolen = Json.Bool(map, "stolen"), InShop = Json.Bool(map, "inShop"),
                PrimaryColor = Json.Int(map, "primary", -1), SecondaryColor = Json.Int(map, "secondary", -1), Livery = Json.Int(map, "livery", -1),
                WheelType = Json.Int(map, "wheelType", -1), WindowTint = Json.Int(map, "tint", -1),
                TiresReinforced = Json.Bool(map, "reinforcedTires"), Plate = Json.String(map, "plate"),
                // Absent in a save written before stages existed, which reads as none fitted.
                StageDrive = Json.Int(map, "stageDrive"), StagePower = Json.Int(map, "stagePower")
            };
            long hash = 0;
            if (map.TryGetValue("hash", out var raw) && raw != null) long.TryParse(raw.ToString(), out hash);
            car.ModelHash = unchecked((uint)hash);
            foreach (var pair in Json.Object(map.TryGetValue("mods", out var mods) ? mods : null))
                if (int.TryParse(pair.Key, out int type) && pair.Value != null && int.TryParse(pair.Value.ToString(), out int index)) car.Mods[type] = index;
            foreach (var pair in Json.Object(map.TryGetValue("finish", out var finish) ? finish : null))
                if (pair.Value != null && int.TryParse(pair.Value.ToString(), out int value)) car.Finish[pair.Key] = value;
            return car;
        }

        public string Fingerprint() => Json.Write(ToJson());
    }
}

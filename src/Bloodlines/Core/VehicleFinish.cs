using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Paint, extras and toggle upgrades that indexed mod slots do not describe.</summary>
    public static class VehicleFinish
    {
        private static void Color(Vehicle v, Dictionary<string, int> values, string key, Hash native, int count = 3)
        {
            var a = new OutputArgument(); var b = new OutputArgument(); var c = new OutputArgument();
            if (count == 2) Function.Call(native, v, a, b); else Function.Call(native, v, a, b, c);
            values[key + "0"] = a.GetResult<int>(); values[key + "1"] = b.GetResult<int>();
            if (count == 3) values[key + "2"] = c.GetResult<int>();
        }
        public static void Capture(Vehicle v, Dictionary<string, int> values)
        {
            values.Clear();
            values["trim"] = (int)v.Mods.TrimColor; values["dashboard"] = (int)v.Mods.DashboardColor;
            Color(v, values, "extraColor", Hash.GET_VEHICLE_EXTRA_COLOURS, 2);
            Color(v, values, "neonColor", Hash.GET_VEHICLE_NEON_COLOUR);
            Color(v, values, "smokeColor", Hash.GET_VEHICLE_TYRE_SMOKE_COLOR);
            values["customPrimary"] = Function.Call<bool>(Hash.GET_IS_VEHICLE_PRIMARY_COLOUR_CUSTOM, v) ? 1 : 0;
            values["customSecondary"] = Function.Call<bool>(Hash.GET_IS_VEHICLE_SECONDARY_COLOUR_CUSTOM, v) ? 1 : 0;
            if (values["customPrimary"] != 0) Color(v, values, "primary", Hash.GET_VEHICLE_CUSTOM_PRIMARY_COLOUR);
            if (values["customSecondary"] != 0) Color(v, values, "secondary", Hash.GET_VEHICLE_CUSTOM_SECONDARY_COLOUR);
            values["plateStyle"] = Function.Call<int>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT_INDEX, v);
            values["xenonColor"] = Function.Call<int>(Hash.GET_VEHICLE_XENON_LIGHT_COLOR_INDEX, v);
            foreach (int slot in new[] { 17, 18, 20, 22 }) values["toggle" + slot] = Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON, v, slot) ? 1 : 0;
            foreach (int slot in new[] { 23, 24 }) values["variation" + slot] = Function.Call<bool>(Hash.GET_VEHICLE_MOD_VARIATION, v, slot) ? 1 : 0;
            for (int i = 0; i < 4; i++) values["neon" + i] = Function.Call<bool>(Hash.GET_VEHICLE_NEON_ENABLED, v, i) ? 1 : 0;
            for (int i = 0; i <= 20; i++) if (Function.Call<bool>(Hash.DOES_EXTRA_EXIST, v, i)) values["extra" + i] = Function.Call<bool>(Hash.IS_VEHICLE_EXTRA_TURNED_ON, v, i) ? 1 : 0;
        }
        public static void Apply(Vehicle v, Dictionary<string, int> values, Dictionary<int, int> mods)
        {
            if (values.TryGetValue("trim", out int trim)) v.Mods.TrimColor = (VehicleColor)trim;
            if (values.TryGetValue("dashboard", out int dash)) v.Mods.DashboardColor = (VehicleColor)dash;
            if (values.Count == 0) return; // Older saves retain the model's finish.
            if (values.TryGetValue("extraColor0", out int pearl) && values.TryGetValue("extraColor1", out int wheel)) Function.Call(Hash.SET_VEHICLE_EXTRA_COLOURS, v, pearl, wheel);
            ApplyColor(v, values, "neonColor", Hash.SET_VEHICLE_NEON_COLOUR);
            ApplyColor(v, values, "smokeColor", Hash.SET_VEHICLE_TYRE_SMOKE_COLOR);
            if (values.TryGetValue("customPrimary", out int primary)) { if(primary != 0) ApplyColor(v, values, "primary", Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR); else v.Mods.ClearCustomPrimaryColor(); }
            if (values.TryGetValue("customSecondary", out int secondary)) { if(secondary != 0) ApplyColor(v, values, "secondary", Hash.SET_VEHICLE_CUSTOM_SECONDARY_COLOUR); else v.Mods.ClearCustomSecondaryColor(); }
            if (values.TryGetValue("plateStyle", out int plate)) Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT_INDEX, v, plate);
            if (values.TryGetValue("xenonColor", out int xenon)) Function.Call(Hash.SET_VEHICLE_XENON_LIGHT_COLOR_INDEX, v, xenon);
            foreach (int slot in new[] { 17, 18, 20, 22 }) if (values.TryGetValue("toggle" + slot, out int toggle)) Function.Call(Hash.TOGGLE_VEHICLE_MOD, v, slot, toggle != 0);
            foreach (int slot in new[] { 23, 24 }) if (values.TryGetValue("variation" + slot, out int variation) && mods.TryGetValue(slot, out int mod)) Function.Call(Hash.SET_VEHICLE_MOD, v, slot, mod, variation != 0);
            for (int i = 0; i < 4; i++) if (values.TryGetValue("neon" + i, out int enabled)) Function.Call(Hash.SET_VEHICLE_NEON_ENABLED, v, i, enabled != 0);
            for (int i = 0; i <= 20; i++) if (values.TryGetValue("extra" + i, out int enabled) && Function.Call<bool>(Hash.DOES_EXTRA_EXIST, v, i)) Function.Call(Hash.SET_VEHICLE_EXTRA, v, i, enabled == 0);
        }
        private static void ApplyColor(Vehicle v, Dictionary<string, int> values, string key, Hash native)
        {
            if (values.TryGetValue(key + "0", out int r) && values.TryGetValue(key + "1", out int g) && values.TryGetValue(key + "2", out int b)) Function.Call(native, v, r, g, b);
        }
    }
}

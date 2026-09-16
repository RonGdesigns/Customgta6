using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Installed DLC road vehicles, spawned through SHVDN's supported factory.</summary>
    public sealed class StoryVehicles
    {
        public sealed class Choice
        {
            public string Name, Model, Category;
            public Choice(string name, string model, string category = "Cars") { Name = name; Model = model; Category = category; }
        }
        public static readonly Choice[] Catalog = {
            // --- CARS ---
            new Choice("Armored Kuruma", "kuruma2", "Cars"),
            new Choice("Ignus", "ignus", "Cars"),
            new Choice("Itali GTO", "italigto", "Cars"),
            new Choice("Itali RSX", "italirsx", "Cars"),
            new Choice("Krieger", "krieger", "Cars"),
            new Choice("Emerus", "emerus", "Cars"),
            new Choice("Thrax", "thrax", "Cars"),
            new Choice("Jester RR", "jester4", "Cars"),
            new Choice("Calico GTF", "calico", "Cars"),
            new Choice("Dominator ASP", "dominator7", "Cars"),
            new Choice("Dominator GTT", "dominator8", "Cars"),
            new Choice("Dominator GT", "dominator10", "Cars"),
            new Choice("Comet S2", "comet6", "Cars"),
            new Choice("Euros", "euros", "Cars"),
            new Choice("Euros X32", "eurosx32", "Cars"),
            new Choice("Cypher", "cypher", "Cars"),
            new Choice("ZR350", "zr350", "Cars"),
            new Choice("Remus", "remus", "Cars"),
            new Choice("Growler", "growler", "Cars"),
            new Choice("Baller ST", "baller7", "Cars"),
            new Choice("Astron", "astron", "Cars"),
            new Choice("Cinquemila", "cinquemila", "Cars"),
            new Choice("Sultan RS", "sultanrs", "Cars"),
            new Choice("Elegy Retro Custom", "elegy", "Cars"),
            new Choice("Banshee 900R", "banshee2", "Cars"),
            new Choice("Nero", "nero", "Cars"),
            new Choice("Tempesta", "tempesta", "Cars"),
            new Choice("XA-21", "xa21", "Cars"),
            new Choice("Vagner", "vagner", "Cars"),
            new Choice("T20", "t20", "Cars"),
            new Choice("Osiris", "osiris", "Cars"),
            new Choice("Zentorno", "zentorno", "Cars"),
            new Choice("Gauntlet Hellfire", "gauntlet4", "Cars"),
            new Choice("Dominator GTX", "dominator3", "Cars"),
            new Choice("Dukes", "dukes", "Cars"),
            new Choice("Tampa", "tampa", "Cars"),
            new Choice("Virgo", "virgo", "Cars"),
            new Choice("Futo GTX", "futo2", "Cars"),
            new Choice("Deveste Eight", "deveste", "Cars"),
            new Choice("Tigon", "tigon", "Cars"),
            new Choice("S80RR", "s80", "Cars"),
            new Choice("Torero XO", "torero2", "Cars"),
            new Choice("Entity MT", "entity3", "Cars"),
            new Choice("10F", "tenf", "Cars"),
            new Choice("Corsita", "corsita", "Cars"),
            new Choice("SM722", "sm722", "Cars"),
            new Choice("Zeno", "zeno", "Cars"),
            new Choice("Coquette D10", "coquette4", "Cars"),
            new Choice("Vigero ZX", "vigero2", "Cars"),
            new Choice("Pipistrello", "pipistrello", "Cars"),
            new Choice("Stinger TT", "stingertt", "Cars"),
            new Choice("Niobe", "niobe", "Cars"),
            new Choice("Envisage", "envisage", "Cars"),
            new Choice("Karin Sultan RS Classic", "sultan3", "Cars"),
            new Choice("Dinka RT3000", "rt3000", "Cars"),
            new Choice("Emperor Vectre", "vectre", "Cars"),
            new Choice("Karin Previon", "previon", "Cars"),
            new Choice("Vulcar Warrener HKR", "warrener2", "Cars"),
            new Choice("Fathom FR36", "driftfr36", "Cars"),
            new Choice("Benefactor Vorschlaghammer", "vorschlaghammer", "Cars"),
            new Choice("Declasse Drift Tampa", "drifttampa", "Cars"),
            new Choice("Dinka Blista Kanjo", "kanjo", "Cars"),
            new Choice("Dinka Kanjo SJ", "kanjosj", "Cars"),
            new Choice("Dinka Postlude", "postlude", "Cars"),
            new Choice("Maibatsu Penumbra FF", "penumbra2", "Cars"),
            new Choice("Annis Elegy RH8", "elegy2", "Cars"),
            new Choice("Übermacht Sentinel Classic", "sentinel3", "Cars"),
            new Choice("Übermacht Sentinel Classic Widebody", "sentinel4", "Cars"),
            new Choice("Benefactor Schwartzer", "schwarzer", "Cars"),
            new Choice("Bravado Banshee", "banshee", "Cars"),
            new Choice("Obey Omnis", "omnis", "Cars"),
            new Choice("Lampadati Tropos Rallye", "tropos", "Cars"),
            new Choice("Vapid Flash GT", "flashgt", "Cars"),
            new Choice("Vapid GB200", "gb200", "Cars"),
            new Choice("Imponte Beater Dukes", "dukes3", "Cars"),
            new Choice("Imponte Ruiner ZZ-8", "ruiner3", "Cars"),
            new Choice("Imponte Phoenix", "phoenix", "Cars"),
            new Choice("Declasse Sabre Turbo Custom", "sabregt2", "Cars"),
            new Choice("Declasse Tulip", "tulip", "Cars"),
            new Choice("Declasse Vamos", "vamos", "Cars"),
            new Choice("Vapid Clique", "clique", "Cars"),
            new Choice("Declasse Impaler", "impaler", "Cars"),
            new Choice("Declasse Impaler LX", "impaler5", "Cars"),
            new Choice("Declasse Impaler SZ", "impaler6", "Cars"),
            new Choice("Bravado Gauntlet Classic", "gauntlet3", "Cars"),
            new Choice("Bravado Gauntlet Classic Custom", "gauntlet5", "Cars"),
            new Choice("Ocelot Pariah", "pariah", "Cars"),
            new Choice("Progen Itali GTB Custom", "italigtb2", "Cars"),
            new Choice("Progen GP1", "gp1", "Cars"),
            new Choice("Overflod Autarch", "autarch", "Cars"),
            new Choice("Overflod Tyrant", "tyrant", "Cars"),
            new Choice("Grotti Furia", "furia", "Cars"),
            new Choice("Bravado Gauntlet Interceptor", "polgauntlet", "Cars"),
            new Choice("Declasse Impaler LX Cruiser", "polimpaler5", "Cars"),
            new Choice("Bravado Dorado Cruiser", "poldorado", "Cars"),
            new Choice("Unmarked Police Cruiser", "police4", "Cars"),
            // --- ELECTRIC ---
            new Choice("Bravado Buffalo EVX", "buffalo5", "Electric"),
            new Choice("Coil Cyclone", "cyclone", "Electric"),
            new Choice("Coil Cyclone II", "cyclone2", "Electric"),
            new Choice("Pegassi Tezeract", "tezeract", "Electric"),
            new Choice("Coil Raiden", "raiden", "Electric"),
            new Choice("Pfister Neon", "neon", "Electric"),
            new Choice("Obey I-Wagen", "iwagen", "Electric"),
            new Choice("Penaud La Coureuse", "coureur", "Electric"),
            new Choice("Överflöd Imorgon", "imorgon", "Electric"),
            new Choice("Coil Voltic", "voltic", "Electric"),
            new Choice("Western Powersurge", "powersurge", "Electric"),
            new Choice("Cheval Surge", "surge", "Electric"),
            new Choice("Hijak Khamelion", "khamelion", "Electric"),
            // --- WEAPONIZED ---
            new Choice("Buffalo STX", "buffalo4", "Weaponized"),
            new Choice("Champion", "champion", "Weaponized"),
            new Choice("Jubilee", "jubilee", "Weaponized"),
            new Choice("Granger 3600LX", "granger2", "Weaponized"),
            new Choice("Mammoth Patriot Mil-Spec", "patriot3", "Weaponized"),
            new Choice("Omnis e-GT", "omnisegt", "Weaponized"),
            new Choice("Virtue", "virtue", "Weaponized"),
            new Choice("Terminus", "terminus", "Weaponized"),
            new Choice("Deluxo", "deluxo", "Weaponized"),
            new Choice("Stromberg", "stromberg", "Weaponized"),
            new Choice("Pegassi Toreador", "toreador", "Weaponized"),
            new Choice("Declasse Scramjet", "scramjet", "Weaponized"),
            new Choice("Grotti Vigilante", "vigilante", "Weaponized"),
            new Choice("Imponte Ruiner 2000", "ruiner2", "Weaponized"),
            new Choice("Insurgent Pick-Up", "insurgent", "Weaponized"),
            new Choice("Insurgent", "insurgent2", "Weaponized"),
            new Choice("Nightshark", "nightshark", "Weaponized"),
            new Choice("HVY Menacer", "menacer", "Weaponized"),
            new Choice("HVY Barrage", "barrage", "Weaponized"),
            new Choice("Bravado Half-track", "halftrack", "Weaponized"),
            new Choice("Vapid Caracara Gun Truck", "caracara", "Weaponized"),
            new Choice("Karin Technical", "technical", "Weaponized"),
            new Choice("Dune FAV", "dune3", "Weaponized"),
            new Choice("TM-02 Khanjali", "khanjali", "Weaponized"),
            new Choice("HVY APC", "apc", "Weaponized"),
            new Choice("HVY Scarab", "scarab", "Weaponized"),
            new Choice("Rhino Tank", "rhino", "Weaponized"),
            new Choice("Oppressor", "oppressor", "Weaponized"),
            new Choice("Oppressor Mk II", "oppressor2", "Weaponized"),
            // --- MOTORCYCLES ---
            new Choice("Hakuchou Drag", "hakuchou2", "Motorcycles"),
            new Choice("Shotaro", "shotaro", "Motorcycles"),
            new Choice("Shinobi", "shinobi", "Motorcycles"),
            new Choice("Reever", "reever", "Motorcycles"),
            new Choice("Manchez Scout", "manchez2", "Motorcycles"),
            new Choice("BF400", "bf400", "Motorcycles"),
            new Choice("Nightblade", "nightblade", "Motorcycles"),
            new Choice("Diabolus Custom", "diablous2", "Motorcycles"),
            new Choice("Principe Lectro", "lectro", "Motorcycles"),
            // --- OFF-ROAD ---
            new Choice("Kamacho", "kamacho", "Off-road"),
            new Choice("Caracara 4x4", "caracara2", "Off-road"),
            new Choice("Everon", "everon", "Off-road"),
            new Choice("Draugur", "draugur", "Off-road"),
            new Choice("Outlaw", "outlaw", "Off-road"),
            new Choice("Vagrant", "vagrant", "Off-road"),
            new Choice("Monstrociti", "monstrociti", "Off-road"),
            new Choice("Liberator Monster Truck", "monster", "Off-road"),
            new Choice("Cheval Marshall Monster Truck", "monster3", "Off-road"),
            new Choice("RUNE Zhaba Amphibious", "zhaba", "Off-road"),
            // --- BOATS ---
            new Choice("Longfin", "longfin", "Boats"),
            new Choice("Toro", "toro", "Boats"),
            new Choice("Lampadati Toro", "toro2", "Boats"),
            new Choice("Speeder", "speeder", "Boats"),
            new Choice("Dinghy", "dinghy", "Boats"),
            new Choice("Seashark", "seashark", "Boats"),
            new Choice("Weaponized Dinghy", "dinghy5", "Boats"),
            new Choice("Kurtz 31 Patrol Boat", "patrolboat", "Boats"),
            // --- HELICOPTERS ---
            new Choice("SuperVolito", "supervolito", "Helicopters"),
            new Choice("SuperVolito Carbon", "supervolito2", "Helicopters"),
            new Choice("Volatus", "volatus", "Helicopters"),
            new Choice("Swift", "swift", "Helicopters"),
            new Choice("Swift Deluxe", "swift2", "Helicopters"),
            new Choice("Frogger", "frogger", "Helicopters"),
            new Choice("Buzzard Attack Chopper", "buzzard", "Helicopters"),
            new Choice("Hunter", "hunter", "Helicopters"),
            new Choice("Akula Stealth Chopper", "akula", "Helicopters"),
            new Choice("Valkyrie Gunship", "valkyrie", "Helicopters"),
            new Choice("Savage", "savage", "Helicopters"),
            new Choice("Annihilator", "annihilator", "Helicopters"),
            new Choice("Cargobob", "cargobob", "Helicopters"),
            new Choice("Sparrow", "seasparrow2", "Helicopters"),
            // --- PLANES ---
            new Choice("Vestra", "vestra", "Planes"),
            new Choice("Nimbus", "nimbus", "Planes"),
            new Choice("Howard NX-25", "howard", "Planes"),
            new Choice("Alpha-Z1", "alphaz1", "Planes"),
            new Choice("Mallard Stunt Plane", "stunt", "Planes"),
            new Choice("Duster", "duster", "Planes"),
            new Choice("Titan Cargo Plane", "titan", "Planes"),
            // --- JETS ---
            new Choice("Lazer", "lazer", "Jets"),
            new Choice("Hydra", "hydra", "Jets"),
            new Choice("Besra", "besra", "Jets"),
            new Choice("Pyro", "pyro", "Jets"),
            new Choice("Molotok", "molotok", "Jets"),
            new Choice("Nokota", "nokota", "Jets"),
            new Choice("Starling", "starling", "Jets"),
            new Choice("B-11 Strikeforce", "strikeforce", "Jets"),
            new Choice("Rogue", "rogue", "Jets"),
            new Choice("F-160 Raiju Stealth Jet", "raiju", "Jets"),
            // --- SPECIAL ---
            new Choice("Thruster jetpack", "thruster", "Special"),
            new Choice("BF Ramp Buggy", "dune4", "Special"),
            new Choice("Phantom Wedge", "phantom2", "Special"),
            new Choice("Vapid Slamtruck Ramp", "slamtruck", "Special"),
            new Choice("Progen PR4 (F1 Open Wheel)", "openwheel1", "Special"),
            new Choice("Benefactor BR8 (F1 Open Wheel)", "openwheel2", "Special")
        };
        private readonly List<Vehicle> _spawned = new List<Vehicle>();
        public static bool Available(Choice choice)
        { var model = new Model(choice.Model); return model.IsInCdImage && model.IsValid && (model.IsCar || model.IsBike || model.IsBoat || model.IsHelicopter || model.IsPlane); }
        public bool Spawn(Choice choice)
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead || !Available(choice))
            { GameUtils.Notify("~y~That vehicle is unavailable in the loaded game."); return false; }
            _spawned.RemoveAll(v => !v.Exists());
            if (_spawned.Count >= 4)
            { GameUtils.Notify("~y~Release your parked vehicles before requesting another."); return false; }
            if (!TrySpawnPoint(choice, player, out var point, out var heading)) return false;
            var model = new Model(choice.Model);
            Vehicle car = null;
            try
            {
                if (!GameUtils.RequestModel(model, 1500)) { GameUtils.Notify("~y~Vehicle model did not finish loading."); return false; }
                car = World.CreateVehicle(model, point + new Vector3(0, 0, .2f), heading);
                if (car == null || !car.Exists()) { GameUtils.Notify("~y~Vehicle creation failed."); return false; }
                car.IsPersistent = true; car.LockStatus = VehicleLockStatus.Unlocked;
                if (!model.IsBoat) car.PlaceOnGround();
                car.IsEngineRunning = !model.IsPlane && !model.IsHelicopter;
                _spawned.Add(car);
                Function.Call(Hash.SET_NEW_WAYPOINT, car.Position.X, car.Position.Y);
                GameUtils.Notify("~g~" + choice.Name + " is ready at the marked location. Enter normally.");
                return true;
            }
            catch { if (car != null && car.Exists()) GameUtils.SafeRelease(car); throw; }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        private static bool Occupied(Vector3 point, float radius)
        { return Function.Call<bool>(Hash.IS_POSITION_OCCUPIED, point.X, point.Y, point.Z + 1f, radius, false, true, true, false, false, 0, false); }
        private static bool TrySpawnPoint(Choice choice, Ped player, out Vector3 point, out float heading)
        {
            point = Vector3.Zero; heading = player.Heading;
            var model = new Model(choice.Model);
            if (model.IsBoat)
            {
                for (int radius = 15; radius <= 75; radius += 15)
                    for (int angle = 0; angle < 8; angle++)
                    {
                        var candidate = player.Position + new Vector3((float)Math.Cos(angle * Math.PI / 4) * radius, (float)Math.Sin(angle * Math.PI / 4) * radius, 0);
                        var height = new OutputArgument();
                        if (!Function.Call<bool>(Hash.GET_WATER_HEIGHT, candidate.X, candidate.Y, candidate.Z + 20f, height)) continue;
                        candidate.Z = height.GetResult<float>();
                        if (Math.Abs(candidate.Z - player.Position.Z) > 8f || Occupied(candidate, 7f)) continue;
                        // Check a water footprint, not just the center of a narrow canal.
                        bool wide = true;
                        for (int i = 0; i < 4; i++)
                        {
                            var edge = candidate + new Vector3((float)Math.Cos(i * Math.PI / 2) * 6f, (float)Math.Sin(i * Math.PI / 2) * 6f, 0);
                            var edgeHeight = new OutputArgument();
                            if (!Function.Call<bool>(Hash.GET_WATER_HEIGHT, edge.X, edge.Y, edge.Z + 10f, edgeHeight)) { wide = false; break; }
                        }
                        if (wide) { point = candidate; return true; }
                    }
                GameUtils.Notify("~y~Stand beside open water to request a boat."); return false;
            }
            if (model.IsPlane)
            {
                // Fixed runway access points avoid requesting a plane on a city street.
                var strips = new[] { new Vector3(1730f, 3254f, 41f), new Vector3(2128f, 4806f, 41f) };
                for (int i = 0; i < strips.Length; i++)
                    if (player.Position.DistanceTo(strips[i]) < 180f && !Occupied(strips[i], 18f))
                    { point = strips[i]; heading = i == 0 ? 105f : 115f; return true; }
                // Away from a runway the plane is set on the road ahead when it is clear (Ron, September 12: force the spawn when the space is there).
                var ahead = World.GetNextPositionOnStreet(player.Position + player.ForwardVector * 25f);
                if (ahead != Vector3.Zero && ahead.DistanceTo(player.Position) < 60f && Math.Abs(ahead.Z - player.Position.Z) < 8f && !Occupied(ahead, 12f))
                { point = ahead; heading = player.Heading; GameUtils.Notify("~y~No runway near: the plane is on the road ahead. Find a straight to take off."); return true; }
                GameUtils.Notify("~y~Request a plane beside a runway or on a clear straight road."); return false;
            }
            if (model.IsHelicopter)
            {
                for (int angle = 0; angle < 8; angle++)
                {
                    var candidate = player.Position + new Vector3((float)Math.Cos(angle * Math.PI / 4) * 25f, (float)Math.Sin(angle * Math.PI / 4) * 25f, 0);
                    var safe = World.GetSafeCoordForPed(candidate, false, 0);
                    if (safe == Vector3.Zero || safe.DistanceTo(candidate) > 4f || Occupied(safe, 13f)) continue;
                    bool flat = true;
                    for (int i = 0; i < 4; i++)
                    {
                        var edge = safe + new Vector3((float)Math.Cos(i * Math.PI / 2) * 10f, (float)Math.Sin(i * Math.PI / 2) * 10f, 0);
                        var ground = World.GetSafeCoordForPed(edge, false, 0);
                        if (ground == Vector3.Zero || ground.DistanceTo(edge) > 2f) { flat = false; break; }
                    }
                    if (flat) { point = safe; return true; }
                }
                // No ring spot: the space right ahead of the player, when nothing stands in it (Ron, September 12).
                var spot = player.Position + player.ForwardVector * 10f;
                if (!Occupied(spot, 8f)) { point = spot; return true; }
                GameUtils.Notify("~y~Move to a flat open area to request a helicopter."); return false;
            }
            point = World.GetNextPositionOnStreet(player.Position + player.ForwardVector * 12f);
            if (point == Vector3.Zero || point.DistanceTo(player.Position) > 60f || Math.Abs(point.Z - player.Position.Z) > 8f || Occupied(point, 4f))
            { GameUtils.Notify("~y~Move beside an open road to request a vehicle."); return false; }
            return true;
        }
        public void ReleaseParked()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                var car = _spawned[i]; bool occupied = false;
                if (car.Exists())
                    for (int seat = -1; seat < car.PassengerCapacity; seat++)
                        if (car.GetPedOnSeat((VehicleSeat)seat)?.Exists() == true) occupied = true;
                if (occupied) continue;
                if (car.Exists()) GameUtils.SafeRelease(car);
                _spawned.RemoveAt(i);
            }
        }
        public void Clear()
        { foreach (var car in _spawned) if (car.Exists()) GameUtils.SafeRelease(car); _spawned.Clear(); }
    }
}

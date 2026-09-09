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
            new Choice("Armored Kuruma", "kuruma2"), new Choice("Buffalo STX", "buffalo4"),
            new Choice("Champion", "champion"), new Choice("Jubilee", "jubilee"),
            new Choice("Ignus", "ignus"), new Choice("Itali GTO", "italigto"),
            new Choice("Itali RSX", "italirsx"), new Choice("Krieger", "krieger"),
            new Choice("Emerus", "emerus"), new Choice("Thrax", "thrax"),
            new Choice("Jester RR", "jester4"), new Choice("Calico GTF", "calico"),
            new Choice("Dominator ASP", "dominator7"), new Choice("Dominator GTT", "dominator8"),
            new Choice("Comet S2", "comet6"), new Choice("Euros", "euros"),
            new Choice("Cypher", "cypher"), new Choice("ZR350", "zr350"),
            new Choice("Remus", "remus"), new Choice("Growler", "growler"),
            new Choice("Baller ST", "baller7"), new Choice("Granger 3600LX", "granger2"),
            new Choice("Astron", "astron"), new Choice("Cinquemila", "cinquemila"),
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
            new Choice("Hakuchou Drag", "hakuchou2", "Motorcycles"),
            new Choice("Shotaro", "shotaro", "Motorcycles"),
            new Choice("Shinobi", "shinobi", "Motorcycles"),
            new Choice("Reever", "reever", "Motorcycles"),
            new Choice("Manchez Scout", "manchez2", "Motorcycles"),
            new Choice("BF400", "bf400", "Motorcycles"),
            new Choice("Nightblade", "nightblade", "Motorcycles"),
            new Choice("Diabolus Custom", "diablous2", "Motorcycles"),
            new Choice("Kamacho", "kamacho", "Off-road"),
            new Choice("Caracara 4x4", "caracara2", "Off-road"),
            new Choice("Everon", "everon", "Off-road"),
            new Choice("Draugur", "draugur", "Off-road"),
            new Choice("Outlaw", "outlaw", "Off-road"),
            new Choice("Vagrant", "vagrant", "Off-road"),
            new Choice("Dune FAV", "dune3", "Off-road"),
            new Choice("Insurgent Pick-Up", "insurgent", "Off-road"),
            new Choice("Insurgent", "insurgent2", "Off-road"),
            new Choice("Nightshark", "nightshark", "Off-road"),
            new Choice("Longfin", "longfin", "Boats"),
            new Choice("Toro", "toro", "Boats"),
            new Choice("Lampadati Toro", "toro2", "Boats"),
            new Choice("Speeder", "speeder", "Boats"),
            new Choice("Dinghy", "dinghy", "Boats"),
            new Choice("Seashark", "seashark", "Boats"),
            new Choice("SuperVolito", "supervolito", "Helicopters"),
            new Choice("SuperVolito Carbon", "supervolito2", "Helicopters"),
            new Choice("Volatus", "volatus", "Helicopters"),
            new Choice("Swift", "swift", "Helicopters"),
            new Choice("Swift Deluxe", "swift2", "Helicopters"),
            new Choice("Frogger", "frogger", "Helicopters"),
            new Choice("Vestra", "vestra", "Planes"),
            new Choice("Nimbus", "nimbus", "Planes"),
            new Choice("Howard NX-25", "howard", "Planes"),
            new Choice("Alpha-Z1", "alphaz1", "Planes")
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
                        // Check a water footprint, not just the centre of a narrow canal.
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
                GameUtils.Notify("~y~Request a plane beside the Sandy Shores or McKenzie runway."); return false;
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
                GameUtils.Notify("~y~Move to a large, flat open area to request a helicopter."); return false;
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

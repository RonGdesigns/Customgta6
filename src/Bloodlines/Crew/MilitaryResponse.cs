using System;
using System.Collections.Generic;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
using Bloodlines.Core;

namespace Bloodlines.Crew
{
    /// <summary>A bounded sixth wanted tier above GTA's native five-star limit.</summary>
    public sealed class MilitaryResponse
    {
        private sealed class Unit { public Blip Indicator; public int RetiredAt; public int ReadyAt, NextShot, LastMoved, NextReport, TargetHandle, NextDrive; public Vector3 LastPosition; public bool Tasked, Convoy, Approaching; public Vehicle Vehicle; public Ped Driver; public readonly List<Ped> Crew = new List<Ped>(); }
        private readonly List<Unit> _units = new List<Unit>();
        private readonly List<Unit> _aftermath = new List<Unit>();
        private int _nextIndicator;
        private readonly PersonalWanted _wanted;
        private CrewSlot? _owner;
        public static readonly string[] Helicopters = { "buzzard", "hunter", "akula", "savage", "annihilator2" };
        private int _helicopterIndex;
        private int _atFive = -1, _nextWave, _nextTask;
        public MilitaryResponse(PersonalWanted wanted) { _wanted = wanted; }
        public int Level(CrewSlot slot) => Math.Max(Game.Player.WantedLevel, _wanted.Get(slot));
        public void SetLevel(CrewSlot slot, int level)
        {
            level = Math.Max(0, Math.Min(6, level));
            Logger.Info("Military: requested wanted level " + level + " for " + slot);
            Clear(); _wanted.Set(slot, level); Game.Player.WantedLevel = Math.Min(5, level);
            if (level == 6) _nextWave = Game.GameTime + 4000;
            if (level > 0)
            {
                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);
                Function.Call(Hash.SET_EVERYONE_IGNORE_PLAYER, Game.Player, false);
                Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);
            }
        }
        public void Trigger(CrewSlot slot) { SetLevel(slot, 6); }
        private int _lastUpdate;
        public void Update(CrewSlot slot, bool enabled, RelationshipGroup crewGroup, bool paused = false)
        {
            int elapsed = Math.Max(0, Game.GameTime - _lastUpdate); _lastUpdate = Game.GameTime;
            MaintainAftermath();
            if (Game.GameTime >= _nextIndicator)
            {
                _nextIndicator = Game.GameTime + 500;
                var color = (Game.GameTime / 500) % 2 == 0 ? BlipColor.Red : BlipColor.Blue;
                foreach (var unit in _units)
                    if (unit.Indicator != null && unit.Indicator.Exists()) unit.Indicator.Color = color;
            }
            if (enabled && paused && Game.Player.WantedLevel == 5 && _wanted.Get(slot) == 6) DrawWanted();
            if (enabled && paused) { if (_atFive >= 0) _atFive += elapsed; return; }
            var player = Game.Player.Character;
            if (!enabled || player == null || !player.Exists() || player.IsDead) { Clear(); return; }
            // Shared sixth-tier heat keeps the same pursuing units through a crew switch.
            if (_owner.HasValue && _owner.Value != slot && (_wanted.Get(slot) < 6 || Game.Player.WantedLevel < 5)) Clear();
            _owner = slot;
            int native = Game.Player.WantedLevel;
            if (native < 5)
            {
                if (_wanted.Get(slot) == 6) _wanted.Set(slot, native);
                Clear(); return;
            }
            if (_wanted.Get(slot) < 6)
            {
                if (_atFive < 0) _atFive = Game.GameTime;
                if (Game.GameTime - _atFive < 90000) return;
                Trigger(slot); GameUtils.Notify("~r~SIXTH TIER: military response authorized.");
            }
            DrawWanted();
            int removed = _units.RemoveAll(unit => { if (unit.Vehicle != null && unit.Vehicle.Exists() && !unit.Vehicle.IsDead && unit.Driver != null && unit.Driver.Exists() && unit.Driver.IsAlive) return false; RetireLoss(unit); return true; });
            removed += _units.RemoveAll(unit => RetireStranded(unit, player));
            if (removed > 0) _nextWave = Math.Max(_nextWave, Game.GameTime + 4000);
            if (Game.GameTime >= _nextWave && _units.Count < 3)
            {
                _nextWave = Game.GameTime + 8000;
                bool hasTank = _units.Exists(u => !u.Convoy && !u.Vehicle.Model.IsHelicopter);
                bool hasHeli = _units.Exists(u => u.Vehicle.Model.IsHelicopter);
                bool hasConvoy = _units.Exists(u => u.Convoy);
                if (!Spawn(player, crewGroup, !hasHeli ? 1 : !hasConvoy ? 2 : !hasTank ? 0 : -1))
                    _nextWave = Game.GameTime + 3000;
            }
            foreach (var unit in _units)
                if (!unit.Convoy && !unit.Vehicle.Model.IsHelicopter) FireTank(unit, player);
            if (Game.GameTime < _nextTask) return;
            _nextTask = Game.GameTime + 4000;
            foreach (var unit in _units)
            {
                if (Game.GameTime < unit.ReadyAt) { _nextTask = Math.Min(_nextTask, unit.ReadyAt); continue; }
                if (!unit.Driver.IsInVehicle(unit.Vehicle) ||
                    unit.Vehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle != unit.Driver.Handle) continue;
                if (!unit.Tasked) Logger.Info("Military: assigning " + Kind(unit) + " task; vehicle=" + unit.Vehicle.Handle + " driver=" + unit.Driver.Handle);
                if (unit.Vehicle.Model.IsHelicopter)
                    unit.Driver.Task.StartHeliMission(unit.Vehicle, player, VehicleMissionType.Attack, 42f, 28f,
                        (int)player.Position.Z + 45, 30, -1f, 65f, (HeliMissionFlags)(128 | 256 | 4096));
                else if (unit.Convoy)
                {
                    if (!unit.Tasked || unit.TargetHandle != player.Handle) unit.Driver.Task.VehicleChase(player);
                    Function.Call(Hash.SET_DRIVE_TASK_CRUISE_SPEED, unit.Driver, 42f);
                    foreach (var gunner in unit.Crew)
                        if (gunner.Exists() && gunner.IsAlive && gunner.IsInVehicle(unit.Vehicle) && gunner.SeatIndex != VehicleSeat.Driver)
                        {
                            // Rear passengers own the gunfire; the driver keeps pursuing.
                            Function.Call(Hash.TASK_DRIVE_BY, gunner, player, 0, 0f, 0f, 0f, 100f, 35, false,
                                Game.GenerateHash("FIRING_PATTERN_BURST_FIRE_DRIVEBY"));
                        }
                }
                else
                {
                    // Attack alone can hold a tank on a distant road. Route into range
                    // first, including when buildings hide a nearby target.
                    bool approach = unit.Vehicle.Position.DistanceTo(player.Position) > 125f ||
                        !Function.Call<bool>(Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY, unit.Vehicle, player, 17);
                    if (!unit.Tasked || unit.TargetHandle != player.Handle || approach != unit.Approaching || Game.GameTime >= unit.NextDrive)
                    {
                        unit.Driver.Task.StartVehicleMission(unit.Vehicle, player,
                            approach ? VehicleMissionType.GoTo : VehicleMissionType.Attack,
                            28f, (VehicleDrivingFlags)786603, 20f, 30f, true);
                        unit.Approaching = approach; unit.NextDrive = Game.GameTime + 10000;
                    }
                }
                if (!unit.Tasked) Logger.Info("Military: attack task accepted.");
                unit.Tasked = true; unit.TargetHandle = player.Handle;
            }
        }
        private bool Spawn(Ped target, RelationshipGroup crewGroup, int kind)
        {
            if (kind < 0) return false;
            bool tank = kind == 0, convoy = kind == 2, ground = tank || convoy;
            Vector3 point;
            if (!TryArrivalPoint(target, ground, out point))
            { Logger.Info("Military: " + (tank ? "tank" : convoy ? "convoy" : "helicopter") + " waiting for a clear off-camera approach; retry in 3s."); return false; }
            string heli = "buzzard";
            if (!ground)
                for (int i = 0; i < Helicopters.Length; i++)
                {
                    string candidate = Helicopters[_helicopterIndex++ % Helicopters.Length];
                    var model = new Model(candidate);
                    if (model.IsValid && model.IsInCdImage && model.IsHelicopter) { heli = candidate; break; }
                }
            var vehicleModel = new Model(tank ? "rhino" : convoy ? "barracks" : heli); var soldierModel = new Model("s_m_y_marine_03");
            Logger.Info("Military: loading " + (tank ? "rhino" : convoy ? "barracks" : heli) + " at " + point + "; target=" + target.Position);
            if (!GameUtils.RequestModel(vehicleModel, 750) || !GameUtils.RequestModel(soldierModel, 750))
            { vehicleModel.MarkAsNoLongerNeeded(); soldierModel.MarkAsNoLongerNeeded(); return false; }
            var unit = new Unit { Convoy = convoy };
            try
            {
                Logger.Info("Military: creating vehicle.");
                unit.Vehicle = World.CreateVehicle(vehicleModel, point, target.Heading);
                if (unit.Vehicle == null || !unit.Vehicle.Exists()) return false;
                unit.Vehicle.IsPersistent = true; unit.Vehicle.IsEngineRunning = true;
                if (ground) unit.Vehicle.PlaceOnGround();
                Logger.Info("Military: creating pilot/driver.");
                unit.Driver = World.CreatePed(soldierModel, point, target.Heading);
                if (unit.Driver == null || !unit.Driver.Exists()) { Release(unit); return false; }
                var group = World.AddRelationshipGroup("BLOODLINES_MILITARY");
                AllyWithPolice(group);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, group, crewGroup);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, crewGroup, group);
                Configure(unit.Driver, group); unit.Driver.SetIntoVehicle(unit.Vehicle, VehicleSeat.Driver);
                Function.Call(Hash.SET_DRIVER_ABILITY, unit.Driver, 1f);
                Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, unit.Driver, convoy ? .8f : .5f);
                if (convoy)
                {
                    // Barracks rear bench seats; do not create a gunner if no seat exists.
                    for (int seat = 1; seat <= 2; seat++)
                    {
                        if (seat >= unit.Vehicle.PassengerCapacity || !unit.Vehicle.IsSeatFree((VehicleSeat)seat)) continue;
                        var gunner = World.CreatePed(soldierModel, point, target.Heading);
                        if (gunner == null || !gunner.Exists()) continue;
                        unit.Crew.Add(gunner); Configure(gunner, group);
                        gunner.SetIntoVehicle(unit.Vehicle, (VehicleSeat)seat);
                        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, gunner, 2, true);
                        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, gunner, 3, false);
                    }
                }
                if (!ground)
                {
                    Function.Call(Hash.SET_HELI_BLADES_FULL_SPEED, unit.Vehicle);

                }
                unit.ReadyAt = Game.GameTime + 1000; unit.NextShot = Game.GameTime + 8000;
                Logger.Info("Military: spawned " + (convoy ? "convoy" : tank ? "tank" : heli) + " with " + unit.Crew.Count + " rear gunners.");
                unit.LastPosition = point; unit.LastMoved = Game.GameTime; unit.NextReport = Game.GameTime + 15000;
                unit.Indicator = unit.Vehicle.AddBlip();
                unit.Indicator.Sprite = ground ? (tank ? BlipSprite.Tank : BlipSprite.PoliceCarDot) : BlipSprite.PoliceHelicopter;
                unit.Indicator.Color = BlipColor.Red; unit.Indicator.Scale = .8f;
                unit.Indicator.IsFriendly = false; unit.Indicator.IsShortRange = false;
                unit.Indicator.Name = tank ? "Military tank" : convoy ? "Military convoy" : "Military helicopter";
                _units.Add(unit); _nextTask = Math.Min(_nextTask, unit.ReadyAt);
                Logger.Info("Military: crew seated; deferring attack until the next game tick.");
                return true;
            }
            catch { Release(unit); throw; }
            finally { vehicleModel.MarkAsNoLongerNeeded(); soldierModel.MarkAsNoLongerNeeded(); }
        }
        // Only our own law-enforcement groups are linked. Native cops retain their
        // normal wanted-level decisions and relationships with ordinary civilians.
        internal static void AllyWithPolice(RelationshipGroup group)
        {
            foreach (string name in new[] { "COP", "BLOODLINES_MILITARY", "BLOODLINES_LIFE_POLICE" })
            {
                int ally = Game.GenerateHash(name);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, group, ally);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, ally, group);
            }
        }
        private void RetireLoss(Unit unit)
        {
            GameUtils.SafeDelete(unit.Indicator); unit.Indicator = null;
            unit.RetiredAt = Game.GameTime;
            // Keep the real vehicle and bodies intact so physics, fire, wreckage
            // and helicopter falls can finish. Never synthesize an explosion.
            _aftermath.Add(unit);
            if (_aftermath.Count > 6) { ReleaseAftermath(_aftermath[0]); _aftermath.RemoveAt(0); }
            Logger.Info("Military: unit lost; preserving physical aftermath and scheduling a staggered replacement.");
        }
        private void MaintainAftermath()
        {
            var player = Game.Player.Character;
            _aftermath.RemoveAll(unit =>
            {
                int age = Game.GameTime - unit.RetiredAt;
                bool nearby = unit.Vehicle != null && unit.Vehicle.Exists() &&
                    (unit.Vehicle.IsOnScreen || player != null && player.Exists() && unit.Vehicle.Position.DistanceTo(player.Position) < 150f);
                if (age < 60000 || nearby && age < 120000) return false;
                ReleaseAftermath(unit); return true;
            });
        }
        private static void ReleaseAftermath(Unit unit)
        {
            GameUtils.SafeDelete(unit.Indicator); unit.Indicator = null;
            // Hand off to normal engine cleanup; even an expired wreck is not deleted.
            GameUtils.SafeRelease(unit.Driver);
            foreach (var ped in unit.Crew) GameUtils.SafeRelease(ped);
            GameUtils.SafeRelease(unit.Vehicle);
        }
        private static string Kind(Unit unit) => unit.Convoy ? "convoy" : unit.Vehicle.Model.IsHelicopter ? "helicopter" : "tank";
        private static bool TryArrivalPoint(Ped target, bool ground, out Vector3 point)
        {
            var forward = target.ForwardVector;
            var right = new Vector3(forward.Y, -forward.X, 0f);
            // Try several streets instead of depending on one road 200m behind.
            var offsets = ground ? new[] { forward * -150f, right * 160f, right * -160f, forward * 180f }
                : new[] { forward * -220f + new Vector3(0f, 0f, 75f), right * 220f + new Vector3(0f, 0f, 75f), right * -220f + new Vector3(0f, 0f, 75f) };
            foreach (var offset in offsets)
            {
                point = ground ? World.GetNextPositionOnStreet(target.Position + offset) : target.Position + offset;
                float distance = point.DistanceTo(target.Position);
                if (point == Vector3.Zero || distance < 110f || distance > 350f ||
                    ground && Math.Abs(point.Z - target.Position.Z) > 18f ||
                    Function.Call<bool>(Hash.IS_SPHERE_VISIBLE, point.X, point.Y, point.Z, ground ? 12f : 25f)) continue;
                if (ground && Function.Call<bool>(Hash.IS_POSITION_OCCUPIED, point.X, point.Y, point.Z, 8f, false, true, true, false, false, 0, false)) continue;
                return true;
            }
            point = Vector3.Zero; return false;
        }
        private static bool RetireStranded(Unit unit, Ped target)
        {
            float distance = unit.Vehicle.Position.DistanceTo(target.Position);
            if (Game.GameTime >= unit.NextReport)
            {
                unit.NextReport = Game.GameTime + 15000;
                Logger.Info("Military: " + Kind(unit) + " range=" + (int)distance + "m speed=" + (int)unit.Vehicle.Speed + "m/s; vehicle=" + unit.Vehicle.Position + "; target=" + target.Position);
            }
            if (unit.Vehicle.Model.IsHelicopter) return false;
            if (unit.Vehicle.Position.DistanceTo(unit.LastPosition) > 10f || unit.Vehicle.Speed > 2f)
            { unit.LastPosition = unit.Vehicle.Position; unit.LastMoved = Game.GameTime; }
            if (distance <= 100f || Game.GameTime - unit.LastMoved < 30000 ||
                !unit.Convoy && distance <= 180f && Function.Call<bool>(Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY, unit.Vehicle, target, 17) ||
                unit.Vehicle.IsOnScreen || Function.Call<bool>(Hash.IS_SPHERE_VISIBLE, unit.Vehicle.Position.X, unit.Vehicle.Position.Y, unit.Vehicle.Position.Z, 12f) || target.IsInVehicle(unit.Vehicle)) return false;
            for (int seat = -1; seat < unit.Vehicle.PassengerCapacity; seat++)
            {
                var occupant = unit.Vehicle.GetPedOnSeat((VehicleSeat)seat);
                if (occupant != null && occupant.Exists() && occupant.Handle != unit.Driver.Handle && !unit.Crew.Exists(p => p.Handle == occupant.Handle)) return false;
            }
            Logger.Info("Military: replacing stranded " + Kind(unit) + " off camera; range=" + (int)distance + "m.");
            Release(unit); return true;
        }
        private static void FireTank(Unit unit, Ped target)
        {
            if (!unit.Tasked || Game.GameTime < unit.NextShot || !unit.Driver.IsInVehicle(unit.Vehicle) ||
                unit.Vehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle != unit.Driver.Handle) return;
            float distance = unit.Vehicle.Position.DistanceTo(target.Position);
            if (distance < 40f || distance > 180f || !Function.Call<bool>(Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY, unit.Vehicle, target, 17)) return;
            unit.NextShot = Game.GameTime + 6500;
            // Select the real mounted cannon before requesting one shot. No synthetic explosion.
            if (!Function.Call<bool>(Hash.SET_CURRENT_PED_VEHICLE_WEAPON, unit.Driver, Game.GenerateHash("VEHICLE_WEAPON_TANK"))) return;
            Logger.Info("Military: tank cannon shot requested; vehicle=" + unit.Vehicle.Handle);
            var aim = target.Position;
            Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, unit.Driver, target, aim.X, aim.Y, aim.Z);
        }
        private static void DrawWanted()
        {
            if (Game.IsPaused || Function.Call<bool>(Hash.IS_HUD_HIDDEN)) return;
            Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, "commonmenu", false);
            if (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, "commonmenu")) return;
            Function.Call(Hash.HIDE_HUD_COMPONENT_THIS_FRAME, 1);
            float safe = Math.Max(.8f, Math.Min(1f, Function.Call<float>(Hash.GET_SAFE_ZONE_SIZE)));
            float aspect = Math.Max(1f, Function.Call<float>(Hash.GET_ASPECT_RATIO, false));
            float margin = (1f - safe) * .5f, width = .030f / aspect;
            int alpha = Function.Call<bool>(Hash.ARE_PLAYER_STARS_GREYED_OUT, Game.Player) && (Game.GameTime / 400) % 2 == 0 ? 100 : 255;
            for (int i = 0; i < 6; i++)
            {
                float x = 1f - margin - .019f / aspect - i * .035f / aspect, y = margin + .037f;
                Function.Call(Hash.DRAW_SPRITE, "commonmenu", "shop_new_star", x + .001f / aspect, y + .0015f, width, .030f, 0f, 0, 0, 0, alpha, false, 0);
                Function.Call(Hash.DRAW_SPRITE, "commonmenu", "shop_new_star", x, y, width, .030f, 0f, 255, 255, 255, alpha, false, 0);
            }
        }
        private static void Configure(Ped ped, RelationshipGroup group)
        { ped.IsPersistent = true; ped.BlockPermanentEvents = true; ped.RelationshipGroup = group; ped.Health = 250; ped.Armor = 75; ped.Accuracy = 25; ped.Weapons.Give(WeaponHash.CarbineRifle, 600, true, true);
            Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, ped, false, false);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 1, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 3, false);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 5, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 23, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 52, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 53, true);
        }
        private static void Release(Unit unit)
        {
            GameUtils.SafeDelete(unit.Indicator); unit.Indicator = null;
            if (unit.Vehicle != null && unit.Vehicle.Exists() && unit.Vehicle.IsDead ||
                unit.Driver != null && unit.Driver.Exists() && unit.Driver.IsDead)
            { ReleaseAftermath(unit); return; }
            bool borrowed = false;
            if (unit.Vehicle != null && unit.Vehicle.Exists())
                for (int seat = -1; seat < unit.Vehicle.PassengerCapacity; seat++)
                {
                    var occupant = unit.Vehicle.GetPedOnSeat((VehicleSeat)seat);
                    if (occupant != null && occupant.Exists() && occupant.Handle != unit.Driver?.Handle && !unit.Crew.Exists(p => p.Handle == occupant.Handle)) borrowed = true;
                }
            // These are our spawned soldiers. Remove them rather than accumulating
            // abandoned hostile entities after every death, mission or replacement.
            if (unit.Driver != null && unit.Driver.Exists() && unit.Driver.Handle != Game.Player.Character?.Handle) GameUtils.SafeDelete(unit.Driver);
            foreach (var ped in unit.Crew) if (ped.Exists() && ped.Handle != Game.Player.Character?.Handle) GameUtils.SafeDelete(ped);
            if (unit.Vehicle != null && unit.Vehicle.Exists())
            { if (borrowed || Game.Player.Character != null && Game.Player.Character.IsInVehicle(unit.Vehicle)) GameUtils.SafeRelease(unit.Vehicle); else GameUtils.SafeDelete(unit.Vehicle); }
        }
        public void Clear()
        {
            var old = _units.ToArray(); _units.Clear(); _owner = null; _atFive = -1; _nextWave = 0;
            foreach (var unit in old) try { Release(unit); } catch (Exception ex) { Logger.Error("Military cleanup", ex); }
            var aftermath = _aftermath.ToArray(); _aftermath.Clear();
            foreach (var unit in aftermath) try { ReleaseAftermath(unit); } catch (Exception ex) { Logger.Error("Military aftermath release", ex); }
        }
    }
}

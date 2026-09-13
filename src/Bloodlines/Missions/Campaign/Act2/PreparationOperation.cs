using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>Shared ownership, road response and custody for M31-M35.</summary>
    public abstract class PreparationOperation : DesertOperation
    {
        protected readonly List<Ped> Opposition = new List<Ped>();
        protected RoleTracks Roles;
        protected Vehicle CrewCar;
        private readonly Dictionary<Ped, int> _boardingStarted = new Dictionary<Ped, int>();
        private readonly Dictionary<Ped, int> _boarding = new Dictionary<Ped, int>();
        private readonly List<Tuple<Vehicle, Ped, Ped>> _response = new List<Tuple<Vehicle, Ped, Ped>>();
        private readonly List<Blip> _responseBlips = new List<Blip>();
        private int _orders;
        protected bool Fighting;
        protected Func<Vector3> DrivingDestination;
        protected bool BeginCrew(CrewSlot active)
        {
            string site = Id == "M31" ? "M31.Senora" : Id;
            if (Id == "M31") BunkerSite.LoadMaps();
            if (!MissionSites.Prepare(Ctx.Locations, Id) ||
                !Ctx.Crew.Deploy(active, At(site + ".Start"), Ctx.Locations.Heading(site + ".Start"))) return false;
            ProtectCrew(); ApplyBibleSetting();
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan, CrewSlot.Guess })
                Station(slot, At(site + "." + slot + "Start"));
            Ctx.Crew.CompanionsHoldPosition = true;
            Roles = new RoleTracks(Ctx.Crew, () => Opposition);
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan, CrewSlot.Guess })
                Roles.For(slot).Observe(At(site + "." + slot + "Start"), At(site + "." + slot + "Start"));
            return true;
        }
        protected Vehicle CrewTransport(string key)
        {
            var car = Ctx.Vans?.Spawn(At(key), Ctx.Locations.Heading(key));
            if (car == null) car = Car("granger", At(key), Ctx.Locations.Heading(key));
            else Track(car);
            if (car != null) { car.IsPersistent = true; RequireAsset(car, "The crew's extraction car was destroyed."); }
            return car;
        }
        protected Ped Person(string modelName, string key, bool friendly = true)
        {
            var model = new Model(modelName);
            try
            {
                if (!GameUtils.RequestModel(model)) return null;
                var ped = Track(World.CreatePed(model, At(key), Ctx.Locations.Heading(key)));
                if (ped == null || !ped.Exists()) return null;
                ped.IsPersistent = true; ped.BlockPermanentEvents = true;
                ped.MaxHealth = 600; ped.Health = 600;
                ped.RelationshipGroup = friendly ? Ctx.Crew.PedFor(CrewSlot.Guess).RelationshipGroup : World.AddRelationshipGroup("BLOODLINES_RAMOSESCORT");
                return ped;
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        protected Ped Enemy(string key)
        {
            var ped = Guard(At(key));
            if (ped == null) throw new InvalidOperationException("Cannot place guard " + key);
            Opposition.Add(ped);
            var blip = Track(ped.AddBlip()); blip.Color = BlipColor.Red; blip.Name = "Armed guard";
            return ped;
        }
        protected void Establish(string phase, string title, string reason, params Entity[] subjects)
        {
            var blocking = new SceneBlocking();
            foreach (var subject in subjects)
                if (subject != null && subject.Exists()) blocking.Then(ShotStep.Low(1800, subject, 5, 3, 2));
            RequiredScene(phase, title, reason, blocking);
        }
        protected Prop Equipment(string model, string key)
        {
            var prop = WorkProp(model, At(key));
            if (!RequireAssets(prop)) throw new InvalidOperationException("Cannot create equipment at " + key);
            prop.Heading = Ctx.Locations.Heading(key); return prop;
        }
        protected void SaveCargo(Prop item, Entity carrier, Vector3 offset)
        {
            Function.Call(Hash.SET_ENTITY_COLLISION, item, false, false);
            if (!StowPropStep.Stow(item, carrier, offset)) throw new InvalidOperationException("Cargo could not attach to its carrier.");
        }
        protected bool Attached(Prop item, Entity carrier) => item != null && item.Exists() &&
            carrier != null && carrier.Exists() && Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY, item, carrier);
        protected void Carry(Prop item, CrewSlot slot)
        {
            Function.Call(Hash.SET_ENTITY_COLLISION, item, false, false);
            var actor = Ctx.Crew.PedFor(slot);
            if (!CarryPropStep.Attach(actor, item, new Vector3(.12f, .02f, -.02f), new Vector3(0, 90, 0)))
                throw new InvalidOperationException("The case could not be placed in the carrier's hand.");
        }
        protected bool Board(Ped actor, Vehicle vehicle, VehicleSeat seat)
        {
            if (actor == null || !actor.Exists() || actor.IsDead || vehicle == null || !vehicle.Exists()) return false;
            if ((int)seat + 2 > Function.Call<int>(Hash.GET_VEHICLE_MODEL_NUMBER_OF_SEATS, vehicle.Model.Hash))
            { Fail("The extraction vehicle does not have the required seat. Retry with a compatible crew car."); return false; }
            if (actor.IsInVehicle(vehicle) && actor.SeatIndex == seat)
            { _boarding.Remove(actor); _boardingStarted.Remove(actor); return true; }
            if (actor.IsInVehicle(vehicle) && actor != Game.Player.Character)
            { actor.Task.LeaveVehicle(); return false; }
            if (actor == Game.Player.Character || vehicle.Speed > 1.5f) { _boardingStarted.Remove(actor); return false; }
            if (actor.Position.DistanceTo(vehicle.Position) > 25f)
            { _boardingStarted.Remove(actor); if (!_boarding.TryGetValue(actor,out int walked) || Game.GameTime-walked>6000) { actor.Task.GoTo(vehicle.Position); _boarding[actor] = Game.GameTime; } return false; }
            if (!_boardingStarted.TryGetValue(actor, out int started)) _boardingStarted[actor] = Game.GameTime;
            else if (Game.GameTime - started > 45000)
            { Fail("A passenger could not reach the extraction seat. Retry with the vehicle stopped clear of obstacles."); return false; }
            if (!_boarding.TryGetValue(actor, out int when) || Game.GameTime - when > 8000)
            {
                if (!vehicle.IsSeatFree(seat)) { Fail("An extraction seat is occupied. Clear the required seats and retry."); return false; }
                actor.Task.EnterVehicle(vehicle, seat); _boarding[actor] = Game.GameTime;
            }
            return actor.IsInVehicle(vehicle);
        }
        protected bool BoardBrothers(Vehicle car)
        {
            Roles?.Release(); Ctx.Crew.CompanionsHoldPosition = false;
            bool all = true;
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan, CrewSlot.Guess })
            {
                Ctx.Crew.CompanionAI.TakeControl(slot);
                var seat = slot == CrewSlot.Guess ? VehicleSeat.Driver : slot == CrewSlot.Ice ? VehicleSeat.LeftRear : VehicleSeat.RightRear;
                if (!Board(Ctx.Crew.PedFor(slot), car, seat)) all = false;
            }
            return all;
        }
        protected void ResponseCar(string key, Vector3 destination)
        {
            var car = Car("mesa", At(key), Ctx.Locations.Heading(key), false);
            var driver = Guard(At(key)); var gunner = Guard(At(key));
            if (!RequireAssets(car, driver, gunner)) throw new InvalidOperationException("The response convoy could not load at " + key);
            driver.SetIntoVehicle(car, VehicleSeat.Driver); gunner.SetIntoVehicle(car, VehicleSeat.Passenger);
            car.IsEngineRunning = true;
            Opposition.Add(driver); Opposition.Add(gunner); _response.Add(Tuple.Create(car, driver, gunner));
            var blip = Track(car.AddBlip()); blip.Color = BlipColor.Red; blip.Name = "Response vehicle"; _responseBlips.Add(blip);
            driver.Task.DriveTo(car, destination, 14f, 26f, (DrivingStyle)CrewDriving.TrafficFlags);
        }
        protected void RetreatResponse()
        {
            Fighting = false;
            foreach (var blip in _responseBlips) if (blip != null && blip.Exists()) blip.Delete();
            _responseBlips.Clear();
            foreach (var unit in _response)
            {
                if (unit.Item2 != null && unit.Item2.Exists()) unit.Item2.Task.ClearAll();
                if (unit.Item3 != null && unit.Item3.Exists()) unit.Item3.Task.ClearAll();
                if (!unit.Item1.Exists() || unit.Item1.IsDead || !unit.Item2.Exists() || unit.Item2.IsDead || !unit.Item2.IsInVehicle(unit.Item1)) continue;
                var from = unit.Item1.Position; var player = Game.Player.Character.Position;
                var away = from + new Vector3(from.X >= player.X ? 500f : -500f, from.Y >= player.Y ? 500f : -500f, 0f);
                unit.Item2.Task.DriveTo(unit.Item1, away, 20f, 28f, (DrivingStyle)CrewDriving.TrafficFlags);
            }
        }
        protected static void DriveBy(Ped actor, Ped target)
        {
            if (actor.SeatIndex == VehicleSeat.Driver) return;
            if (actor.SeatIndex == VehicleSeat.LeftRear && actor.CurrentVehicle != null &&
                (actor.CurrentVehicle.Model == new Model("halftrack") || actor.CurrentVehicle.Model == new Model("technical")))
            { actor.Task.VehicleShootAtPed(target); return; }
            Function.Call(Hash.TASK_DRIVE_BY, actor, target, 0, 0f, 0f, 0f, 100f, 30, false,
                Game.GenerateHash("FIRING_PATTERN_BURST_FIRE_DRIVEBY"));
        }
        protected void TickSupport()
        {
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan, CrewSlot.Guess })
                if (slot != Ctx.Crew.ActiveSlot && Ctx.Crew.CompanionAI.StateOf(slot) != CompanionState.Scripted)
                    Ctx.Crew.CompanionAI.TakeControl(slot);
            Roles?.Update();
            if (Game.GameTime < _orders) return;
            _orders = Game.GameTime + 2500;
            if (DrivingDestination != null)
            {
                var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
                if (Ctx.Crew.ActiveSlot != CrewSlot.Guess && guess != null && guess.IsInVehicle() && guess.SeatIndex == VehicleSeat.Driver)
                { Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Guess); CrewDriving.Configure(guess, CrewSlot.Guess, Fighting); guess.Task.DriveTo(guess.CurrentVehicle, DrivingDestination(), 12f, 32f, (DrivingStyle)CrewDriving.TrafficFlags); }
            }
            if (!Fighting) return;
            var targets = Protagonist.All.Select(h => Ctx.Crew.PedFor(h.Slot)).Where(p => p != null && p.Exists() && !p.IsDead).ToArray();
            foreach (var unit in _response)
            {
                if (!unit.Item1.Exists() || unit.Item1.IsDead) continue;
                var target = targets.OrderBy(p => p.Position.DistanceTo(unit.Item1.Position)).FirstOrDefault();
                if (target == null) continue;
                if (unit.Item2.Exists() && !unit.Item2.IsDead && unit.Item2.IsInVehicle(unit.Item1))
                {
                    if (unit.Item1.Position.DistanceTo(target.Position) < 45f && !target.IsInVehicle()) unit.Item2.Task.LeaveVehicle();
                    else unit.Item2.Task.DriveTo(unit.Item1, target.Position, 16f, 27f, (DrivingStyle)CrewDriving.TrafficFlags);
                }
                if (unit.Item3.Exists() && !unit.Item3.IsDead && unit.Item3.IsInVehicle(unit.Item1))
                {
                    if (unit.Item1.Position.DistanceTo(target.Position) < 50f && !target.IsInVehicle()) unit.Item3.Task.LeaveVehicle();
                    else DriveBy(unit.Item3, target);
                }
            }
            foreach (var enemy in Opposition.Where(p => p != null && p.Exists() && !p.IsDead && !p.IsInVehicle()))
            {
                var target = targets.OrderBy(p => p.Position.DistanceTo(enemy.Position)).FirstOrDefault();
                if (target != null) enemy.Task.FightAgainst(target);
            }
            foreach (var slot in new[] { CrewSlot.Guess, CrewSlot.Gohan, CrewSlot.Ice })
            {
                if (slot == Ctx.Crew.ActiveSlot) continue;
                var actor = Ctx.Crew.PedFor(slot);
                if (actor == null || !actor.Exists() || actor.IsDead || (actor.IsInVehicle() && actor.SeatIndex == VehicleSeat.Driver)) continue;
                var threat = Opposition.Where(p => p != null && p.Exists() && !p.IsDead && p.Position.DistanceTo(actor.Position) < 110f)
                    .OrderBy(p => p.Position.DistanceTo(actor.Position)).FirstOrDefault();
                if (threat != null) { if (actor.IsInVehicle()) DriveBy(actor, threat); else actor.Task.FightAgainst(threat); }
            }
        }
        protected override void OnUpdate() { TickSupport(); base.OnUpdate(); }
        protected override void OnCleanup() { Roles?.Release(); _boarding.Clear(); _boardingStarted.Clear(); DrivingDestination = null; base.OnCleanup(); }
    }
}

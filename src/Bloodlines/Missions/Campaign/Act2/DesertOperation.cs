using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>Owned spawning helpers for the desert chapters; stages remain individually authored.</summary>
    public abstract class DesertOperation : ComposedMission
    {
        protected Vector3 At(string key) => Ctx.Locations.Position(key);
        protected void ProtectCrew()
        {
            foreach (var hero in Protagonist.All)
                RequireSurvivor(Ctx.Crew.PedFor(hero.Slot), hero.Handle + " is down. Restart the mission to rebuild the crew and objectives.");
        }
        protected Prop WorkProp(string name, Vector3 point, bool ground = true, bool reuse = false)
        {
            var model = new Model(name);
            try
            {
                if (!GameUtils.RequestModel(model)) return null;
                if (reuse)
                {
                    var previous = World.GetNearbyProps(point, 2f).FirstOrDefault(p => p != null && p.Exists()
                        && p.Model == model && GameUtils.IsWithinFlat(p.Position, point, .5f));
                    if (previous != null) { Track(previous); Preserve(previous); return previous; }
                }
                var prop = Track(World.CreateProp(model, point, false, ground));
                if (prop == null || !prop.Exists()) return null;
                prop.IsPersistent = true; prop.IsPositionFrozen = true;
                return prop;
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        protected void RequiredScene(string phase, string title, string reason, SceneBlocking blocking)
        {
            var spec = new SceneSpec { MissionId = Id, Phase = phase, Title = title,
                Reason = reason, RequiresCompletion = true, Blocking = blocking };
            if (Ctx.Cutscenes.Play(spec)) return;
            // A missing camera can use the same physical result as a deliberate skip.
            blocking.Complete();
            if (!blocking.Succeeded) throw new System.InvalidOperationException(title + " could not complete.");
        }
        protected Vehicle Car(string modelName, Vector3 point, float heading = 0, bool markTransport = true)
        {
            var model = new Model(modelName);
            try
            {
                if (!GameUtils.RequestModel(model)) return null;
                var vehicle = Track(World.CreateVehicle(model, point, heading));
                if (vehicle == null || !vehicle.Exists()) return null;
                vehicle.IsPersistent = true;
                if (model.IsCar) vehicle.PlaceOnGround();
                var blip = markTransport ? Track(vehicle.AddBlip()) : null;
                if (blip != null) { blip.Color = BlipColor.Orange; blip.Name = "Required mission transport"; }
                return vehicle;
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        protected Ped Guard(Vector3 point, WeaponHash weapon = WeaponHash.CarbineRifle)
        {
            var model = new Model("s_m_y_blackops_01");
            try
            {
                if (!GameUtils.RequestModel(model)) return null;
                // A guard post is not required to have pedestrian navmesh under it:
                // a compound floor, a jetty, a deck and a seabed platform all count,
                // and M32 and M39 both died refusing one (Ron, September 13). Prefer
                // navmesh where it exists, otherwise stand him on the authored point's
                // own ground, and only give up if the ped itself cannot be created.
                var safe = World.GetSafeCoordForPed(point, false, 0);
                if (safe == Vector3.Zero || safe.DistanceTo(point) > 35f)
                {
                    safe = GameUtils.OnGround(point);
                    Logger.Warn("Guard post has no navmesh; standing him on the authored point at " + safe + ".");
                }
                var ped = Track(World.CreatePed(model, safe, 180));
                if (ped == null || !ped.Exists()) return null;
                ped.IsPersistent = true; ped.BlockPermanentEvents = true;
                ped.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
                ped.Health = 220; ped.Armor = 40; ped.Accuracy = 28;
                ped.Weapons.Give(weapon, 180, true, true);
                ped.Task.GuardCurrentPosition();
                return ped;
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        protected List<Ped> Squad(Vector3 point, int count)
        {
            var result = new List<Ped>();
            for (int i=0;i<count;i++)
            {
                var ped = Guard(point + new Vector3(i*4f, i%2*6f, 0));
                if (ped == null) break;
                result.Add(ped);
            }
            return result;
        }
        protected void Attack(IEnumerable<Ped> peds)
        { foreach (var ped in peds.Where(p=>p!=null&&p.Exists()&&!p.IsDead)) ped.Task.FightAgainstHatedTargets(180f); }
        protected Vehicle FuelRig(string key, out Vehicle trailer)
        {
            var location = Ctx.Locations.Get(key);
            var truck = Car("phantom", location.Position, location.Heading);
            trailer = truck == null ? null : Car("tanker", truck.Position - truck.ForwardVector * 11f, location.Heading);
            if (truck != null && trailer != null) Function.Call(Hash.ATTACH_VEHICLE_TO_TRAILER, truck, trailer, 2f);
            return truck;
        }
        protected void ReleaseForPickup()
        {
            Ctx.Crew.CompanionAI.ReleaseAll(); Ctx.Crew.CompanionsHoldPosition = false;
            Ctx.Crew.CompanionAI.RequireSharedVehicle = true; Ctx.Crew.AssignCompanionAI();
        }
    }
}

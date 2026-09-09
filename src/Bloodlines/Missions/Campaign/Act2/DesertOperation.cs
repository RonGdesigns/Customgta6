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
        protected Vehicle Car(string modelName, Vector3 point, float heading = 0)
        {
            var model = new Model(modelName);
            try
            {
                if (!GameUtils.RequestModel(model)) return null;
                var vehicle = Track(World.CreateVehicle(model, point, heading));
                if (vehicle == null || !vehicle.Exists()) return null;
                vehicle.IsPersistent = true;
                if (model.IsCar) vehicle.PlaceOnGround();
                var blip = Track(vehicle.AddBlip());
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
                var safe = World.GetSafeCoordForPed(point, false, 0);
                if (safe == Vector3.Zero || safe.DistanceTo(point) > 35f) return null;
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

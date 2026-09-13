using System;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;
namespace Bloodlines.Missions.Campaign
{
    public abstract class CoastalOperation : PreparationOperation
    {
        protected Vehicle Sub, Launch;
        protected bool BeginCoast()
        {
            if (!BeginCrew(CrewSlot.Gohan)) return false;
            Sub = Boat("submersible2", Id + ".Sub", 3f, 3.5f, 5f);
            Launch = Boat("dinghy", Id + ".Boat", 2f, 2.5f, 4f);
            if (!RequireAssets(Sub, Launch)) return false;
            Station(CrewSlot.Gohan, Sub, VehicleSeat.Driver);
            Station(CrewSlot.Guess, Launch, VehicleSeat.Driver);
            Roles.For(CrewSlot.Guess).Stop(); Roles.For(CrewSlot.Gohan).Stop();
            return true;
        }
        protected Vehicle Boat(string model, string key, float draft, float width, float length)
        {
            var boat = Car(model, MarineSites.ResolveOrThrow(Ctx.Locations, key, draft, width, length), Ctx.Locations.Heading(key));
            if (boat != null) RequireAsset(boat, "The required marine transport was destroyed.");
            return boat;
        }
        protected Prop SeabedEquipment(string key)
        {
            var prop = WorkProp("prop_elecbox_12", At(key), false);
            if (!RequireAssets(prop)) throw new InvalidOperationException("Underwater equipment failed at " + key);
            return prop;
        }
        protected bool Recorded; protected int ExposedSince = -1;
        protected void CheckSurfaceExposure(Ped patrol)
        {
            bool seen = Sub.Position.Z > -2f && patrol != null && patrol.Exists() && !patrol.IsDead &&
                patrol.Position.DistanceTo(Sub.Position) < 65f &&
                Function.Call<bool>(Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY, patrol, Sub, 17);
            if (!seen) { ExposedSince = -1; return; }
            if (ExposedSince < 0) ExposedSince = Game.GameTime;
            int left = Math.Max(0, 8 - (Game.GameTime - ExposedSince) / 1000);
            GameUtils.Subtitle("~r~Patrol has the surfaced sub: dive or move clear (" + left + "s)", 500);
            if (left == 0) Fail("The surfaced sub remained visible to the patrol. Dive or avoid its route on retry.");
        }
    }
}

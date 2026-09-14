using System.Collections.Generic;
using System.Linq;
using Bloodlines.Crew;
using GTA;

namespace Bloodlines.Missions
{
    /// <summary>
    /// What the Port Heist itself considers intact. The ownership, the named
    /// handles, the crew check and the disposal belong to <see cref="OperationWorld"/>;
    /// only the bullion, the lift, the two escorts and the beach are this heist's.
    /// </summary>
    public sealed class PortHeistWorld : OperationWorld
    {
        public PortHeistWorld(MissionContext context) : base(context) { }

        public override string Label => "Port Heist";

        public override bool ValidateActive(Mission phase, out string reason)
        {
            if (!CrewReady(out reason)) return false;
            var keys = new List<string> { "lift" };
            if (phase is Campaign.M19UnderwaterBreach) keys.Add("kraken");
            if (!(phase is Campaign.M19UnderwaterBreach) || Get<Prop>("bullion") != null) keys.Add("bullion");
            if (phase is Campaign.M20SkyHook hook && !hook.Transferred) { keys.Add("kraken"); keys.Add("launch"); }
            if (phase is Campaign.M21OpenWater escort) { if (!escort.Transferred) keys.Add("launch"); keys.Add("granger"); }
            if (phase is Campaign.M22ScorchedBay) keys.Add("granger");
            if (!Intact(keys, out reason)) return false;
            bool cableRequired = phase is Campaign.M20SkyHook h && h.Hooked || phase is Campaign.M21OpenWater ||
                phase is Campaign.M22ScorchedBay b && !b.Dropped;
            if (cableRequired && !Attached(Get<Prop>("bullion"), Get<Vehicle>("lift")))
            { reason = "The bullion separated from the lift before its planned drop."; return false; }
            reason = null;
            return true;
        }

        public override bool ValidatePhaseEnd(Mission phase, out string reason)
        {
            if (!CrewReady(out reason)) return false;
            var lift = Get<Vehicle>("lift");
            var cargo = Get<Prop>("bullion");
            if (lift == null || !lift.Exists() || !lift.IsDriveable || cargo == null || !cargo.Exists())
            { reason = "The loaded operation state is incomplete. " + RestartNotice; return false; }
            var guess = Context.Crew.PedFor(CrewSlot.Guess);
            var ice = Context.Crew.PedFor(CrewSlot.Ice);
            var gohan = Context.Crew.PedFor(CrewSlot.Gohan);
            if (phase is Campaign.M19UnderwaterBreach breach)
            {
                if (!breach.Floated || !Seated(gohan, breach.Kraken, VehicleSeat.Driver) ||
                    breach.Kraken.Position.DistanceTo(Context.Locations.Position("M19.Surface")) > 16f)
                { reason = "Surface the Kraken with the floated container ready before the lift."; return false; }
            }
            else if (phase is Campaign.M20SkyHook hook)
            {
                if (!hook.Hooked || !hook.Transferred || !Attached(cargo, lift) ||
                    !Seated(guess, lift, VehicleSeat.Driver) || !Seated(gohan, hook.Launch, VehicleSeat.Driver) ||
                    !Seated(ice, hook.Launch, VehicleSeat.Passenger))
                { reason = "The cable and escort seats are not ready. " + RestartNotice; return false; }
            }
            else if (phase is Campaign.M21OpenWater escort)
            {
                if (!escort.Transferred || !Attached(cargo, lift) || !Seated(guess, lift, VehicleSeat.Driver) ||
                    !Seated(gohan, escort.Granger, VehicleSeat.Driver) || !Seated(ice, escort.Granger, VehicleSeat.Passenger))
                { reason = "The shore transfer did not finish with the crew and bullion intact."; return false; }
            }
            else if (phase is Campaign.M22ScorchedBay bay)
            {
                var beach = Context.Locations.Position("M22.Beach");
                var drop = Context.Locations.Position("M22.AlamoDrop");
                if (!bay.Arrived || !bay.Dropped || !bay.Landed || !bay.Struck || Attached(cargo, lift) ||
                    cargo.Position.DistanceTo(drop) > 15f || lift.IsInAir || lift.Speed > 2f ||
                    Protagonist.All.Any(hero => { var ped = Context.Crew.PedFor(hero.Slot); return ped.IsInVehicle() || ped.Position.DistanceTo(beach) > 45f; }))
                { reason = "The cargo must be deposited, the aircraft landed and all three men on the beach."; return false; }
            }
            reason = null;
            return true;
        }
    }
}

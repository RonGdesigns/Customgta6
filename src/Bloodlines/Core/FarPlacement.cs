using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Keeping the world built under a brother the player is nowhere near.
    ///
    /// Collision streams around the player and the focus, not around every ped. Ice on the
    /// Vinewood sign in M59 is created 515 m from the transmitter yard where the player
    /// stands; M55 stations two brothers in penthouses 450 m from the first. Without this the
    /// engine holds such a man where he is until the player comes within range, and the
    /// ground then arrives under a man it may already have dropped through it.
    ///
    /// <c>SET_ENTITY_LOAD_COLLISION_FLAG</c> is the engine's own answer for exactly this and
    /// it is left set. Nothing here freezes him: the switch machinery already waits on
    /// collision before handing him to the player, and a freeze that a scene captured and
    /// restored would be a brother nobody ever thawed.
    /// </summary>
    public static class FarPlacement
    {
        /// <summary>Farther than this from the player, and the world is not built under him.</summary>
        public const float Meters = 150f;

        public static void Keep(Ped ped, string why)
        {
            if (ped == null || !ped.Exists()) return;
            try
            {
                var at = ped.Position;
                Function.Call(Hash.SET_ENTITY_LOAD_COLLISION_FLAG, ped, true);
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, at.X, at.Y, at.Z);
                Logger.Info("Keeping collision loaded around a brother " + why + " at " + at + ".");
            }
            catch (System.Exception ex) { Logger.Warn("Could not ask for collision around a far brother: " + ex.Message); }
        }
    }
}

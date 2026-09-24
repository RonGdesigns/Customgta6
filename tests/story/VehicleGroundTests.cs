using Bloodlines.Core;

public static partial class StoryTests
{
    /// <summary>
    /// Ron, September 23: the crew van spawned on the roof of the Cypress Flats warehouse. His
    /// surveyed stash is on the floor at z 30.5; the roof over it is at 43.7.
    /// </summary>
    static void VehicleGroundChecks()
    {
        // A floor at 30.5 under a roof at 43.7: the first surface below a height.
        System.Func<float, float?> warehouse = from => from > 43.7f ? 43.7f : from > 30.5f ? 30.5f : (float?)null;
        Check(VehicleGround.Choose(30.5f, warehouse) == 30.5f, "A van spawned under a roof stays on the floor under it, not on the roof");

        // M03's Primo: an estimate 4 m under the terrain at 20, where a probe from inside the
        // ground falls through to a cave at 2.
        System.Func<float, float?> hillside = from => from > 20f ? 20f : 2f;
        Check(VehicleGround.Choose(16f, hillside) == 20f, "A spawn under the terrain is still lifted onto it");

        // Nothing answers at all: the vehicle is left where it was placed.
        Check(VehicleGround.Choose(10f, from => null) == null, "No ground answer leaves the spawn alone");
    }
}

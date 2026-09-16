using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Missions;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
    /// <summary>
    /// Getting into the vessel, and the probe that tells a deck from a cabin floor.
    ///
    /// Ron boarded at the stern platform and could not get inside the ship, so M46's
    /// first marker sat in a room he could not reach. The map request was there and the
    /// interior was never pinned — the apartments have pinned theirs since the prologue,
    /// and the yacht had nothing.
    /// </summary>
    static void PaletoInteriorChecks()
    {
        // ---- The upward probe. Something overhead is a deckhead; nothing is the sky.
        var at = new Vector3(-1734f, 5325f, 15.5f);
        World.RaycastHandler = (from, to) => new RaycastResult { DidHit = true, HitPosition = to };
        Check(!MissionSites.OpenAbove(at, 2.2f), "A point with something over it is not open to the sky");
        World.RaycastHandler = (from, to) => new RaycastResult();
        Check(MissionSites.OpenAbove(at, 2.2f), "A point with nothing over it is");
        Check(MissionSites.OpenAbove(at, 0f), "Asking for no clearance at all is always satisfied");
        // The probe goes up from the surface, not down into it: a ray that started at the
        // feet would hit the deck the man is standing on every time.
        Vector3 start = Vector3.Zero, end = Vector3.Zero;
        World.RaycastHandler = (from, to) => { start = from; end = to; return new RaycastResult(); };
        MissionSites.OpenAbove(at, 2.2f);
        Check(start.Z > at.Z && end.Z > start.Z, "and it looks upward from just above the surface");
        Check(Math.Abs(end.Z - (at.Z + 2.2f)) < .01f, "as far as the clearance it was asked for");
        World.RaycastHandler = null;

        // ---- The interior itself.
        int wasInterior = Function.InteriorId;
        bool wasReady = Function.InteriorReady, wasDisabled = Function.InteriorDisabled, wasCapped = Function.InteriorCapped;
        Function.InteriorId = 4242; Function.InteriorReady = true;
        Function.InteriorDisabled = true; Function.InteriorCapped = true;
        Function.Calls.Clear();

        var inside = new ScriptedInterior("test vessel", new Vector3(-1769.67f, 5334.14f, 4.89f));
        Check(inside.Id == 0 && !inside.Ready, "An interior nobody has looked for yet is not open");
        Check(inside.Ensure(), "Ensure finds it, opens it and reports it ready");
        Check(inside.Id == 4242, "It holds the interior the game named");
        Check(Function.Calls.Any(c => c.Item1 == Hash.PIN_INTERIOR_IN_MEMORY) &&
              Function.Calls.Any(c => c.Item1 == Hash.REFRESH_INTERIOR),
            "and pins and refreshes it, which is the step the yacht never had");
        Check(Function.Calls.Any(c => c.Item1 == Hash.DISABLE_INTERIOR && (bool)c.Item2[1] == false) &&
              Function.Calls.Any(c => c.Item1 == Hash.CAP_INTERIOR && (bool)c.Item2[1] == false),
            "An interior found disabled or capped is opened up");

        int pinned = Function.Calls.Count(c => c.Item1 == Hash.PIN_INTERIOR_IN_MEMORY);
        inside.Ensure(); inside.Ensure();
        Check(Function.Calls.Count(c => c.Item1 == Hash.PIN_INTERIOR_IN_MEMORY) == pinned,
            "Calling it every frame pins it once");
        Check(inside.Covers(new Vector3(-1770f, 5334f, 4.89f)), "It can say whether a point is inside what loaded");

        // ---- Whatever it was found as, it is put back. The same contract the scripted map
        // keeps: release exactly what we turned on, on every exit path.
        Function.Calls.Clear();
        inside.Release();
        Check(Function.Calls.Any(c => c.Item1 == Hash.UNPIN_INTERIOR), "Release unpins it");
        Check(Function.Calls.Any(c => c.Item1 == Hash.CAP_INTERIOR && (bool)c.Item2[1]) &&
              Function.Calls.Any(c => c.Item1 == Hash.DISABLE_INTERIOR && (bool)c.Item2[1]),
            "and puts the disabled and capped state back the way it found them");
        Function.Calls.Clear();
        inside.Release();
        Check(!Function.Calls.Any(c => c.Item1 == Hash.UNPIN_INTERIOR), "Releasing twice does it once");
        Check(!inside.Ensure(), "and a released interior is not reopened behind the mission's back");

        // ---- An interior the map has not placed yet answers zero, which is a wait, not a
        // failure: the first frames after a map request have nothing there.
        Function.InteriorId = 0;
        var missing = new ScriptedInterior("not there yet", new Vector3(0f, 0f, 0f));
        Check(!missing.Ensure() && missing.Id == 0, "An interior the game has not placed yet is asked for again next frame");
        Function.InteriorId = 4242;
        Check(missing.Ensure(), "and found the moment it is there");
        missing.Release();

        Function.InteriorId = wasInterior; Function.InteriorReady = wasReady;
        Function.InteriorDisabled = wasDisabled; Function.InteriorCapped = wasCapped;

        // ---- Wiring. The interior is asked for in the chapter that first stands on the
        // vessel, not the one that needs it: M45 is several minutes of deck fight ahead of
        // the door, and that is the streaming budget.
        string world = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "PaletoWorld.cs"));
        string m45 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M45PaletoBreach.cs"));
        string m46 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M46PaletoVault.cs"));
        Check(world.Contains("new ScriptedInterior(\"Paleto vessel\"") && world.Contains("public ScriptedInterior Inside"),
            "The Paleto world owns the vessel's interior beside its map");
        Check(world.Contains("Inside.Release(); Structure.Release();"),
            "and gives it back on pass, failure and abort with everything else");
        Check(m45.Contains("Paleto.EnsureInside(Ctx);") && m46.Contains("Paleto.EnsureInside(Ctx);"),
            "M45 opens it and M46 opens it too, so a chapter run alone in QA still has a way in");
        Check(m45.Contains("record.Notes[\"interior\"]"),
            "and the handoff records whether the way in was actually there when Gohan came aboard");
        Check(PaletoWorld.InteriorProbes.Length >= 1 &&
              PaletoWorld.InteriorProbes[0].DistanceTo(new Vector3(-1769.67f, 5334.14f, 4.89f)) < .01f,
            "It looks for the interior at the MLO's own placed origin, which is a read number");
    }
}

using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
    /// <summary>
    /// The quick order strip, its host wiring, the smaller mission HUD, and the M55 exit
    /// and security that Ron's September 17 play threw up.
    /// </summary>
    static void CrewOrderChecks()
    {
        // ---- The strip: hold, pick, release.
        Reset(); var crew = Roster(); Use(crew, CrewSlot.Ice);
        Game.GameTime = 50000; Game.TimeScale = 1f;
        var strip = new CrewOrderStrip { HasWaypoint = () => false };
        CrewSlot? loggedSlot = null; string logged = null;
        strip.Sent = (slot, text) => { loggedSlot = slot; logged = text; };
        Check(strip.Open(crew) && strip.IsOpen, "The strip opens for a deployed crew");
        Check(strip.Brothers.Count == 2 && !strip.Brothers.Contains(CrewSlot.Ice), "and lists the two brothers who are not being played");
        Check(Game.TimeScale < .3f, "Time slows while it is up, the way the character wheel slows it");
        Check(strip.Orders.SequenceEqual(new[] { CrewOrder.FollowMe, CrewOrder.HoldHere, CrewOrder.OwnThing }),
            "On foot with no vehicle about, the three standing orders are all that is offered");
        strip.Move(MenuDirection.Right, crew); strip.Move(MenuDirection.Down, crew);
        Check(strip.SelectedBrother == CrewSlot.Guess && strip.SelectedOrder == CrewOrder.HoldHere, "Right picks the next brother, down the next order");
        Check(strip.Send(crew) && !strip.IsOpen, "Release sends and closes");
        Check(Math.Abs(Game.TimeScale - 1f) < .001f, "and gives the time scale back");
        Check(crew.CompanionAI.OrderOf(CrewSlot.Guess) == CrewOrder.HoldHere, "The order reached the controller for the brother picked");
        Check(strip.EchoShowing && strip.EchoText == "Guess: holding here" && loggedSlot == CrewSlot.Guess && logged == strip.EchoText,
            "and what he says back is echoed on the HUD and logged for the phone");
        Game.GameTime += CrewOrderStrip.EchoMs + 1;
        Check(!strip.EchoShowing, "The echo goes after its moment");

        // With the player driving a gun truck the vehicle orders come first.
        var truck = new Vehicle { Capacity = 3 }; truck.TurretSeats.Add(VehicleSeat.RightRear);
        Game.Player.Character.SetIntoVehicle(truck, VehicleSeat.Driver);
        strip.Open(crew);
        Check(strip.Orders[0] == CrewOrder.ManTheGun && strip.Orders.Contains(CrewOrder.GetIn) && !strip.Orders.Contains(CrewOrder.TakeTheWheel),
            "Driving a gun truck, the first thing offered is the gun; the wheel is the player's");
        strip.Close();
        Game.Player.Character.Task.LeaveVehicle(); Game.Player.LastVehicle = truck; truck.Position = Game.Player.Character.Position;
        strip.HasWaypoint = () => true;
        strip.Open(crew);
        Check(strip.Orders[0] == CrewOrder.TakeTheWheel && strip.Orders.Contains(CrewOrder.DriveToWaypoint),
            "Out of it and beside it, the wheel is offered, and with a waypoint set so is the drive to it");
        strip.Close();

        // The controller gesture: a tap never opens it, a hold does, release sends.
        Game.GameTime = 60000;
        Check(!strip.HandlePad(true, 0f, 0f, crew) && !strip.IsOpen, "The first frame of a press is a tap for whatever the button does for the game");
        Game.GameTime += CrewOrderStrip.HoldToOpenMs - 50;
        Check(!strip.HandlePad(true, 0f, 0f, crew) && !strip.IsOpen, "and so is anything short of the hold");
        Game.GameTime += 100;
        Check(strip.HandlePad(true, 0f, 0f, crew) && strip.IsOpen, "Held past it, the strip is up and owns the button");
        strip.HandlePad(true, 1f, 0f, crew); strip.HandlePad(true, 1f, 0f, crew);
        Check(strip.SelectedBrother == CrewSlot.Guess, "A stick deflection is one step until it comes back to center");
        strip.HandlePad(true, 0f, 0f, crew); strip.HandlePad(true, 1f, 0f, crew);
        Check(strip.SelectedBrother == CrewSlot.Gohan, "and steps again once it has");
        Check(strip.HandlePad(false, 0f, 0f, crew) && !strip.IsOpen && crew.CompanionAI.OrderOf(CrewSlot.Gohan) == CrewOrder.TakeTheWheel,
            "Release sends the pick");
        Game.TimeScale = 1f; Game.Player.LastVehicle = null;

        // ---- Host wiring: the key, the pad, the gates, the draw.
        string host = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "BloodlinesMain.cs"));
        Check(host.Contains("Step(\"crew orders\", HandleCrewOrders);"), "The host steps the strip every tick");
        Check(host.Contains("if (_orders.IsOpen) Step(\"send crew order\", () => _orders.Send(_crew));"), "Letting go of the key sends");
        Check(host.Contains("Game.LastInputMethod == InputMethod.GamePad") && host.Contains("ControllerInput.Pressed(GTA.Control.VehicleRadioWheel)"),
            "The pad path only runs for a pad, on a hold of d-pad left");
        Check(host.Contains("Game.DisableControlThisFrame(GTA.Control.VehicleRadioWheel);"), "and the radio wheel is taken only once the strip is up");
        Check(host.Contains("!_missions.IsRunning && !_death.IsHandling") && host.IndexOf("private bool OrdersAvailable()", StringComparison.Ordinal) > 0,
            "It is a free-roam instrument: refused during missions and recovery");
        string config = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "ModConfig.cs"));
        Check(config.Contains("ReadKey(settings, \"CrewOrders\", config.CrewOrdersKey)") && config.Contains("public Keys CrewOrdersKey { get; private set; } = Keys.G;"),
            "The key is configurable and defaults to G");
        string ini = File.ReadAllText(Path.Combine(Repo, "config", "Bloodlines.ini"));
        Check(ini.Contains("CrewOrders = G") && ini.Contains("ControllerOrders = True"), "and the shipped ini says so");

        // ---- The seat rules the controller now keeps, as text: the harness that runs them is the thin one.
        string controller = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Crew", "CompanionController.cs"));
        Check(controller.Contains("CrewOrders.IsTurretSeat(vehicle, seat) == (pass == 0)"), "A boarding brother takes a turret before a plain seat");
        Check(controller.Contains("!LeaderStepping(vehicle, leader) && !CrewOrders.HoldsWheel(OrderOf(slot))"), "and a driver holds the wheel through a seat change or an order");

        // ---- The HUD is smaller. Ron: too big, information solid.
        Check(MissionHud.Width <= 420f && MissionHud.ObjectiveScale <= .25f && MissionHud.HeadingScale <= .22f && MissionHud.TimerScale <= .24f,
            "The mission HUD is about two thirds of its first size");
        string hud = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "MissionHud.cs"));
        Check(!hud.Contains(".27f") && !hud.Contains(".29f") && !hud.Contains(".30f"), "and none of the old text sizes survive in it");

        // ---- M55: a way out, and security on every node.
        string m55 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act3", "M55SkylineDescent.cs"));
        Check(M55SkylineDescent.TerminalGuards == 3 && M55SkylineDescent.VaultGuards == 3 && M55SkylineDescent.SuiteGuards == 4,
            "Every penthouse has security: the authored four for Ice, three each for the other two");
        Check(m55.Contains("() => _terminalGuards) { RequiredCharacter = CrewSlot.Gohan }") && m55.Contains("() => _vaultGuards) { RequiredCharacter = CrewSlot.Guess }"),
            "and each brother is asked to put his own down");
        // Matched with the line endings taken out: a source assertion that depends on
        // CRLF against LF passes in one checkout and fails in the next, which it did.
        string m55Flat = m55.Replace("\r", "").Replace("\n", " ");
        Check(m55.Split(new[] { "take the service elevator down" }, StringSplitOptions.None).Length == 4 &&
              System.Text.RegularExpressions.Regex.IsMatch(m55Flat, @"\.AnyOf\(\)\s*\.OnExit\(c => Descend\(\)\);"),
            "Each brother has a service elevator at his arrival point; the first one taken puts everybody on the street");
        Check(!m55.Contains("Script.Wait("), "The descent waits on nothing inside a tick");
        World.SafeCoordHandler = p => new Vector3(p.X + 12f, p.Y - 3f, 31.2f);
        try
        {
            var exit = M55SkylineDescent.StreetBelow(new Vector3(-13.08f, -593.62f, 93.03f), "test");
            Check(Math.Abs(exit.Z - 31.2f) < .01f && Math.Abs(exit.X + 1.08f) < .01f, "Street level is the sidewalk the engine finds under the tower, not a number written in a file");
        }
        finally { World.SafeCoordHandler = null; }
        Check(!File.ReadAllText(Path.Combine(Repo, "data", "locations.tsv")).Contains("M55.IceExit"), "and no exit key is authored, because nobody knows which side of the tower the sidewalk is on");
    }
}

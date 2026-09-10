using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
 static void M01ExitChecks()
 {
  // The September 10 log: the prologue placed Ron at the dock and M01 refused every
  // start on M01.ExitPoint, whose data row pointed 838 m away.
  Reset();var crew=Roster();var c=Context(crew);var exit=c.Locations.Get("M01.ExitPoint");var dock=c.Locations.Position("M01.RegroupPoint");
  Check(exit.Status!=LocationStatus.Surveyed&&GameUtils.IsWithinFlat(exit.Position,dock,260f)&&exit.Position.Z<20f,"M01's exit is the dockyard gate, a short drive from the regroup point, not the bible's address across the water");
  World.FailNavigationNear=exit.Position;
  Check(ProloguePlacement.Prepare(c.Locations),"The cold open no longer needs the exit gate's navmesh: it is a drive-to marker, not a spawn");
  Reset();crew=Roster();c=Context(crew);World.FailNavigationNear=c.Locations.Position("M01.CapoSpawn");
  Check(!ProloguePlacement.Prepare(c.Locations),"A spawn point with no walkable surface still refuses the cold open");
  World.FailNavigationNear=null;
  string placement=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Core","ProloguePlacement.cs"));
  Check(!placement.Contains("\"M01.RegroupPoint\", \"M01.ExitPoint\""),"The exit is out of the validated list in the source, not just tolerated");
 }
}

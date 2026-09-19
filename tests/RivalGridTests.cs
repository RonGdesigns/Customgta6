using System;
using System.Linq;
using Bloodlines.Core;
using GTA;
public static partial class RegressionTests
{
 /// <summary>
 /// Which cars KJ fields. Ron, September 19: "Did you make the drivers cars the same as mine
 /// that's not what I want them to do I do want them to drive better and have more of a
 /// challenge but I definitely don't want them to have my car." So: his own roster, matched
 /// to the player's class, and his exact model excluded whatever the matching says.
 /// </summary>
 static void RivalGridChecks()
 {
  // A stand-in speed table over KJ's roster: the real one comes from the game's own
  // model natives, and the selection must not care which.
  var speeds=new System.Collections.Generic.Dictionary<string,float>{
   {"penumbra",40.8f},{"futo",45.0f},{"buffalo",45.2f},{"gauntlet",47.0f},{"phoenix",47.3f},
   {"elegy2",48.7f},{"ninef",48.7f},{"cheetah",50.0f},{"comet2",50.2f},{"coquette",50.5f},
   {"entityxf",50.9f},{"adder",51.8f}};
  Func<string,float> speedOf=m=>speeds[m];
  Check(RivalGrid.Roster.Length>=8&&RivalGrid.Roster.All(m=>speeds.ContainsKey(m)),
   "KJ keeps a roster of his own cars, and every one of them is a car this test knows the speed of");
  Check(RivalGrid.Roster.Distinct().Count()==RivalGrid.Roster.Length,"No car appears on his roster twice");
  float span=speeds.Values.Max()-speeds.Values.Min();
  Check(span>8f,"The roster spans a real range of performance, so there is something comparable whatever the player brings");

  // ---- Matched to the player's class, not copied from his car.
  var fast=RivalGrid.Pick(2,51.5f,0,speedOf);
  Check(fast.Count==2&&fast.Contains("adder")&&fast.Contains("entityxf"),
   "Against a supercar he brings out his fastest two");
  var modest=RivalGrid.Pick(2,41f,0,speedOf);
  Check(modest.Count==2&&modest.Contains("penumbra"),
   "Against something modest he brings something that can live with it rather than his fastest");
  Check(!modest.Contains("adder"),"and does not turn up in a supercar to race a runabout");
  Check(RivalGrid.Pick(2,47f,0,speedOf).Distinct().Count()==2,"The two cars he fields are two different cars");
  Check(RivalGrid.Pick(2,47f,0,speedOf).SequenceEqual(RivalGrid.Pick(2,47f,0,speedOf)),
   "The same player car gets the same grid on a retry, because ties break on the name");

  // ---- The player's own model is never on the grid. This is the whole point.
  foreach(var mine in RivalGrid.Roster)
  {
   int hash=new Model(mine).Hash;
   var grid=RivalGrid.Pick(2,speeds[mine],hash,speedOf);
   Check(grid.Count==2&&!grid.Contains(mine),
    "Turning up in a "+mine+" is the one car KJ will not field against it");
  }
  var matched=RivalGrid.Pick(2,speeds["cheetah"],new Model("cheetah").Hash,speedOf);
  Check(matched.Contains("comet2")||matched.Contains("entityxf")||matched.Contains("coquette"),
   "Excluding his car still fields the next closest thing, so the exclusion costs no challenge");

  // ---- The boundaries.
  Check(RivalGrid.Pick(0,47f,0,speedOf).Count==0&&RivalGrid.Pick(-3,47f,0,speedOf).Count==0,"A grid of no cars is no cars, not a crash");
  Check(RivalGrid.Pick(99,47f,0,speedOf).Count==RivalGrid.Roster.Length,"He cannot field more cars than he owns");
  var everyOther=RivalGrid.Pick(99,47f,new Model(RivalGrid.Roster[0]).Hash,speedOf);
  Check(everyOther.Count==RivalGrid.Roster.Length-1,"and the excluded car is gone from the whole list, not just the front of it");
  Check(RivalGrid.FallbackSpeed>1f,"A model the engine will not report a speed for is assumed to do something rather than nothing");
 }
}

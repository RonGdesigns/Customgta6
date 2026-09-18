using System;
using Bloodlines.Core;
public static partial class RegressionTests
{
 /// <summary>
 /// The numbers that decide whether KJ's sprint is a contest. Ron reported the rivals were
 /// no challenge; their ability was already at the engine's maximum, so the fault was the
 /// commanded speed. These are the bounds of the replacement.
 /// </summary>
 static void RacePacingChecks()
 {
  // ---- The dead band. A race that is close gets no help in either direction.
  Check(Math.Abs(RacePacing.Correction(0f)-1f)<.0001f,"Level with the player, a rival simply drives");
  Check(Math.Abs(RacePacing.Correction(-RacePacing.AssistFrom+1f)-1f)<.0001f,"and just inside the assist band he is still on his own");
  Check(Math.Abs(RacePacing.Correction(RacePacing.LiftFrom-1f)-1f)<.0001f,"and just inside the lift band too");
  Check(!RacePacing.IsCorrecting(0f)&&RacePacing.IsCorrecting(-900f),"IsCorrecting says which of those two a log line is worth");

  // ---- Behind: helped, and capped. An assist nobody can see the edge of is cheating.
  float near=RacePacing.Correction(-(RacePacing.AssistFrom+50f));
  float far=RacePacing.Correction(-(RacePacing.AssistFrom+RacePacing.Band));
  Check(near>1f&&near<far,"A trailing rival is helped more the further back he is");
  Check(Math.Abs(far-RacePacing.AssistCeiling)<.0001f,"and the help reaches its ceiling at the end of the band");
  Check(Math.Abs(RacePacing.Correction(-100000f)-RacePacing.AssistCeiling)<.0001f,"A rival a hundred kilometers back is helped no harder than that");
  Check(RacePacing.AssistCeiling<=1.5f,"The ceiling is a nudge, not a teleport");

  // ---- Ahead: eased off, and floored, so the race stays winnable without being a walkover.
  float lead=RacePacing.Correction(RacePacing.LiftFrom+RacePacing.Band);
  Check(lead<1f&&Math.Abs(lead-RacePacing.LiftFloor)<.0001f,"A runaway leader eases back to the floor and no further");
  Check(Math.Abs(RacePacing.Correction(100000f)-RacePacing.LiftFloor)<.0001f,"however far ahead he gets");
  Check(RacePacing.LiftFloor>=.8f,"and the lift never hands the race over");
  Check(RacePacing.LiftFrom>RacePacing.AssistFrom,"Easing off starts later than helping, so a close race is left alone");

  // ---- The commanded speed comes from the car, not from a constant.
  float quick=RacePacing.Speed(80f,false,0f), slow=RacePacing.Speed(40f,false,0f);
  Check(quick>slow,"A faster car is told to go faster: the speed is the car's own capability");
  Check(Math.Abs(quick-80f*RacePacing.RoadFraction)<.01f,"On asphalt that is nearly all of it");
  Check(RacePacing.Speed(80f,true,0f)<RacePacing.Speed(80f,false,0f)*.6f,"On the mountain trail it is far less, because the surface is dirt and the bends are blind");
  Check(RacePacing.Speed(80f,false,-900f)>quick&&RacePacing.Speed(80f,false,900f)<quick,"The correction moves the commanded speed both ways");
  Check(RacePacing.Speed(0f,false,0f)>=RacePacing.FloorSpeed&&RacePacing.Speed(float.NaN,false,0f)>=RacePacing.FloorSpeed,
   "A capability the engine will not report still yields a speed worth driving at, rather than a stop");
  Check(RacePacing.Speed(80f,true,100000f)>=RacePacing.FloorSpeed,"and the lift can never command a standstill");
  Check(Math.Abs(RacePacing.Correction(float.NaN)-1f)<.0001f&&Math.Abs(RacePacing.Correction(float.PositiveInfinity)-1f)<.0001f,
   "A progress figure that is not a number is no correction at all");

  // ---- The old flat order is what this replaced, and it must not come back.
  Check(RacePacing.Speed(80f,false,0f)>39f,"The commanded road speed beats the flat 39 m/s that made the rivals harmless");
 }
}

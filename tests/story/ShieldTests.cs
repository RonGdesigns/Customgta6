using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
 static void ShieldChecks()
 {
  // The roster is a stand-in in this suite; the shield's contract is pinned at the source.
  string roster=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Crew","CrewRoster.cs"));
  string config=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Core","ModConfig.cs"));
  string ini=File.ReadAllText(Path.Combine(Repo,"config","Bloodlines.ini"));
  Check(config.Contains("CompanionsInvincible")&&config.Contains("\"Crew\", \"CompanionsInvincible\"")&&ini.Contains("CompanionsInvincible = True"),"The shield is a documented crew setting, on by default");
  Check(roster.Contains("if (_config.CompanionsInvincible && ped.Exists())")&&roster.Contains("if (!ped.IsInvincible) ped.IsInvincible = true;"),"Inactive brothers are made invincible every tick");
  Check(roster.Contains("_shielded.Remove(ped.Handle)) ped.IsInvincible = false;"),"The brother the player takes is mortal again the moment he is active");
  int shield=roster.IndexOf("_shielded.Add(ped.Handle)");int floor=roster.IndexOf("ped.Health = _config.CompanionHealthFloor");
  Check(shield>0&&floor>shield,"The shield is asserted before the health floor, so the floor remains the fallback when the shield is off");

  // M01: Mateo is untouchable and the escape phase exists for the scene.
  string m01=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M01GhostInTheDockyard.cs"));
  Check(m01.Contains("_mateo.IsInvincible = true;")&&!m01.Contains("route to the launch was blocked"),"Mateo cannot die and the mission no longer fails on his route");
  Check(m01.Contains("Phase = \"escape\"")&&m01.Contains(".With(\"MATEO\", _mateo)"),"The escape is a specified scene with Mateo as its speaker");
  var scenes=File.ReadAllLines(Path.Combine(dataDir,"scenes.tsv")).Where(l=>l.StartsWith("M01_SCENE_ESCAPE_")).ToList();
  Check(scenes.Count==2&&scenes[0].Contains("\tMATEO\t")&&scenes[1].Contains("\tICE\t"),"The authored escape lines are generated: Mateo first, then Ice");
 }
}

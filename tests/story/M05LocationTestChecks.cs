using System;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Native;

partial class StoryTests
{
 static void M05LocationTestChecks()
 {
  Reset(); Function.Seabed=-3f;
  var surface=new OutputArgument();
  Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD,0f,0f,30f,surface,true,false);
  Check(surface.GetResult<float>()==0f,"The depth test double honors includeWater: it can return the surface as ground");
  Check(MissionSites.DeepEnough(0,0,0),"The real depth probe excludes water and accepts a three-meter seabed");
  Function.Seabed=1f;Check(!MissionSites.DeepEnough(0,0,0),"The corrected depth probe still rejects land above the water");
  foreach(var issue in new[]{"shallow","missing water","ground"})
  {
   Reset();var crew=Roster();var c=Context(crew);c.Config.M05LocationTestMode=true;
   var saved=c.Locations.All.Where(l=>l.Key.StartsWith("M05.")).ToDictionary(l=>l.Key,l=>l.Position);
   if(issue=="shallow")Function.Seabed=1f;
   if(issue=="missing water")World.WaterAvailable=false;
   if(issue=="ground")World.FailNavigation=true;
   var mission=new M05TidalLock();
   Check(mission.Begin(c),"M05 location-test mode starts despite "+issue);
   Check(mission.UnverifiedLocations.Count==(issue=="ground"?2:3),"Every failed "+issue+" site is exposed for survey");
   Check(mission.Dinghy!=null&&mission.Dinghy.Exists()&&mission.Mateo!=null&&mission.Mateo.Exists(),"Location fallback still creates the actual boats and actors");
   foreach(var key in mission.UnverifiedLocations)
   {
    var actual=c.Locations.Position(key);var original=saved[key];
    Check(actual.X==original.X&&actual.Y==original.Y,"Unverified "+key+" remains at its configured map position");
   }
   c.Cutscenes.Skip();mission.Tick();
   Check(GameUtils.Message.Contains("M05 LOCATION TEST")&&GameUtils.Message.Contains(mission.UnverifiedLocations[0]),"The warning survives the opening cutscene and identifies the failed spots");
   mission.Abort();
   Check(saved.All(pair=>c.Locations.Position(pair.Key)==pair.Value),"Abort restores configured locations for the next attempt");
  }
  Reset();var regular=Context(Roster());Function.Seabed=1f;
  Check(!new M05TidalLock().Begin(regular),"Ordinary M05 retains location checks when test mode is off");
  Reset();var missing=Context(Roster());missing.Config.M05LocationTestMode=true;World.FailVehicles=true;
  Check(!new M05TidalLock().Begin(missing)&&GameUtils.Message.Contains("essential boat or actor"),"Test mode reports missing essential assets instead of claiming a playable mission");
 }
}

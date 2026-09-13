using System;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;

public static partial class StoryTests
{
 static M19UnderwaterBreach BeginClampTest(out MissionContext c)
 {
  var crew=Roster();c=Context(crew);var m=new M19UnderwaterBreach();
  Check(m.Begin(c),"M19 ballast repair starts with available assets");
  c.Cutscenes.Skip();m.Tick();
  Interact(m,c,CrewSlot.Gohan,m.Breach,16,afloat:true);
  Check(m.CurrentStage==2,"M19 reaches its actual clamp objective");
  return m;
 }

 static void M19FloatRepairChecks()
 {
  Reset();var crew=Roster();var c=Context(crew);GameUtils.FailModel=M19UnderwaterBreach.FloatModel;
  var unavailable=new M19UnderwaterBreach();
  Check(!unavailable.Begin(c)&&World.Vehicles.Count==0&&!c.Cutscenes.IsActive,
   "Unavailable ballast is caught before deploying the operation");

  foreach(bool skip in new[]{false,true})
  {
   Reset();var m=BeginClampTest(out c);
   foreach(var point in m.Clamps)Interact(m,c,CrewSlot.Gohan,point,8,afloat:true);
   m.Tick();m.Tick();
   Check(m.Floats.Count==2&&m.Floats.All(f=>f.Exists()&&f.Model.Name=="prop_dock_bouy_3"),
    "Both clamps create the locally verified model; the unavailable old model cannot pass");
   FinishSoloScene(c,skip);m.Tick();
   Check(m.Floated&&m.Status==MissionStatus.Running&&m.CurrentStage==3&&
    m.Floats.All(f=>f.AttachedTo==m.Container)&&Game.Player.CanControlCharacter,
    "Watching or skipping the float scene secures both floats and restores sub control");
   m.Abort();
  }

  foreach(bool missingModel in new[]{false,true})
  {
   Reset();var m=BeginClampTest(out c);
   if(missingModel)GameUtils.FailModel=M19UnderwaterBreach.FloatModel;
   else World.FailPropModel=M19UnderwaterBreach.FloatModel;
   var stage=((System.Collections.Generic.List<MissionStage>)typeof(ComposedMission)
    .GetField("_stages",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(m))[2];
   var clamps=stage.Objectives.OfType<MultiHoldObjective>().Single();
   Interact(m,c,CrewSlot.Gohan,m.Clamps[0],8,afloat:true);
   Check(m.Status==MissionStatus.Failed&&clamps.Remaining==2&&!m.Floated&&!c.Cutscenes.IsActive,
    "A failed float load or allocation cannot credit the clamp or start the lift scene");
  }
  Reset();
 }
}

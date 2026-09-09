using System;
using System.IO;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Native;
public static partial class StoryTests
{
 static void PersonalChecks()
 {
  Reset(); var crew=Roster();var old=crew.PedFor(CrewSlot.Ice);var next=crew.PedFor(CrewSlot.Gohan);
  old.Health=600;old.Armor=95;next.Health=330;next.Armor=12;Function.ResetVitalsOnSwitch=true;
  var switching=new SwitchController(crew);
  Check(switching.TrySwitch(CrewSlot.Gohan)&&old.Health==600&&old.Armor==95&&next.Health==330&&next.Armor==12,"Native handover preserves BOTH heroes' distinct health and armor");
  Game.GameTime+=500;next.Health=260;
  Check(switching.TrySwitch(CrewSlot.Ice)&&old.Health==600&&next.Health==260&&old.MaxHealth==900&&next.MaxHealth==900,"Switching back keeps real damage on the injured hero without transferring it");Function.ResetVitalsOnSwitch=false;
  Reset();var ped=Game.Player.Character;var path=Path.Combine(root,"wardrobe.ini");CrewAppearance.Load(path);
  CrewAppearance.AdjustClothing(ped,CrewSlot.Guess,"Shirt",-1,false);
  CrewAppearance.AdjustClothing(ped,CrewSlot.Guess,"Shirt",-1,true);
  Check(CrewAppearance.Drawable(CrewSlot.Guess,"Shirt")==19&&CrewAppearance.Texture(CrewSlot.Guess,"Shirt")==11,"Clothing wraps within drawable and texture counts");
  CrewAppearance.AdjustClothing(ped,CrewSlot.Guess,"Shirt",1,false);
  Check(CrewAppearance.Drawable(CrewSlot.Guess,"Shirt")==0&&CrewAppearance.Texture(CrewSlot.Guess,"Shirt")==0,"Changing an item resets its incompatible texture");
  CrewAppearance.AdjustClothing(ped,CrewSlot.Guess,"Hat",-1,false);
  Check(CrewAppearance.Drawable(CrewSlot.Guess,"Hat")==2,"Props wrap from none to the final supported item");
  CrewAppearance.AdjustClothing(ped,CrewSlot.Guess,"Hat",1,false);
  Check(CrewAppearance.Drawable(CrewSlot.Guess,"Hat")==-1,"Hat can be removed again");
  CrewAppearance.Adjust(ped,CrewSlot.Guess,"Beard",-1);CrewAppearance.Adjust(ped,CrewSlot.Guess,"BeardColor",-1);
  CrewAppearance.Save();CrewAppearance.Load(path);
  Check(CrewAppearance.For(CrewSlot.Guess).Beard==28&&CrewAppearance.For(CrewSlot.Guess).BeardColor==63&&CrewAppearance.Drawable(CrewSlot.Guess,"Shirt")==0,"Facial hair and individual clothing survive save/reload");
  Game.GameTime+=130000;
  Check(!CrewAppearance.ChangeAfterAbsence(ped,CrewSlot.Guess,true),"Custom clothing disables automatic outfit replacement across distant switches");
  Check(CrewAppearance.Drawable(CrewSlot.Ice,"Shirt")==-1&&CrewAppearance.For(CrewSlot.Ice).Beard==-1,"Editing Guess does not change Ice's clothing or facial hair");
 }
}

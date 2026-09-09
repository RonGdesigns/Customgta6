using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;
using GTA.Native;
public static partial class StoryTests
{
 static void ApartmentWeaponChecks()
 {
  Reset();var crew=Roster();var access=new ApartmentAccess(crew);var player=Game.Player.Character;
  var before=player.Position;var inside=new Vector3(347,-999,-99);Function.InteriorId=123;Function.InteriorReady=false;
  Check(access.Begin(inside,null,true)&&access.Busy&&!Game.Player.CanControlCharacter,"Apartment entry begins with bounded loading and held controls");
  Game.GameTime+=300;access.Update();Check(player.Position==before&&!access.Inside,"Unready interior does not move the player into an unloaded room");
  Game.GameTime+=7100;access.Update();Check(!access.Busy&&!access.Inside&&Game.Player.CanControlCharacter&&!player.IsPositionFrozen&&!GameUtils.Faded&&crew.CompanionAI.Controlled.Count==0,"Apartment timeout restores position, input, fade and companion ownership");
  Function.InteriorReady=true;World.CollisionReady=false;access.Begin(inside,null,true);Game.GameTime+=300;access.Update();access.Update();
  Check(access.Busy&&player.IsPositionFrozen,"Player stays held until apartment collision loads");
  World.CollisionReady=true;access.Update();Check(access.Inside&&!access.Busy&&player.Position==inside&&!player.IsPositionFrozen,"Ready interior releases the living player into the apartment");
  Check(!new SwitchController(crew){ExternalBlockReason=()=>access.Inside?"Exit first":null}.TrySwitch(CrewSlot.Gohan),"Switching cannot leave an uncontrolled hero stranded in an apartment");
  access.Begin(access.ExitPosition,null,false);Game.GameTime+=300;access.Update();access.Update();
  Check(!access.Inside&&player.Position==before&&crew.CompanionAI.Controlled.Count==0&&Game.Player.CanControlCharacter,"Apartment exit returns to the exact exterior and resumes companions");
  access.Begin(inside,null,true);Function.ThrowOnce=Hash.REQUEST_COLLISION_AT_COORD;bool failed=false;try{access.Update();}catch{failed=true;}
  Check(failed&&!access.Busy&&!player.IsPositionFrozen&&Game.Player.CanControlCharacter,"Apartment native failure releases controls before propagating");
  var c=Context(crew);var state=CampaignState.Load(Path.Combine(root,"apartments.json"));var locker=new WeaponProgression(state);var homes=new CrewHomes(crew,state,c.Locations,locker);
  var starter=homes.Position(CrewSlot.Ice);Check(!homes.LuxuryUnlocked&&starter.HasValue,"Furnished starter apartment is available before luxury progression");
  state.Completed.Add("M27");Check(homes.LuxuryUnlocked&&homes.Position(CrewSlot.Ice)!=starter,"M27 upgrades the home route to the luxury building");
  Check(WeaponProgression.DlcCatalog.Length==28&&WeaponProgression.DlcCatalog.Select(w=>w.Hash).Distinct().Count()==28,"DLC locker lists 28 distinct additional weapons");
  var weapon=WeaponProgression.DlcCatalog.First(w=>w.Key=="WEAPON_BATTLERIFLE");
  Check(locker.GiveDlc(CrewSlot.Ice,player,weapon)&&state.Weapons["Ice"].Contains(weapon.Hash),"A DLC weapon outside the pinned enum is granted and saved to its owner");
  var loaded=CampaignState.Load(Path.Combine(root,"apartments.json"));var replacement=new Ped();new WeaponProgression(loaded).Apply(CrewSlot.Ice,replacement);
  Check(replacement.Weapons.Owned.Contains((WeaponHash)weapon.Hash)&&!loaded.Weapons["Gohan"].Contains(weapon.Hash),"Dynamic DLC weapon hash survives reload without granting it to teammates");
 }
}

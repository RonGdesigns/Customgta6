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
 static void HomeAndVehicleChecks()
 {
  Reset();var crew=Roster();var state=CampaignState.Load(Path.Combine(root,"locker.json"));var c=Context(crew);var locker=new WeaponProgression(state);
  var ice=crew.Peds[CrewSlot.Ice];ice.Weapons.Give(WeaponHash.CombatPistol,37,false,true);locker.SaveCrew(crew);
  var loaded=CampaignState.Load(Path.Combine(root,"locker.json"));
  Check(loaded.Weapons["Ice"].Contains((uint)WeaponHash.CombatPistol)&&!loaded.Weapons["Gohan"].Contains((uint)WeaponHash.CombatPistol),"Acquired gun ownership saves to the correct character's locker");
  var fresh=new Ped();new WeaponProgression(loaded).Apply(CrewSlot.Ice,fresh);Check(fresh.Weapons.Owned.Contains(WeaponHash.CombatPistol),"Recreated hero receives their acquired weapon");
  state.Completed.Add("M03");Check(locker.UnlockRewards()&&state.Weapons["Gohan"].Contains((uint)WeaponHash.StunGun),"Mission completion expands each hero's role-specific locker");
  Check(!locker.UnlockRewards(),"Revisiting an unlocked reward does not duplicate it");
  var homes=new CrewHomes(crew,state,c.Locations,locker);Check(homes.Position(CrewSlot.Ice).HasValue&&homes.Position(CrewSlot.Gohan).HasValue&&homes.Position(CrewSlot.Guess).HasValue,"All three story homes have unlocked access points");
  Game.Player.Character.Position=homes.Position(crew.ActiveSlot).Value;Game.Player.WantedLevel=2;Game.Accept=true;homes.Update(true);
  Check(!Function.Values.ContainsKey(Hash.ADD_TO_CLOCK_TIME),"Home rest cannot erase an active police pursuit");
  Game.Player.WantedLevel=0;Game.Accept=true;Game.Player.Character.Health=150;homes.Update(true);
  Check(Game.Player.Character.Health==CrewDurability.Health&&Game.Player.CanControlCharacter&&!GameUtils.Faded&&state.LastLocation==Game.Player.Character.Position,"Rest saves the actual home location, restores health and releases controls");
  Game.GameTime+=10001;Game.Accept=true;Function.ThrowOnce=Hash.ADD_TO_CLOCK_TIME;bool failed=false;try{homes.Update(true);}catch{failed=true;}
  Check(failed&&Game.Player.CanControlCharacter&&!GameUtils.Faded,"A home-rest native failure cannot strand the player in a fade");homes.Clear();
  var garage=new StoryVehicles();World.Vehicles.Clear();Game.Player.Character.Position=new Vector3(100,100,10);Game.Player.Character.ForwardVector=new Vector3(0,1,0);
  for(int i=0;i<4;i++)Check(garage.Spawn(StoryVehicles.Catalog[i]),"Available DLC car parks without replacing the player's vehicle "+i);
  Check(!garage.Spawn(StoryVehicles.Catalog[4])&&World.Vehicles.Count==4,"DLC requests respect the four-vehicle limit");
  Game.Player.Character.SetIntoVehicle(World.Vehicles[0],VehicleSeat.Driver);garage.ReleaseParked();
  Check(!World.Vehicles[0].Released&&World.Vehicles.Skip(1).All(v=>v.Released),"Releasing parked DLC cars preserves occupied cars");garage.Clear();
  state.Reset();Check(CampaignState.Load(Path.Combine(root,"locker.json")).Weapons.Values.All(v=>v.Count==0),"Campaign reset also clears earned weapon progression");
 }
}

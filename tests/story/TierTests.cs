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
 static void TierChecks()
 {
  // ---- Three tiers, each an upgrade on the last; three different starter rooms.
  Reset();var crew=Roster();var c=Context(crew);var state=CampaignState.Load(Path.Combine(root,"tiers.json"));
  Check(ApartmentTiers.Current(state)==ApartmentTier.Starter&&ApartmentTiers.Next(ApartmentTier.Starter)==ApartmentTier.Luxury&&ApartmentTiers.Next(ApartmentTier.Luxury)==ApartmentTier.Top&&ApartmentTiers.Next(ApartmentTier.Top)==null,"The tiers run starter, luxury, top");
  var starters=Protagonist.All.Select(h=>ApartmentTiers.For(h.Slot,ApartmentTier.Starter)).ToList();
  Check(starters.Select(r=>r.InteriorKey).Distinct().Count()==3&&starters.All(r=>c.Locations.Get(r.InteriorKey)!=null&&c.Locations.Get(r.EntranceKey)!=null),"Each brother's starter room is a different interior with its own door");
  Check(starters.Select(r=>c.Locations.Position(r.InteriorKey)).Distinct().Count()==3,"The three starter interiors are three different places");
  var luxury=Protagonist.All.Select(h=>ApartmentTiers.For(h.Slot,ApartmentTier.Luxury)).ToList();
  Check(luxury.Select(r=>r.Ipl).Distinct().Count()==3&&luxury.All(r=>r.Probe.HasValue&&r.EntranceKey=="Apartment.Luxury.Entrance"),"The luxury tier is the three Eclipse penthouse floors behind one door");
  var top=ApartmentTiers.For(CrewSlot.Ice,ApartmentTier.Top);
  Check(top.Floors==2&&top.Ipl=="vw_casino_penthouse"&&top.EntitySets.Length>=10&&top.Slot==null&&c.Locations.Get(top.InteriorKey)!=null&&c.Locations.Get(top.EntranceKey)!=null,"The top tier is the two-floor Diamond penthouse, furnished by entity sets, shared by the crew");
  state.Completed.Add("M27");Check(ApartmentTiers.Current(state)==ApartmentTier.Luxury,"M27 opens the luxury tier");
  state.Completed.Add("M47");Check(ApartmentTiers.Current(state)==ApartmentTier.Top,"M47 opens the top tier");
  Check(ApartmentTiers.Progression(ApartmentTier.Starter).Contains("M27")&&ApartmentTiers.Progression(ApartmentTier.Luxury).Contains("M47")&&ApartmentTiers.Progression(ApartmentTier.Top).Contains("Top tier"),"The home menu says what the next step up is");

  // ---- The homes follow the tier.
  var homes=new CrewHomes(crew,state,c.Locations,new WeaponProgression(state));
  Check(homes.Tier==ApartmentTier.Top&&homes.LuxuryUnlocked&&homes.ResidenceName.Contains("Diamond")&&homes.Position(CrewSlot.Ice)==c.Locations.Position("Apartment.Top.Entrance"),"At the top tier every brother's home is the Diamond's front door");
  state.Completed.Remove("M47");Check(homes.Tier==ApartmentTier.Luxury&&homes.Position(CrewSlot.Gohan)==c.Locations.Position("Apartment.Luxury.Entrance"),"At the luxury tier the home is Eclipse Towers");
  state.Completed.Remove("M27");Check(homes.Tier==ApartmentTier.Starter&&homes.Position(CrewSlot.Guess)==c.Locations.Position("Apartment.Starter.Guess")&&homes.RoomSurveyKeys[0]==homes.Current.InteriorKey,"At the start each brother's home is his own door, and the room survey is his own room");

  // ---- Entity sets furnish the room and are put back on leaving.
  var access=new ApartmentAccess(crew);var player=Game.Player.Character;Function.InteriorId=9001;Function.InteriorReady=true;World.CollisionReady=true;Function.Calls.Clear();
  Check(access.Begin(new Vector3(976,70,115),"vw_casino_penthouse",true,null,0f,new[]{"Set_Pent_Tint_Shell","Set_Pent_Spa_Open"}),"Entry into the penthouse begins with its IPL and sets");
  Game.GameTime+=300;access.Update();access.Update();
  Check(access.Inside&&Function.Calls.Count(x=>x.Item1==Hash.ACTIVATE_INTERIOR_ENTITY_SET)==2&&Function.Calls.Any(x=>x.Item1==Hash.ACTIVATE_INTERIOR_ENTITY_SET&&(string)x.Item2[1]=="Set_Pent_Spa_Open"),"Both sets are activated on the pinned interior before the room is judged ready");
  Function.Calls.Clear();access.Cancel();
  Check(!access.Inside&&Function.Calls.Count(x=>x.Item1==Hash.DEACTIVATE_INTERIOR_ENTITY_SET)==2,"Leaving deactivates what it activated");
  Function.InteriorId=123;World.CollisionReady=false;
  string homesSrc=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Core","CrewHomes.cs"));
  Check(homesSrc.Contains("Apartment.Begin(location.Position, residence.Ipl, true, residence.Probe, location.Heading, residence.EntitySets)")&&!homesSrc.Contains("apa_v_mp_h_01_a"),"Entering an apartment goes through the residence: its interior, IPL, probe, heading and sets");
 }
}

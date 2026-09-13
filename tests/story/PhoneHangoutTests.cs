using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
public static partial class StoryTests
{
 static void PhoneHangoutChecks()
 {
  Reset();var crew=Roster();Use(crew,CrewSlot.Guess);var c=Context(crew);c.State=CampaignState.Load(System.IO.Path.Combine(root,"phone-hangout.json"));
  var catalog=new MissionCatalog();var manager=new MissionManager(c,c.State,catalog);
  var hub=new CampaignHub(c.State,crew,catalog,manager,null){CanCommand=()=>true};
  var rows=hub.Entries(CampaignPhone.App.Crew);
  Check(rows.Count(e=>e.Id.StartsWith("contact:"))==2&&rows.All(e=>e.Id!="contact:Guess"),"Phone Crew offers each inactive brother and excludes the current player");
  rows.Single(e=>e.Id=="contact:Ice").Children().Single(e=>e.Id=="hangout:Ice").Action();
  Check(crew.CompanionAI.IsHangingOut(CrewSlot.Ice)&&!crew.CompanionAI.IsHangingOut(CrewSlot.Gohan),"Crew invites one brother without sending a group order");
  var dismiss=hub.Entries(CampaignPhone.App.Crew).Single(e=>e.Id=="contact:Ice").Children().Single(e=>e.Id=="hangout:Ice");
  Check(dismiss.Title=="Let Ice head out"&&dismiss.Body.Contains("safe stop"),"Invited contact changes to a clearly explained end-hangout action");
  crew.CompanionAI.MissionActive=true;dismiss.Action();
  Check(crew.CompanionAI.IsHangingOut(CrewSlot.Ice),"An action opened before a mission cannot dismiss the mission actor later");
  crew.CompanionAI.MissionActive=false;dismiss.Action();
  Check(!crew.CompanionAI.IsHangingOut(CrewSlot.Ice),"End hangout returns the selected brother to independent mode");
  Use(crew,CrewSlot.Gohan);rows=hub.Entries(CampaignPhone.App.Crew);
  Check(rows.Any(e=>e.Id=="contact:Guess")&&rows.All(e=>e.Id!="contact:Gohan"),"Switching protagonists updates the two available hangout contacts");
  Check(hub.Entries(CampaignPhone.App.Settings).All(e=>!e.Id.StartsWith("hangout:")),"Phone Settings contains preferences, while all individual invites live in Crew");
  hub.Entries(CampaignPhone.App.Crew).Single(e=>e.Id=="contact:Ice").Children().Single(e=>e.Id=="travel:Ice:True").Action();
  hub.Entries(CampaignPhone.App.Crew).Single(e=>e.Id=="contact:Guess").Children().Single(e=>e.Id=="travel:Guess:False").Action();
  Check(crew.CompanionAI.RidesAlong(CrewSlot.Ice)&&!crew.CompanionAI.RidesAlong(CrewSlot.Guess),"Each crew contact configures its own ride-along or drive-along invitation");
  var phone=new CampaignPhone(c.State,new CampaignDispatches(c.State),()=>"Job",()=>"GPS",slot=>"Ready","F6"){Hub=hub};
  phone.Open(CrewSlot.Gohan);PhoneApp(phone,CampaignPhone.App.Crew);PhonePreview(phone,"individual-hangouts");phone.Select();PhonePreview(phone,"individual-travel-options");PhoneApp(phone,CampaignPhone.App.Settings);phone.Select();phone.Select();
  Check(phone.Page==CampaignPhone.App.Home&&phone.IsArranging,"Settings starts icon arrangement with A, leaving controller X for gameplay");phone.Shutdown();
 }
}

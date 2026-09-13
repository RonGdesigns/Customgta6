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
 static void HubChecks()
 {
  Reset();var crew=Roster();Use(crew,CrewSlot.Guess);var c=Context(crew);
  string path=Path.Combine(root,"hub.json");var state=c.State=CampaignState.Load(path);state.CashOnHand=50000;
  var catalog=new MissionCatalog();foreach(var info in c.Data.Missions.Concat(c.Data.SoloMissions).GroupBy(m=>m.Id).Select(g=>g.First()))catalog.All.Add(new MissionDefinition{Info=info,Factory=()=>new ProbeMission()});
  var manager=new MissionManager(c,state,catalog);var garage=new GarageService(crew,state,c.Locations,null){Allowed=()=>true};
  bool allowed=true;int routes=0;
  var hub=new CampaignHub(state,crew,catalog,manager,garage){CanCommand=()=>allowed,RouteMission=m=>{routes++;return "Routed";}};
  garage.OnActivity=(title,body)=>hub.Log("Garage / KJ",title,body);
  var phone=new CampaignPhone(state,new CampaignDispatches(state),()=>"Objective",()=>"Route",slot=>"Ready","F6"){Hub=hub};
  try
  {
   Check(hub.Entries(CampaignPhone.App.Garage).Any(e=>e.Id=="empty"),"Empty garage app explains how to store a car");
   Check(hub.Entries(CampaignPhone.App.Garage).Any(e=>e.Id=="crew-car"&&e.Action==null),"Crew fleet is visible but cannot be duplicated through personal deliveries");
   var saved=new OwnedVehicle{Id=1,ModelName="sultanrs",ModelHash=(uint)Game.GenerateHash("sultanrs"),Label="Sultan RS",Garage="bay-guess",Price=10000,InShop=true};state.Vehicles.Add(saved);
   var entry=hub.Entries(CampaignPhone.App.Garage).Single(e=>e.Id=="car:1");
   Check(entry.Body.Contains("$1,000")&&entry.Quote!=null,"KJ quote exposes the repair charge before ordering");
   phone.Open(CrewSlot.Guess);PhoneApp(phone,CampaignPhone.App.Garage);phone.Select();
   int cash=state.CashOnHand;phone.Select();Check(phone.Body().StartsWith("CONFIRM REQUEST")&&state.CashOnHand==cash&&!garage.DeliveryActive,"First delivery press opens confirmation without spending money or spawning a car");
   phone.Back();Check(!phone.Body().StartsWith("CONFIRM REQUEST")&&state.CashOnHand==cash,"B cancels a paid delivery confirmation");
   phone.Select();saved.Price=20000;phone.Select();phone.FinishFrame(true);
   Check(phone.Notice.Contains("quote changed")&&state.CashOnHand==cash&&!garage.DeliveryActive,"A changed repair price invalidates the original confirmation");
   saved.Price=10000;phone.FinishFrame(true);phone.Select();phone.Select();allowed=false;phone.FinishFrame(true);
   Check(state.CashOnHand==cash&&!garage.DeliveryActive,"Transition or mission permission loss cancels an already queued order");
   allowed=true;phone.Select();phone.Select();phone.FinishFrame(false);Check(!phone.IsOpen&&!garage.DeliveryActive,"Phone closure discards queued garage work");
   phone.Shutdown();phone.Open(CrewSlot.Guess);PhoneApp(phone,CampaignPhone.App.Garage);phone.Select();phone.Select();phone.Select();phone.Back();phone.FinishFrame(true);
   Check(!garage.DeliveryActive&&state.CashOnHand==cash,"Backing out before execution clears a pending delivery action");
   phone.Shutdown();Game.Player.Character.Position=new Vector3(1000,500,30);Game.Player.Character.ForwardVector=new Vector3(0,1,0);
   Check(hub.Entries(CampaignPhone.App.Garage).Single(e=>e.Id=="car:1").Action().Contains("accepted")&&garage.DeliveryActive&&state.CashOnHand==cash-1000,"Valid KJ order reuses the real garage service and charges the repair once");
   Check(state.PhoneHistory.Any(n=>n.Title=="Delivery accepted"),"Real KJ acceptance creates a persistent delivery alert");
   int paid=state.CashOnHand;hub.Entries(CampaignPhone.App.Garage).Single(e=>e.Id=="car:1").Action();
   Check(state.CashOnHand==paid,"Repeated request cannot double-charge the same active delivery");
   Check(hub.Entries(CampaignPhone.App.Garage).Any(e=>e.Id=="track")&&hub.Entries(CampaignPhone.App.Garage).Single(e=>e.Id=="track").Action().Contains("marked"),"Garage app can track the live delivery vehicle");
   var drop=garage.DeliveryVehicle;drop.Position=Game.Player.Character.Position;drop.Speed=0;garage.Update(false);
   Check(!garage.DeliveryActive&&garage.IsOut(saved)&&state.PhoneHistory.Any(n=>n.Title=="Vehicle ready for pickup"),"KJ delivery progresses while service menus are closed and records actual handover");
   Check(hub.Entries(CampaignPhone.App.Garage).Single(e=>e.Id=="car:1").Button=="Locate vehicle","Delivered car becomes a locate action instead of another delivery order");
   garage.RecallDelivery();garage.Clear();

   foreach(var choice in hub.Entries(CampaignPhone.App.Commands))
   {
    allowed=false;bool before=crew.CompanionAI.IndependentFreeRoam;int refresh=crew.CompanionAI.Refreshes;choice.Action();
    Check(crew.CompanionAI.IndependentFreeRoam==before&&crew.CompanionAI.Refreshes==refresh,"Blocked crew command does not replace mission AI: "+choice.Title);
   }
   allowed=true;crew.CompanionAI.MissionActive=true;hub.Entries(CampaignPhone.App.Commands)[0].Action();
   Check(crew.CompanionAI.IndependentFreeRoam,"Mission ownership blocks orders even if an external permission callback permits them");crew.CompanionAI.MissionActive=false;
   hub.Entries(CampaignPhone.App.Commands)[1].Action();Check(!crew.CompanionAI.IndependentFreeRoam&&crew.CompanionAI.RideAlong,"Travel together requests shared transport through existing AI");
   hub.Entries(CampaignPhone.App.Commands)[2].Action();Check(!crew.CompanionAI.IndependentFreeRoam&&!crew.CompanionAI.RideAlong,"Drive alongside selects the existing convoy behavior");
   hub.Entries(CampaignPhone.App.Commands)[3].Action();Check(crew.CompanionAI.IndependentFreeRoam,"Independent order restores off-duty life");

   hub.Update(false);int history=state.PhoneHistory.Count;state.Completed.Add("M29");hub.Update(false);
   Check(state.PhoneHistory.Count==history+2&&state.PhoneHistory.Any(n=>n.Title=="Enough to stay"),"New completed job records journal update and its authored crew message");
   hub.Update(false);Check(state.PhoneHistory.Count==history+2,"Observing a completed job again does not duplicate its alerts");
   var alert=hub.Entries(CampaignPhone.App.Alerts).First(e=>e.Id!="clear-alerts");int unread=hub.Unread;alert.Read();Check(hub.Unread==unread-1,"Opening an alert marks exactly that message read");
   state.Save();var reloaded=CampaignState.Load(path);Check(reloaded.PhoneHistory.Count==state.PhoneHistory.Count&&reloaded.PhoneHistory.Count(n=>n.Read)==state.PhoneHistory.Count(n=>n.Read),"Notification content and read state survive a save reload");
   var restartHub=new CampaignHub(reloaded,crew,catalog,manager,garage);int restarted=reloaded.PhoneHistory.Count;restartHub.Update(false);Check(reloaded.PhoneHistory.Count==restarted,"Loading completed missions never floods history with duplicate notifications");
   for(int i=0;i<65;i++)hub.Log("Shop","Receipt "+i,"$100 paid.");
   Check(state.PhoneHistory.Count==60&&state.PhoneHistory.Last().Title=="Receipt 64","Notification storage stays bounded and preserves newest transactions");
   var last=hub.Entries(CampaignPhone.App.Alerts).First(e=>e.Id!="clear-alerts");last.Read();GameUtils.Message="unchanged";Game.GameTime+=11000;hub.Update(true);Check(GameUtils.Message=="unchanged","An already read alert does not produce a delayed toast");

   state.Completed.Add("M12");state.Evidence["hullSurvey"]=EvidenceState.CopyHeld.ToString();
   var board=hub.Planning();Check(board.Single(e=>e.Id=="plan:Hull survey").Subtitle=="Recorded","Planning board reads the completed survey and actual evidence together");
   state.Evidence.Remove("hullSurvey");Check(hub.Planning().Single(e=>e.Id=="plan:Hull survey").Subtitle.StartsWith("Missing"),"Completing a job alone cannot fabricate missing preparation evidence");
   int routed=routes;hub.Planning().Single(e=>e.Id=="plan:Kraken").Action();Check(routes==routed,"Planning board routing respects unmet supplier prerequisites");
   state.Completed.Add("M22");state.Completed.Add("M42");state.FleetUpgrades["subAirdropReady"]=true;
   Check(hub.Planning().Single(e=>e.Id=="plan:Delivered offshore sub").Subtitle.StartsWith("Missing"),"A submarine unlock flag alone cannot replace the cargo record");
   state.Cargo["offshoreSub"]="M42.Delivery";Check(hub.Planning().Single(e=>e.Id=="plan:Delivered offshore sub").Subtitle=="Recorded","Offshore board recognizes a delivered submarine with its saved cargo location");
   state.BeginAttempt("M42");Check(hub.Planning().Single(e=>e.Id=="plan:Delivered offshore sub").Subtitle.Contains("Verify after"),"Preparation shown during a replay is labelled provisional until the attempt ends");state.DiscardAttempt();
   Check(hub.Journal().Any(e=>e.Id=="M29")&&!hub.Journal().Any(e=>e.Id=="M43"),"Journal shows completed jobs without revealing future mission synopses");
   Check(hub.Journal().Single(e=>e.Id=="M42").Body.Contains("ROLES"),"Completed mission journal retains each brother's authored role context");
   Check(hub.Progression().Any(e=>e.Id=="reward:M02"&&e.Body.Contains("Requires M02")),"Progression shows real upcoming weapon rewards and their source missions");
   Check(hub.Progression().Single(e=>e.Id=="homes").Body.Contains("M27"),"Housing progression uses the existing tier unlock requirement");
   Check(hub.Progression().Single(e=>e.Id=="upgrade:grangerTurbineInstalled").Subtitle.Contains("M11"),"Workshop roadmap uses actual campaign entitlement keys and milestone requirements");

   var shop=ShopService.Sites.First(s=>s.Kind==ShopKind.Customs);var car=new Vehicle{Position=shop.Position};Game.Player.Character.Position=shop.Position;Game.Player.Character.SetIntoVehicle(car,VehicleSeat.Driver);state.CashOnHand=10000;
   var service=new ShopService(crew,state,new WeaponProgression(state),new CrewMemory(state)){Allowed=()=>true};int receipts=0;
   service.Purchased=(name,cost)=>{receipts++;hub.Log(name,"Purchase receipt","$"+cost+" paid.");};
   service.BeginVehiclePreview(shop,()=>service.Fit(shop,VehicleModType.Spoilers,1));Check(receipts==0,"Previewing a cosmetic part creates no receipt");service.CancelVehiclePreview();
   service.BeginVehiclePreview(shop,()=>service.Fit(shop,VehicleModType.Spoilers,1));Check(service.ConfirmVehiclePreview()&&receipts==1,"Confirmed shop purchase records one receipt after payment succeeds");
   service.ConfirmVehiclePreview();Check(receipts==1,"Duplicate preview confirmation produces no duplicate receipt");

   phone.Open(CrewSlot.Guess);PhoneApp(phone,CampaignPhone.App.Garage);phone.Back();PhonePreview(phone,"hub-apps");
   PhoneApp(phone,CampaignPhone.App.Garage);PhonePreview(phone,"hub-garage");
   PhoneApp(phone,CampaignPhone.App.Planning);PhonePreview(phone,"hub-plan");
   PhoneApp(phone,CampaignPhone.App.Progression);PhonePreview(phone,"hub-progression");
   hub.Update(false);PhoneApp(phone,CampaignPhone.App.Alerts);PhonePreview(phone,"hub-alerts");
   int beforeClear=state.PhoneHistory.Count;phone.Select();phone.Select();phone.Back();
   Check(state.PhoneHistory.Count==beforeClear,"Cancelling Clear all preserves every saved alert");
   phone.Select();phone.Select();hub.Log("Crew","New arrival","Keep this until reviewed");phone.FinishFrame(true);
   Check(state.PhoneHistory.Any(n=>n.Title=="New arrival")&&phone.Notice.Contains("quote changed"),"An alert arriving during confirmation prevents clearing unseen messages");
   phone.Select();phone.Select();phone.FinishFrame(true);
   Check(state.PhoneHistory.Count==0&&hub.Unread==0&&phone.Notice=="All alerts cleared.","Confirmed Clear all removes read and unread alerts and resets the badge");
   hub.Update(false);Check(state.PhoneHistory.Count==0,"Completed-job alerts do not reappear immediately after clearing");
   Check(CampaignState.Load(path).PhoneHistory.Count==0,"Cleared alerts stay cleared after a save reload");
   PhonePreview(phone,"cleared-alerts");
   state.Reset();Check(state.PhoneHistory.Count==0,"A deliberate campaign reset clears phone history too");hub.Update(false);
  }
  finally {phone.Shutdown();garage.Clear();manager.Shutdown();Game.Pressed.Clear();Game.Disabled.Clear();}
 }
}

using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;

public static partial class StoryTests
{
 static void PhoneCommerceChecks()
 {
  Reset();var crew=Roster();Use(crew,CrewSlot.Guess);var c=Context(crew);
  var state=c.State=CampaignState.Load(Path.Combine(root,"phone-commerce.json"));state.CashOnHand=5000000;
  var catalog=new MissionCatalog();var manager=new MissionManager(c,state,catalog);
  var garage=new GarageService(crew,state,c.Locations,null){Allowed=()=>true};
  var hub=new CampaignHub(state,crew,catalog,manager,garage){CanCommand=()=>true};
  garage.OnActivity=(title,body)=>hub.Log("Garage",title,body);
  var phone=new CampaignPhone(state,new CampaignDispatches(state),()=>"Job",()=>"GPS",slot=>"Ready","F6"){Hub=hub};
  try
  {
   phone.Open(CrewSlot.Guess);PhoneApp(phone,CampaignPhone.App.Vehicles);
   phone.Select();phone.Select();phone.Select();
   Check(phone.Body().Contains("Store at:")&&phone.Body().Contains("Crew funds:"),"Phone vehicle checkout shows the exact storage destination and crew funds");
   int cash=state.CashOnHand;phone.Select();Check(phone.Body().StartsWith("CONFIRM REQUEST")&&state.CashOnHand==cash,"Browsing and reviewing a vehicle never charges before confirmation");
   phone.Select();phone.FinishFrame(true);
   Check(state.Vehicles.Count==1&&state.CashOnHand==cash-state.Vehicles[0].Price&&!garage.IsOut(state.Vehicles[0]),"Confirmed phone purchase charges once and stores one owned car instead of spawning it beside the player");
   phone.FinishFrame(true);Check(state.Vehicles.Count==1,"Drawing later frames cannot repeat the vehicle order");
   Check(state.PhoneHistory.Any(n=>n.Title=="Vehicle purchased"),"Phone purchases use the existing garage receipt history");
   PhonePreview(phone,"vehicle-checkout");
   phone.Back();phone.Back();phone.Back();phone.Back();Check(phone.Page==CampaignPhone.App.Home&&phone.Selection==phone.HomeIndexOf(CampaignPhone.App.Vehicles),"Back navigation unwinds checkout, destinations and categories to the correctly placed home icon");

   var property=GarageService.Sites.First(site=>site.Price>0&&!garage.Owned(site));
   var listing=hub.Entries(CampaignPhone.App.Properties).Single(e=>e.Id=="property:"+property.Id);
   Check(listing.Quote!=null&&listing.Body.Contains("does not include a residential interior"),"Property offer distinguishes garage storage from story-earned homes");
   cash=state.CashOnHand;listing.Action();Check(garage.Owned(property)&&state.CashOnHand==cash-property.Price,"Property purchase uses existing ownership and exact listed price");
   int after=state.CashOnHand;listing.Action();Check(state.CashOnHand==after,"An old property action cannot charge twice after ownership changes");
   Check(hub.Entries(CampaignPhone.App.Properties).Single(e=>e.Id==listing.Id).Quote==null,"An owned property changes from paid checkout to a map-location action");
   Check(hub.Entries(CampaignPhone.App.Properties).Where(e=>e.Id.StartsWith("home:")).All(e=>e.Action==null),"Property browsing cannot buy past the campaign's housing gates");
   PhoneApp(phone,CampaignPhone.App.Properties);PhonePreview(phone,"property-browser");

   var categories=hub.Entries(CampaignPhone.App.Vehicles);var choiceRow=categories.First(e=>e.Id.StartsWith("category:")).Children().First();
   var destination=choiceRow.Children().First();state.CashOnHand=0;int count=state.Vehicles.Count;destination.Action();
   Check(state.Vehicles.Count==count&&state.CashOnHand==0,"Insufficient funds cannot create a free phone vehicle");state.CashOnHand=after;
   var firstSite=GarageService.Site(state.Vehicles[0].Garage);
   while(garage.Used(firstSite)<firstSite.Capacity)state.Vehicles.Add(new OwnedVehicle{Id=state.NextVehicleId++,Garage=firstSite.Id,Label="Filler",ModelName="sultanrs"});
   count=state.Vehicles.Count;cash=state.CashOnHand;choiceRow.Children().First().Action();
   Check(state.Vehicles.Count==count&&state.CashOnHand==cash,"A full destination refuses purchase without spending crew funds");

   var car=state.Vehicles[0];var live=garage.Retrieve(car);Check(live!=null,"Recovery fixture takes an actual owned car out of storage");
   live.Mods[VehicleModType.Engine].Index=2;cash=state.CashOnHand;
   Game.Player.Character.SetIntoVehicle(live,VehicleSeat.Driver);
   Check(!garage.Recover(car,garage.RecoveryPrice(car))&&live.Exists()&&state.CashOnHand==cash,"Recovery refuses a vehicle while its driver remains inside");
   Game.Player.Character.Task.LeaveVehicle();int fee=garage.RecoveryPrice(car);
   Check(!garage.Recover(car,fee+1)&&state.CashOnHand==cash,"Recovery validates the agreed fee at execution");
   phone.Close();phone.Open(CrewSlot.Guess);PhoneApp(phone,CampaignPhone.App.Garage);phone.Select();
   Func<string> screen=()=>{var text=new System.Collections.Generic.List<string>();phone.Render((x,y,w,h,color)=>{},(value,x,y,size,color)=>text.Add(value),"12:00");return string.Join(" ",text);};
   Check(phone.IsOpen&&screen().Contains("Locate vehicle")&&screen().Contains("Recover vehicle"),"Opening an out car exposes both Locate and Recover in its own options");
   PhonePreview(phone,"out-car-options");phone.Move(MenuDirection.Down);phone.Select();phone.Select();
   Check(state.CashOnHand==cash&&phone.Body().StartsWith("CONFIRM REQUEST"),"Per-car recovery shows a price confirmation before charging");
   phone.Select();phone.FinishFrame(true);
   Check(state.CashOnHand==cash-500&&!live.Exists()&&!garage.IsOut(car)&&car.Mods[(int)VehicleModType.Engine]==2,"Unoccupied recovery costs $500, removes the old live car and preserves the actual fitted upgrade");
   Check(phone.Body().Contains("Recovered to your garage"),"The full transaction result lives in the scrollable panel instead of a clipped footer");
   phone.Back();Check(screen().Contains("Request KJ"),"Recovered car immediately offers KJ delivery in the same options folder");
   cash=state.CashOnHand;Check(!garage.Recover(car,fee)&&state.CashOnHand==cash,"Repeated recovery cannot charge for an already stored car");
   car.InShop=true;fee=garage.RecoveryPrice(car);Check(fee==Math.Max(500,car.Price/10),"Wreck recovery quotes the existing ten-percent repair scale");
   Check(garage.Recover(car,fee)&&!car.InShop&&state.CashOnHand==cash-fee,"Paid wreck recovery clears its repair flag without creating a second owned record");
   var restored=CampaignState.Load(Path.Combine(root,"phone-commerce.json"));Check(restored.Garages.Contains(property.Id)&&restored.Vehicles.Count==state.Vehicles.Count&&!restored.Vehicles[0].InShop,"Vehicle purchases, property ownership and recovery state survive a save reload");
  }
  finally{phone.Shutdown();}
 }
}

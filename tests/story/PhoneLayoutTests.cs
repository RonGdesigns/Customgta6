using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Native;

public static partial class StoryTests
{
 static void PhoneLayoutChecks()
 {
  var wide=CampaignPhone.WrapMeasured(new string('W',90)+" wide vehicle description",228,.27f);
  Check(wide.All(line=>PhoneOverlay.MeasureText(line,.27f)<=228),"Wide letters and long unbroken labels wrap inside the actual detail panel width");
  Check(string.Join("",wide).Replace(" ","")==new string('W',90)+"widevehicledescription","Measured wrapping preserves the entire description without dropping text");
  Reset();string path=Path.Combine(root,"phone-layout.json");var state=CampaignState.Load(path);state.CashOnHand=42000;state.Completed.Add("M03");
  Func<CampaignState,CampaignPhone> create=s=>new CampaignPhone(s,new CampaignDispatches(s),()=>"Job",()=>"GPS",slot=>"Ready","F6",Path.GetFullPath(Path.Combine(dataDir,"../assets/ui")));
  var phone=create(state);phone.Open(CrewSlot.Guess);
  var expected=new[]{CampaignPhone.App.Garage,CampaignPhone.App.Commands,CampaignPhone.App.Journal,CampaignPhone.App.Progression,CampaignPhone.App.Alerts,CampaignPhone.App.Planning};
  Check(Enumerable.Range(0,6).Select(phone.HomeAppAt).SequenceEqual(expected)&&phone.HomeIndexOf(CampaignPhone.App.News)>=6,"Default first page contains the former second-page apps, with Weazel News on page two");
  var original=Enumerable.Range(0,15).Select(phone.HomeAppAt).ToArray();
  phone.Arrange();
  Check(phone.IsArranging,"A home layout can be arranged without consuming controller X");
  for(int i=0;i<9;i++)phone.Move(MenuDirection.Right);
  Check(phone.HomeIndexOf(CampaignPhone.App.Garage)==9&&phone.Selection==9,"Picked-up icon follows D-pad movement across home pages");
  Check(state.PhoneAppOrder.Count==0,"Unconfirmed rearrangement does not mutate saved preferences");
  phone.Back();Check(!phone.IsArranging&&phone.IsOpen&&Enumerable.Range(0,15).Select(phone.HomeAppAt).SequenceEqual(original),"B cancels the complete arrangement and returns to its original selected app");
  phone.Arrange();phone.Move(MenuDirection.Down);phone.Select();
  Check(!phone.IsArranging&&phone.Page==CampaignPhone.App.Home&&phone.HomeIndexOf(CampaignPhone.App.Garage)==2,"A confirms the new order without launching the app or triggering a purchase");
  var saved=CampaignState.Load(path);Check(saved.PhoneAppOrder.Count==15&&saved.CashOnHand==42000&&saved.Completed.Contains("M03"),"App order persists alongside unchanged campaign money and progress");
  phone.Close();var other=create(saved);other.Open(CrewSlot.Ice);
  Check(other.HomeIndexOf(CampaignPhone.App.Garage)==2,"A new phone instance and another brother use the same saved arrangement");
  PhoneApp(other,CampaignPhone.App.Garage);other.Back();Check(other.Selection==2,"Returning from an app selects its current home position, not its old enum index");
  other.Arrange();other.Move(MenuDirection.Right);other.FinishFrame(false);other.Open(CrewSlot.Ice);
  Check(!other.IsArranging&&other.HomeIndexOf(CampaignPhone.App.Garage)==2,"Mission interruption cancels an unconfirmed move instead of silently saving it");
  other.Shutdown();saved.PhoneAppOrder.Clear();saved.PhoneAppOrder.AddRange(new[]{"News","News","Home","Unknown","999","Garage"});saved.Save();
  var recovered=create(CampaignState.Load(path));recovered.Open(CrewSlot.Gohan);
  Check(Enumerable.Range(0,15).Select(recovered.HomeAppAt).Distinct().Count()==15&&recovered.HomeIndexOf(CampaignPhone.App.News)==0&&recovered.HomeIndexOf(CampaignPhone.App.Garage)==1,"Duplicate, obsolete and invalid saved app IDs are removed while missing apps are restored");
  recovered.Shutdown();phone=create(state);phone.Open(CrewSlot.Guess);PhonePreview(phone,"compact-saved-layout");
  for(int i=0;i<8;i++)phone.Move(MenuDirection.Right);phone.Render((x,y,w,h,color)=>{},(value,x,y,size,color)=>{},"12:00");
  Check(phone.Page==CampaignPhone.App.Home,"Page two renders every app including Settings when artwork is unavailable");
  phone.Arrange();phone.Move(MenuDirection.Left);PhonePreview(phone,"arrange-across-pages");phone.Shutdown();
 }
}

using System;
using System.Drawing;
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
 static void PhoneApp(CampaignPhone phone, CampaignPhone.App app)
 {
  while(phone.Page!=CampaignPhone.App.Home)phone.Back();
  while(phone.Selection!=phone.HomeIndexOf(app))phone.Move(MenuDirection.Right);
  phone.Select();
 }
 static void PhonePreview(CampaignPhone phone,string name)
 {
  string output=Path.GetFullPath(Path.Combine(dataDir,"../build/phone-preview"));Directory.CreateDirectory(output);
  using(var bmp=new Bitmap(316,578))using(var g=Graphics.FromImage(bmp))
  {
   g.Clear(Color.FromArgb(62,69,79));g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
   phone.Render((x,y,w,h,c)=>{using(var brush=new SolidBrush(c))g.FillRectangle(brush,x-934,y-116,w,h);},
    (text,x,y,size,c)=>PhoneOverlay.LayoutText(text,x,y,size,c,(code,gx,gy,w,h,tint)=>{
      using(var glyph=Image.FromFile(Path.GetFullPath(Path.Combine(dataDir,"../assets/ui/"+PhoneOverlay.GlyphName(code)))))
      using(var attributes=new System.Drawing.Imaging.ImageAttributes()) {
       var matrix=new System.Drawing.Imaging.ColorMatrix();matrix.Matrix00=tint.R/255f;matrix.Matrix11=tint.G/255f;matrix.Matrix22=tint.B/255f;matrix.Matrix33=tint.A/255f;attributes.SetColorMatrix(matrix);
       g.DrawImage(glyph,new[]{new PointF(gx-934,gy-116),new PointF(gx-934+w,gy-116),new PointF(gx-934,gy-116+h)},new RectangleF(0,0,glyph.Width,glyph.Height),GraphicsUnit.Pixel,attributes);
      }
    }),"21:47",(asset,x,y,width,height)=>{
      string path=Path.GetFullPath(Path.Combine(dataDir,"../assets/ui/phone-"+asset+".png"));
      using(var image=Image.FromFile(path))g.DrawImage(image,x-934,y-116,width,height);return true;
    });
   bmp.Save(Path.Combine(output,name+".png"),System.Drawing.Imaging.ImageFormat.Png);
  }
 }
 static void CampaignPhoneChecks()
 {
  Reset();Game.Disabled.Clear();Game.TimeScale=.8f;
  var state=CampaignState.Load(Path.Combine(root,"phone.json"));state.CashOnHand=125000;
  string objective="Reach the foundry with the crew. Park the current vehicle in the yellow marker. Stay together until the delivery is complete.";
  int routes=0;var dispatches=new CampaignDispatches(state);
  var phone=new CampaignPhone(state,dispatches,()=>"Return to the foundry\n\n"+objective,()=>{routes++;return "Destination shown.";},slot=>slot==CrewSlot.Guess?"You":"On assignment","F6");
  try
  {
   // A scoped sniper zooms on the same d-pad button, so that press belongs to the scope.
   Game.Player.Character.IsAiming=true;Function.WeaponGroup=unchecked((uint)Game.GenerateHash("GROUP_SNIPER"));
   Game.Pressed.Add(Control.Phone);phone.Input(true,true,true,CrewSlot.Guess);
   Check(!phone.IsOpen&&CampaignPhone.ScopeOwnsTheDpad,"A sniper scope keeps the d-pad, so the phone stays shut mid-shot");
   Game.Player.Character.IsAiming=false;Function.WeaponGroup=0;Game.Pressed.Clear();
   // Ron, September 22: TrainerV's menu opens on RB + X and scrolls on the d-pad, and d-pad Up
   // opened the phone over it.
   var method=Game.LastInputMethod;Game.LastInputMethod=InputMethod.GamePad;CampaignPhone.ForgetForeignMenu();
   Function.Held[Control.FrontendRb]=true;Game.Pressed.Add(Control.FrontendX);phone.Input(true,true,true,CrewSlot.Guess);Function.Held[Control.FrontendRb]=false;
   Game.Pressed.Add(Control.Phone);phone.Input(true,true,true,CrewSlot.Guess);
   Check(CampaignPhone.ForeignMenuOpen&&!phone.IsOpen,"With the trainer's menu up, d-pad Up is left to the trainer and the phone stays shut");
   Function.Held[Control.FrontendUp]=true;Game.GameTime+=CampaignPhone.ForeignMenuIdleMs-1000;phone.Input(true,true,true,CrewSlot.Guess);Function.Held[Control.FrontendUp]=false;
   Game.GameTime+=CampaignPhone.ForeignMenuIdleMs-1000;phone.Input(true,true,true,CrewSlot.Guess);
   Check(CampaignPhone.ForeignMenuOpen,"Scrolling the trainer keeps it counted as open");
   Game.GameTime+=2000;phone.Input(true,true,true,CrewSlot.Guess);Game.Pressed.Clear();
   Check(!CampaignPhone.ForeignMenuOpen,"and once the trainer has sat unused for a while the d-pad is the phone's again");
   Game.LastInputMethod=method;
   Game.Pressed.Add(Control.Phone);phone.Input(true,true,true,CrewSlot.Guess);
   Check(phone.IsOpen&&phone.Page==CampaignPhone.App.Home,"D-pad Up opens the campaign phone on its home screen");
   Check(CampaignPhone.BlocksGameplayInput&&Game.Disabled.Contains(Control.Phone),"Custom phone takes input focus and suppresses the vanilla phone");
   Check(Game.Player.CanControlCharacter&&!Game.Player.Character.IsPositionFrozen&&Game.TimeScale==.8f,"Opening the phone leaves player ownership, entity freeze and time untouched");
   Check(!Game.Disabled.Contains(Control.MoveLeftRight)&&!Game.Disabled.Contains(Control.LookUpDown)&&!Game.Disabled.Contains(Control.VehicleAccelerate)&&!Game.Disabled.Contains(Control.VehicleBrake),"Both movement sticks, acceleration and braking remain live");
   Check(Game.Disabled.Contains(Control.Context)&&Game.Disabled.Contains(Control.ScriptPadLeft)&&Game.Disabled.Contains(Control.VehicleRadioWheel)&&Game.Disabled.Contains(Control.CharacterWheel),"Phone navigation consumes D-pad actions including scripted prompts, radio and character switching");
   Check(!Game.Disabled.Contains(Control.Attack)&&!Game.Disabled.Contains(Control.Aim)&&!Game.Disabled.Contains(Control.Jump)&&!Game.Disabled.Contains(Control.Enter)&&!Game.Disabled.Contains(Control.VehicleHandbrake),"Phone leaves triggers, X, Y and shoulder-button gameplay available");
   Game.Pressed.Add(Control.Attack);Check(ControllerInput.JustPressed(Control.Attack),"Shared mod input still accepts unreserved controller buttons while the phone is open");
   Check(Function.Calls.Any(call=>call.Item1==Hash.DISABLE_CONTROL_ACTION&&(int)call.Item2[0]==2&&(int)call.Item2[1]==(int)Control.ScriptRRight),"Phone also consumes B aliases in the frontend control group");
   Game.Pressed.Add(Control.FrontendAccept);Check(!ControllerInput.JustPressed(Control.FrontendAccept),"Gameplay cannot read disabled A input while the phone owns it");
   phone.Input(true,true,true,CrewSlot.Guess);Check(phone.Page==CampaignPhone.App.Garage,"The phone itself can still read A after gameplay input was blocked and opens Garage first");PhoneApp(phone,CampaignPhone.App.Messages);
   Check(phone.Messages.Length==0,"A fresh campaign reveals no future messages");phone.Select();
   Check(phone.Body()=="","Selecting an empty inbox is harmless");
   state.Completed.Add("M01");state.Completed.Add("M06");state.Save();
   Check(phone.Messages.Length==1&&phone.Messages[0].Sender=="Gohan","Inbox exposes only unlocked crew follow-ups, keeping news in its own app");
   phone.Select();Check(phone.Body().Contains("ledger copy"),"Message detail shows the complete authored story text");
   state.ReadDispatches.Add("M01");Check(phone.Body().Contains("ledger copy"),"Previously delivered notifications remain readable in the phone");
   PhoneApp(phone,CampaignPhone.App.News);Check(phone.Messages.Length==1&&phone.Messages[0].Sender=="Weazel News","News uses campaign-gated events");
   phone.Select();Check(phone.Body().Contains("industrial district"),"News detail displays the real completed-job report");
   PhoneApp(phone,CampaignPhone.App.Crew);phone.Select();Check(phone.Body().StartsWith("Ice\n")&&phone.Body().Contains("On assignment"),"Crew contacts use nicknames and live status");
   phone.Back();for(int i=0;i<8;i++)phone.Move(MenuDirection.Down);
   Check(phone.Selection==2,"KJ's contact does not appear before his solo job");
   state.Completed.Add("SM03");phone.Move(MenuDirection.Down);phone.Select();
   Check(phone.Body().StartsWith("KJ\n")&&phone.Body().Contains("garage"),"KJ contact unlocks with his mission and directs players to the actual delivery service");
   PhoneApp(phone,CampaignPhone.App.Wallet);Check(phone.Body().Contains("$125,000"),"Wallet displays the real shared campaign balance");
   state.CashOnHand=32000;Check(phone.Body().Contains("$32,000"),"Wallet refreshes after a balance change without reopening");
   PhoneApp(phone,CampaignPhone.App.Job);Check(phone.Body().Contains(objective),"Job app preserves the full active objective");
   phone.Select();Check(routes==0,"Destination request waits until this frame's mission markers are updated");
   phone.FinishFrame(true);Check(routes==1&&phone.Notice=="Destination shown.","Validated route action runs once after mission update");
   phone.FinishFrame(true);Check(routes==1,"Rendering never repeats a navigation action");
   objective="The objective changed while the phone was open.";Check(phone.Body().Contains(objective),"An open job page follows stage changes");
   phone.Select();phone.FinishFrame(false);Check(!phone.IsOpen&&routes==1,"Cutscene, death or mandatory handoff closes the phone and discards pending navigation");
   Check(CampaignPhone.BlocksGameplayInput,"Closing consumes trailing button input briefly");
   Game.GameTime+=181;Check(!CampaignPhone.BlocksGameplayInput,"Input focus is released automatically without an unfreeze task");
   phone.Open(CrewSlot.Guess);phone.Input(true,true,true,CrewSlot.Ice);Check(!phone.IsOpen,"An externally changed active brother closes the old phone");
   Game.GameTime+=181;phone.Open(CrewSlot.Gohan);phone.Input(false,true,true,CrewSlot.Gohan);Check(!phone.IsOpen,"Disabling the feature closes the overlay");
   Game.GameTime+=181;phone.Open(CrewSlot.Guess);phone.Input(true,false,true,CrewSlot.Guess);Check(!phone.IsOpen,"Standing down releases the phone");
   phone.Shutdown();Game.Disabled.Clear();phone.Input(false,false,false,CrewSlot.Guess);
   Check(!Game.Disabled.Contains(Control.Phone)&&!CampaignPhone.BlocksGameplayInput,"Undeployed/disabled phone leaves vanilla phone input alone");
   phone.Open(CrewSlot.Guess);phone.Back();Check(!phone.IsOpen,"B from home closes the phone");
   phone.Shutdown();phone.Open(CrewSlot.Guess);PhoneApp(phone,CampaignPhone.App.Help);
   for(int i=0;i<100;i++)phone.Move(MenuDirection.Down);Check(phone.Scroll>0,"Long help and objective text can be scrolled to its end");
   for(int i=0;i<100;i++)phone.Move(MenuDirection.Up);Check(phone.Scroll==0,"Scrolling clamps at the beginning");
   Check(CampaignPhone.Wrap("~y~GOHAN~s~\n\n"+new string('x',90)).All(s=>s.Length<=34),"Phone text strips GTA formatting and wraps even unbroken long tokens");
   Check(CampaignPhone.Wrap("ABC",0).Count==3,"Invalid wrap widths cannot hang the render loop");
   phone.Shutdown();phone.Open(CrewSlot.Guess);PhonePreview(phone,"guess-home");
   phone.Shutdown();phone.Open(CrewSlot.Ice);PhoneApp(phone,CampaignPhone.App.Messages);phone.Select();PhonePreview(phone,"ice-message");
   phone.Shutdown();phone.Open(CrewSlot.Gohan);PhoneApp(phone,CampaignPhone.App.Job);objective="Drive the crew to the foundry. Park inside the yellow marker, then leave the vehicle and meet Ice by the loading doors.";PhonePreview(phone,"gohan-job");
   state.Save();var loaded=CampaignState.Load(Path.Combine(root,"phone.json"));
   Check(new CampaignDispatches(loaded).Inbox.Count()==3&&loaded.CashOnHand==32000,"Existing save format preserves phone messages and balance across reloads");
   phone.Shutdown();Check(!CampaignPhone.BlocksGameplayInput&&Game.TimeScale==.8f&&Game.Player.CanControlCharacter,"Script shutdown releases phone focus without resetting unrelated time/control state");

   ObjectiveMarkers.Clear();ObjectiveMarkers.ActiveSlot=CrewSlot.Guess;ObjectiveMarkers.BeginFrame(true);
   ObjectiveMarkers.Navigation(new Vector3(100,0,0),CrewSlot.Ice);ObjectiveMarkers.EndFrame();
   Check(ObjectiveMarkers.FocusPlayerDestination(CrewSlot.Guess).StartsWith("No single"),"Phone GPS refuses another brother's assignment");
   ObjectiveMarkers.BeginFrame(true);ObjectiveMarkers.Navigation(new Vector3(200,0,0),CrewSlot.Guess);ObjectiveMarkers.EndFrame();
   Check(ObjectiveMarkers.FocusPlayerDestination(CrewSlot.Guess).Contains("yellow")&&World.LastBlip.ShowRoute,"Phone restores the current mission route without adding a personal waypoint");
   ObjectiveMarkers.BeginFrame(true);ObjectiveMarkers.Navigation(new Vector3(200,0,0),CrewSlot.Guess);ObjectiveMarkers.Navigation(new Vector3(300,0,0),CrewSlot.Guess);ObjectiveMarkers.EndFrame();
   Check(ObjectiveMarkers.FocusPlayerDestination(CrewSlot.Guess).StartsWith("No single"),"Ambiguous parallel destinations ask the player to follow objective instructions");
   var boat=new Vehicle{Model=new Model("dinghy")};ObjectiveMarkers.BeginFrame(true);ObjectiveMarkers.Navigation(new Vector3(500,0,0),CrewSlot.Guess,boat);ObjectiveMarkers.EndFrame();
   Check(ObjectiveMarkers.FocusPlayerDestination(CrewSlot.Guess).Contains("water")&&!World.LastBlip.ShowRoute,"Boat objectives retain a water marker instead of a false road route");
   ObjectiveMarkers.Clear();Check(ObjectiveMarkers.FocusPlayerDestination(CrewSlot.Guess).StartsWith("No single"),"Mission cleanup makes old destinations unavailable immediately");
  }
  finally {phone.Shutdown();ObjectiveMarkers.Clear();ObjectiveMarkers.ActiveSlot=null;Game.TimeScale=1;Game.Pressed.Clear();Game.Disabled.Clear();}
 }
}

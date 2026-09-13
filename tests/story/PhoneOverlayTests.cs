using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA.UI;

public static partial class StoryTests
{
 static void PhoneSpritePreview(string name)
 {
  string output=Path.GetFullPath(Path.Combine(dataDir,"../build/phone-preview"));Directory.CreateDirectory(output);
  using(var bmp=new Bitmap(316,578))using(var g=Graphics.FromImage(bmp))
  {
   g.Clear(Color.FromArgb(62,69,79));
   g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
   foreach(var draw in CustomSprite.Drawn)
   using(var source=Image.FromFile(draw.Item1))using(var attributes=new System.Drawing.Imaging.ImageAttributes())
   {
    var color=draw.Item4;var matrix=new System.Drawing.Imaging.ColorMatrix();
    matrix.Matrix00=color.R/255f;matrix.Matrix11=color.G/255f;matrix.Matrix22=color.B/255f;matrix.Matrix33=color.A/255f;
    attributes.SetColorMatrix(matrix);float x=draw.Item2.X-934,y=draw.Item2.Y-116,w=draw.Item3.Width,h=draw.Item3.Height;
    g.DrawImage(source,new[]{new PointF(x,y),new PointF(x+w,y),new PointF(x,y+h)},new RectangleF(0,0,source.Width,source.Height),GraphicsUnit.Pixel,attributes);
   }
   bmp.Save(Path.Combine(output,name+".png"),System.Drawing.Imaging.ImageFormat.Png);
  }
 }
 static void PhoneOverlayChecks()
 {
  Reset();
  string assets=Path.GetFullPath(Path.Combine(dataDir,"../assets/ui"));
  var state=CampaignState.Load(Path.Combine(root,"phone-overlay.json"));state.CashOnHand=123456;
  state.Completed.Add("M01");
  var phone=new CampaignPhone(state,new CampaignDispatches(state),()=>"Return to the foundry",()=>"Route shown",slot=>"Available","F6",assets);
  phone.Open(CrewSlot.Guess);
  CustomSprite.Drawn.Clear();TextElement.Draws=0;phone.FinishFrame(true);
  Check(TextElement.Draws==0,"Artwork phone never places its text underneath the external sprite layer");
  Check(CustomSprite.Drawn.Count(d=>Path.GetFileName(d.Item1).StartsWith("phone-glyph-"))>70,"Real FinishFrame emits visible glyph sprites for clock, labels, balance and footer");
  int wallpaper=CustomSprite.Drawn.FindIndex(d=>Path.GetFileName(d.Item1)=="phone-guess.png");
  int label=CustomSprite.Drawn.FindIndex(d=>Path.GetFileName(d.Item1).StartsWith("phone-glyph-"));
  Check(wallpaper>=0&&label>wallpaper,"Phone text is submitted after its wallpaper on the same overlay");
  PhoneSpritePreview("runtime-home");
  int loaded=CustomSprite.Created;
  for(int n=0;n<80;n++){CustomSprite.Drawn.Clear();state.CashOnHand=n*1701;phone.FinishFrame(true);}
  Check(CustomSprite.Created==loaded,"Changing balances and clock never creates additional glyph textures");
  PhoneApp(phone,CampaignPhone.App.Messages);CustomSprite.Drawn.Clear();phone.FinishFrame(true);
  Check(CustomSprite.Drawn.Any(d=>Path.GetFileName(d.Item1)=="phone-glyph-071.png"),"Message rows render the G in Gohan with the artwork layer");
  PhoneSpritePreview("runtime-inbox");
  phone.Select();CustomSprite.Drawn.Clear();phone.FinishFrame(true);
  Check(CustomSprite.Drawn.Count(d=>Path.GetFileName(d.Item1).StartsWith("phone-glyph-"))>100,"Opening a message emits its body text rather than an empty card");
  Check(TextElement.Draws==0,"Every rendered home, inbox and message frame stays on the overlay");
  PhoneSpritePreview("runtime-message");
  phone.Shutdown();

  string incomplete=Path.Combine(root,"incomplete-phone-art");Directory.CreateDirectory(incomplete);
  File.Copy(Path.Combine(assets,"phone-guess.png"),Path.Combine(incomplete,"phone-guess.png"),true);
  var fallback=new CampaignPhone(state,new CampaignDispatches(state),()=>"Job",()=>"GPS",slot=>"Ready","F6",incomplete);
  fallback.Open(CrewSlot.Guess);CustomSprite.Drawn.Clear();TextElement.Draws=0;fallback.FinishFrame(true);
  Check(TextElement.Draws>10&&CustomSprite.Drawn.Count==0,"Missing font assets produce a completely native readable phone with no covering wallpaper");
  fallback.Shutdown();
  var codes=new System.Collections.Generic.List<int>();var positions=new System.Collections.Generic.List<PointF>();
  PhoneOverlay.LayoutText("~y~A A\n\u201cB\u201d \u2014 \u2603",10,20,.5f,Color.White,(code,x,y,w,h,c)=>{codes.Add(code);positions.Add(new PointF(x,y));});
  Check(codes.SequenceEqual(new[]{65,65,34,66,34,45,63}),"Glyph layout strips GTA tokens, preserves punctuation and substitutes unsupported characters safely");
  Check(positions[1].X>positions[0].X&&positions[2].Y>positions[1].Y,"Glyph layout advances past spaces and resets onto the next line");
 }
}

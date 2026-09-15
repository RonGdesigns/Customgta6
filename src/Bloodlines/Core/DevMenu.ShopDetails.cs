using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Native;
using GTA.UI;

namespace Bloodlines.Core
{
    /// <summary>
    /// The card beside the car, and the swatch grid that replaced the list of paint names.
    ///
    /// Ron found a rental interface that showed a car the way a car should be shown — the
    /// make above the model, the performance as bars with numbers on them, and the paint
    /// as colors you can see — and asked for that in our customs shop, in our own type and
    /// our own colors rather than theirs. None of the rental machinery came with it: no
    /// duration, no hourly rate, no total due. This is the part that was missing.
    ///
    /// It sits on the right-hand edge now. It used to sit at x 490, which in a 1280-wide
    /// layout is the middle of the screen — directly over the car being worked on.
    /// </summary>
    public sealed partial class DevMenu
    {
        private readonly List<string> _shopReceipts=new List<string>();
        private void ShopNotice(string message)
        {
            _shopReceipts.Add(System.Text.RegularExpressions.Regex.Replace(message??"","~[^~]*~",""));
            if(_shopReceipts.Count>3)_shopReceipts.RemoveAt(0);
        }

        // ---------- the card's own measurements and palette ----------

        /// <summary>Hard right, so the middle of the screen belongs to the car.</summary>
        private const float CardX = 878f, CardW = 374f, CardTop = 52f;
        private const float CardPad = 14f, BarWidth = CardW - CardPad * 2f;
        /// <summary>
        /// Readable over a car rather than instead of it. This panel used to be alpha
        /// 235 over a fixed 510 pixels, which is effectively opaque and sat exactly
        /// where the vehicle preview is: Ron could not see the part he was fitting.
        /// </summary>
        private const int ShopPanelAlpha = 148;

        private static readonly Color Amber = Color.FromArgb(235, 232, 168, 56);
        private static readonly Color Paper = Color.FromArgb(245, 245, 245, 245);
        private static readonly Color Quiet = Color.FromArgb(205, 150, 156, 166);
        private static readonly Color Money = Color.FromArgb(230, 150, 200, 160);
        private static readonly Color Trough = Color.FromArgb(210, 46, 51, 58);

        /// <summary>
        /// How far down the panel content reached last frame, so the backdrop is the
        /// size of what is in it. One frame behind, which nobody can see, and it means
        /// the panel stops covering the car the moment the content is short.
        /// </summary>
        private float _shopPanelBottom;

        private static void ShopText(string text,float x,float y,float scale,Color color)
        {new TextElement(text,new PointF(x,y),scale,color).Draw();}
        private static void RightText(string text,float right,float y,float scale,Color color)
        {new TextElement(text,new PointF(right,y),scale,color){Alignment=Alignment.Right}.Draw();}
        private static void Box(float x,float y,float w,float h,Color color)
        {new ContainerElement(new PointF(x,y),new SizeF(w,h),color).Draw();}

        /// <summary>A section heading with the hairline above it that separates the blocks.</summary>
        private static void Section(string title,ref float y)
        {
            Box(CardX+CardPad,y,BarWidth,1f,Color.FromArgb(120,90,96,106));y+=7f;
            ShopText(title,CardX+CardPad,y,.27f,Amber);y+=24f;
        }

        private static void StatBar(string label,float current,float? before,ref float y)
        {
            float x=CardX+CardPad;
            float value=Math.Max(0,Math.Min(1,current));
            ShopText(label,x,y,.25f,Quiet);
            RightText(((int)Math.Round(value*100f)).ToString(),x+BarWidth,y,.25f,Paper);
            y+=19f;
            Box(x,y,BarWidth,7f,Trough);
            Box(x,y,BarWidth*value,7f,Paper);
            if(before.HasValue&&Math.Abs(current-before.Value)>.0001f)
            {
                float old=Math.Max(0,Math.Min(1,before.Value));
                Box(x+BarWidth*Math.Min(value,old),y,BarWidth*Math.Abs(value-old),7f,
                    current>before.Value?Color.LimeGreen:Color.IndianRed);
            }
            y+=17f;
        }

        // ---------- the card ----------

        private void DrawShopDetails(Page page)
        {
            Function.Call(Hash.THEFEED_HIDE_THIS_FRAME);
            float y=CardTop;
            float height=Math.Min(600f,Math.Max(120f,_shopPanelBottom-CardTop+12f));
            Box(CardX,CardTop,CardW,height,Color.FromArgb(ShopPanelAlpha,18,20,24));
            // Our own edge: the menu title's amber, stood on end down the card.
            Box(CardX,CardTop,3f,height,Amber);

            var car=ShopService.IsGarage(_shopping)?Shops.Car(_shopping):null;
            if(car!=null)DrawVehicleHead(car,ref y);else DrawShopHead(ref y);

            if(car!=null)
            {
                var values=ShopPerformance.Vehicle(car);
                Section("PERFORMANCE",ref y);
                string[] names={"Top speed","Acceleration","Braking","Traction"};
                if(values!=null)
                {
                    for(int i=0;i<values.Length;i++)
                        StatBar(names[i],values[i],Shops.PreviewPerformance==null?(float?)null:Shops.PreviewPerformance[i],ref y);
                    y+=4f;
                    int kph=(int)Math.Round(values[0]*VehicleSpecs.SpeedCeilingKph);
                    ShopText(kph+" km/h  -  "+(int)Math.Round(kph*.621371f)+" mph estimated",CardX+CardPad,y,.23f,Quiet);
                    y+=20f;
                }
                ShopText(Shops.HasVehiclePreview?"Preview: green gain, red loss, nothing charged yet."
                    :"Fitted build. Cosmetic parts move no bar.",CardX+CardPad,y,.22f,Quiet);
                y+=26f;
                DrawFinishChips(car,ref y);
            }
            else if(_shopping.Kind==ShopKind.Weapons) DrawWeaponStats(page,ref y);

            Section("RECENT RECEIPTS",ref y);
            if(_shopReceipts.Count==0){ShopText("Nothing bought yet this visit.",CardX+CardPad,y,.23f,Quiet);y+=18f;}
            foreach(var message in _shopReceipts)
            {
                foreach(var line in WrapReceipt(message,50))
                {if(y>620f){_shopPanelBottom=y;return;}ShopText(line,CardX+CardPad,y,.23f,Paper);y+=17f;}
                y+=5f;
            }
            _shopPanelBottom=y;
        }

        /// <summary>
        /// Make above model, the way a car is actually named: KARIN, then Futo. The make
        /// comes out of the game's own label table, so it is the manufacturer Rockstar
        /// gave the car and not a guess from the model name.
        /// </summary>
        private void DrawVehicleHead(Vehicle car,ref float y)
        {
            ShopText(MakeOf(car.Model),CardX+CardPad,y+8f,.24f,Quiet);
            // Most model names are one short word; the few that are three set smaller
            // rather than run off the edge of the card.
            string model=car.LocalizedName??"";
            ShopText(model,CardX+CardPad-2f,y+20f,model.Length>15?.34f:.46f,Paper);
            string badge=car.ClassLocalizedName;
            int seats=car.PassengerCapacity+1;
            if(seats>0)badge+="  -  "+seats+(seats==1?" seat":" seats");
            ShopText(badge.ToUpperInvariant(),CardX+CardPad,y+56f,.22f,Amber);
            RightText("CREW CASH",CardX+CardW-CardPad,y+8f,.2f,Quiet);
            RightText("$"+_state.CashOnHand.ToString("N0"),CardX+CardW-CardPad,y+20f,.32f,Money);
            y+=78f;
        }

        private void DrawShopHead(ref float y)
        {
            ShopText(_shopping.Kind==ShopKind.Weapons?"SHOPPING FOR":"SERVICE",CardX+CardPad,y+8f,.24f,Quiet);
            ShopText(_shopping.Kind==ShopKind.Weapons?Shops.CustomerName:_shopping.Name,CardX+CardPad-2f,y+20f,.42f,Paper);
            RightText("CREW CASH",CardX+CardW-CardPad,y+8f,.2f,Quiet);
            RightText("$"+_state.CashOnHand.ToString("N0"),CardX+CardW-CardPad,y+20f,.32f,Money);
            y+=62f;
        }

        /// <summary>
        /// The manufacturer, localized where the game has a string for it. The native
        /// hands back a label such as <c>KARIN</c>; most of them read as the name already,
        /// which is why the raw label is the fallback rather than an empty line.
        /// </summary>
        private static string MakeOf(Model model)
        {
            try
            {
                string label=Function.Call<string>(Hash.GET_MAKE_NAME_FROM_VEHICLE_MODEL,model.Hash);
                if(string.IsNullOrWhiteSpace(label))return "";
                string named=Game.GetLocalizedString(label);
                if(string.IsNullOrWhiteSpace(named)||named=="NULL")named=label;
                return named.ToUpperInvariant();
            }
            catch{return "";}
        }

        /// <summary>
        /// The four painted channels as the colors they are. Nothing here reads a name to
        /// the player: "Pearlescent: Ultra Blue" is two words that still do not say which
        /// blue is on the car.
        /// </summary>
        private void DrawFinishChips(Vehicle car,ref float y)
        {
            if(!PaintPalette.Ready)return;
            int[] channels;
            try
            {
                var primary=new OutputArgument();var secondary=new OutputArgument();
                Function.Call(Hash.GET_VEHICLE_COLOURS,car,primary,secondary);
                var pearl=new OutputArgument();var rim=new OutputArgument();
                Function.Call(Hash.GET_VEHICLE_EXTRA_COLOURS,car,pearl,rim);
                channels=new[]{primary.GetResult<int>(),secondary.GetResult<int>(),pearl.GetResult<int>(),rim.GetResult<int>()};
            }
            catch{return;}

            Section("FINISH",ref y);
            string[] names={"PRIMARY","SECOND","PEARL","RIMS"};
            float chip=(BarWidth-18f)/4f;
            for(int i=0;i<4;i++)
            {
                float x=CardX+CardPad+i*(chip+6f);
                Box(x-1f,y-1f,chip+2f,20f,Color.FromArgb(180,90,96,106));
                Box(x,y,chip,18f,PaintPalette.Of(channels[i]));
                ShopText(names[i],x,y+20f,.19f,Quiet);
            }
            y+=44f;
        }

        private void DrawWeaponStats(Page page,ref float y)
        {
            Section("PERFORMANCE",ref y);
            uint hash=page.Selected?.StatWeapon??0;
            if(hash==0&&Shops.CustomerPed!=null&&Shops.CustomerPed.Exists())hash=Function.Call<uint>(Hash.GET_SELECTED_PED_WEAPON,Shops.CustomerPed);
            var stats=ShopPerformance.Weapon(hash);uint part=page.Selected?.StatComponent??0;
            var delta=part==0?null:ShopPerformance.Weapon(part,true);
            string[] names={"Damage","Fire rate","Capacity","Accuracy","Range"};
            if(stats==null){ShopText("No performance ratings for this item.",CardX+CardPad,y,.24f,Quiet);y+=28f;return;}
            var fitted=(int[])stats.Clone();
            var parts=WeaponUpgrades.Parts(Shops.CustomerPed,hash).ToArray();
            foreach(var active in parts.Where(p=>p.Active))
            {var d=ShopPerformance.Weapon(WeaponUpgrades.Id(active),true);if(d!=null)for(int i=0;i<5;i++)fitted[i]+=d[i];}
            // Show the selected part's native rating separately; compatible
            // replacements can remove a different fitted attachment.
            for(int i=0;i<5;i++)StatBar(names[i]+(delta==null?"":"   selected part "+(delta[i]>=0?"+":"")+delta[i]),fitted[i]/100f,null,ref y);
            y+=4f;
            ShopText("Game ratings with fitted parts.",CardX+CardPad,y,.22f,Quiet);y+=26f;
        }

        private static IEnumerable<string> WrapReceipt(string message,int width)
        {
            string line="";foreach(var word in message.Split(' '))
            {if(line.Length+word.Length+1>width){yield return line;line="";}line+=(line.Length>0?" ":"")+word;}
            if(line.Length>0)yield return line;
        }

        // ---------- the swatch grid ----------

        /// <summary>Eight across fits the menu's own width without crowding the tiles.</summary>
        public const int SwatchColumns = 8;
        /// <summary>
        /// How many rows are on screen at once. Ron asked for two or three and a scroll;
        /// five costs no height the menu was not already using and takes a hundred and
        /// sixty paints down to four screens instead of seven.
        /// </summary>
        public const int SwatchRows = 5;
        private const float TileW = 46f, TileH = 30f, TileGap = 5f;

        /// <summary>
        /// Draw a page as colored tiles instead of rows. Returns false when the page has
        /// no colors to draw — a palette the game would not give up — so the caller falls
        /// back to the list of names, which is what this replaced.
        /// </summary>
        private bool DrawSwatchGrid(Page page,float x,float width,ref float y)
        {
            if(page.Columns<=0||page.Items.Count==0)return false;
            var selected=page.Selected;
            if(selected?.Swatch?.Invoke()==null)return false;

            int columns=page.Columns;
            int rows=(page.Items.Count+columns-1)/columns;
            int current=page.Index/columns;
            int first=Math.Max(0,Math.Min(current-SwatchRows/2,rows-SwatchRows));

            float left=x+(width-(columns*TileW+(columns-1)*TileGap))/2f;
            for(int row=first;row<Math.Min(first+SwatchRows,rows);row++)
            {
                for(int column=0;column<columns;column++)
                {
                    int index=row*columns+column;
                    if(index>=page.Items.Count)break;
                    var color=page.Items[index].Swatch?.Invoke();
                    if(color==null)continue;
                    float tileX=left+column*(TileW+TileGap);
                    if(index==page.Index)
                    {
                        Box(tileX-3f,y-3f,TileW+6f,TileH+6f,Amber);
                        Box(tileX-1f,y-1f,TileW+2f,TileH+2f,Color.FromArgb(235,18,20,24));
                    }
                    else Box(tileX-1f,y-1f,TileW+2f,TileH+2f,Color.FromArgb(150,70,75,84));
                    Box(tileX,y,TileW,TileH,color.Value);
                }
                y+=TileH+TileGap+3f;
            }

            y+=4f;
            Box(x,y,width,26f,Color.FromArgb(235,56,62,74));
            ShopText(selected.Label,x+10f,y+4f,.30f,Paper);
            string value=selected.Value?.Invoke()??"";
            if(!string.IsNullOrEmpty(value))RightText(value,x+width-10f,y+4f,.28f,Money);
            y+=28f;
            return true;
        }
    }
}

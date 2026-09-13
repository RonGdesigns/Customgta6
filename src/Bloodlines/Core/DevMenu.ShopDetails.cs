using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Native;
using GTA.UI;

namespace Bloodlines.Core
{
    public sealed partial class DevMenu
    {
        private readonly List<string> _shopReceipts=new List<string>();
        private void ShopNotice(string message)
        {
            _shopReceipts.Add(System.Text.RegularExpressions.Regex.Replace(message??"","~[^~]*~",""));
            if(_shopReceipts.Count>3)_shopReceipts.RemoveAt(0);
        }
        private static void ShopText(string text,float x,float y,float scale,Color color)
        {new TextElement(text,new PointF(x,y),scale,color).Draw();}
        private static void StatBar(string label,float current,float? before,ref float y)
        {
            const float x=500f,w=330f;
            ShopText(label,x,y,.28f,Color.White); y+=21f;
            new ContainerElement(new PointF(x,y),new SizeF(w,8f),Color.FromArgb(255,55,62,70)).Draw();
            float value=Math.Max(0,Math.Min(1,current));
            new ContainerElement(new PointF(x,y),new SizeF(w*value,8f),Color.FromArgb(255,220,220,225)).Draw();
            if(before.HasValue&&Math.Abs(current-before.Value)>.0001f)
            {
                float old=Math.Max(0,Math.Min(1,before.Value));
                new ContainerElement(new PointF(x+w*Math.Min(value,old),y),new SizeF(w*Math.Abs(value-old),8f),current>before.Value?Color.LimeGreen:Color.IndianRed).Draw();
            }
            y+=19f;
        }
        private void DrawShopDetails(Page page)
        {
            Function.Call(Hash.THEFEED_HIDE_THIS_FRAME);
            const float x=490f;float y=60f;
            new ContainerElement(new PointF(x,y),new SizeF(365f,510f),Color.FromArgb(235,18,20,24)).Draw();
            ShopText("PERFORMANCE / PURCHASES",x+10,y+10,.33f,Color.Orange);y+=48f;
            if(ShopService.IsGarage(_shopping))
            {
                var values=ShopPerformance.Vehicle(Shops.Car(_shopping));
                string[] names={"Top speed rating","Acceleration","Braking","Traction"};
                if(values!=null)for(int i=0;i<values.Length;i++)StatBar(names[i],values[i],Shops.PreviewPerformance==null?(float?)null:Shops.PreviewPerformance[i],ref y);
                ShopText(Shops.HasVehiclePreview?"Green: gain / red: loss. Preview is unpaid.":"Current fitted build. Cosmetic parts may add no speed.",500,y,.24f,Color.Silver);y+=35f;
            }
            else if(_shopping.Kind==ShopKind.Weapons)
            {
                uint hash=page.Selected?.StatWeapon??0;
                if(hash==0&&Shops.CustomerPed!=null&&Shops.CustomerPed.Exists())hash=Function.Call<uint>(Hash.GET_SELECTED_PED_WEAPON,Shops.CustomerPed);
                var stats=ShopPerformance.Weapon(hash);uint part=page.Selected?.StatComponent??0;
                var delta=part==0?null:ShopPerformance.Weapon(part,true);
                string[] names={"Damage","Fire rate","Capacity","Accuracy","Range"};
                if(stats!=null)
                {
                    var fitted=(int[])stats.Clone();
                    var parts=WeaponUpgrades.Parts(Shops.CustomerPed,hash).ToArray();
                    foreach(var active in parts.Where(p=>p.Active))
                    {var d=ShopPerformance.Weapon(WeaponUpgrades.Id(active),true);if(d!=null)for(int i=0;i<5;i++)fitted[i]+=d[i];}
                    // Show the selected part's native rating separately; compatible
                    // replacements can remove a different fitted attachment.
                    for(int i=0;i<5;i++)StatBar(names[i]+(delta==null?"":" | selected part "+(delta[i]>=0?"+":"")+delta[i]),fitted[i]/100f,null,ref y);
                    ShopText("Game ratings with fitted parts; selected part delta shown.",500,y,.23f,Color.Silver);y+=30f;
                }
                else {ShopText("No performance ratings for this item.",500,y,.28f,Color.Silver);y+=35f;}
            }
            ShopText("RECENT RECEIPTS",500,y,.28f,Color.Orange);y+=23f;
            foreach(var message in _shopReceipts)
            {
                foreach(var line in WrapReceipt(message,52)) {if(y>545f)return;ShopText(line,500,y,.24f,Color.White);y+=18f;}
                y+=5f;
            }
        }
        private static IEnumerable<string> WrapReceipt(string message,int width)
        {
            string line="";foreach(var word in message.Split(' '))
            {if(line.Length+word.Length+1>width){yield return line;line="";}line+=(line.Length>0?" ":"")+word;}
            if(line.Length>0)yield return line;
        }
    }
}

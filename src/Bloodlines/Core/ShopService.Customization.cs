using System;
using System.Drawing;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    public sealed partial class ShopService
    {
        public int ModIndex(ShopSite site,VehicleModType type) => Car(site)?.Mods[type].Index ?? -1;
        public string ModName(ShopSite site,VehicleModType type,int index)
        {
            if(index==-1)return "Stock";
            var car=Car(site);if(car==null)return "Option "+(index+1);
            string key=Function.Call<string>(Hash.GET_MOD_TEXT_LABEL,car,(int)type,index);
            string name=string.IsNullOrEmpty(key)?null:Function.Call<string>(Hash.GET_FILENAME_FOR_AUDIO_CONVERSATION,key);
            return string.IsNullOrEmpty(name)||name=="NULL"?"Upgrade "+(index+1):name;
        }
        public bool TogglePart(ShopSite site,VehicleToggleModType type,bool enabled) => Purchase(site,Price(site,type==VehicleToggleModType.Turbo?6000:1200),()=> {
            var car=Car(site);if(car==null)return false;car.Mods.InstallModKit();
            var mod=car.Mods[type];if(mod.IsInstalled==enabled)return false;mod.IsInstalled=enabled;return mod.IsInstalled==enabled;
        });
        public bool Tint(ShopSite site,VehicleWindowTint tint) => Purchase(site,Price(site,500),()=> {
            var car=Car(site);if(car==null||tint==VehicleWindowTint.Invalid||car.Mods.WindowTint==tint)return false;
            car.Mods.WindowTint=tint;return car.Mods.WindowTint==tint;
        });
        public bool Extra(ShopSite site,int id,bool enabled) => Purchase(site,Price(site,500),()=> {
            var car=Car(site);if(car==null||!car.ExtraExists(id)||car.IsExtraOn(id)==enabled)return false;
            car.ToggleExtra(id,enabled);return car.IsExtraOn(id)==enabled;
        });
        public bool Livery(ShopSite site,int index) => Purchase(site,Price(site,1500),()=> {
            var car=Car(site);if(car==null||index<0||index>=car.Mods.LiveryCount||car.Mods.Livery==index)return false;
            car.Mods.Livery=index;return car.Mods.Livery==index;
        });
        public bool Plate(ShopSite site,LicensePlateStyle style) => Purchase(site,Price(site,400),()=> {
            var car=Car(site);if(car==null||car.Mods.LicensePlateStyle==style)return false;
            car.Mods.LicensePlateStyle=style;return car.Mods.LicensePlateStyle==style;
        });
        public bool Neon(ShopSite site,VehicleNeonLight side,bool enabled) => Purchase(site,Price(site,800),()=> {
            var car=Car(site);if(car==null||!car.Mods.HasNeonLight(side)||car.Mods.IsNeonLightsOn(side)==enabled)return false;
            car.Mods.SetNeonLightsOn(side,enabled);return car.Mods.IsNeonLightsOn(side)==enabled;
        });
        public VehicleColor PaintColor(ShopSite site,int channel)
        {
            var car=Car(site);if(car==null)return VehicleColor.MetallicBlack;
            switch(channel){case 0:return car.Mods.PrimaryColor;case 1:return car.Mods.SecondaryColor;case 2:return car.Mods.PearlescentColor;
                case 3:return car.Mods.RimColor;case 4:return car.Mods.TrimColor;default:return car.Mods.DashboardColor;}
        }
        public bool PaintChannel(ShopSite site,int channel,VehicleColor color) => Purchase(site,Price(site,400),()=> {
            var car=Car(site);if(car==null||channel<0||channel>5)return false;
            bool custom=channel==0?car.Mods.IsPrimaryColorCustom:channel==1&&car.Mods.IsSecondaryColorCustom;
            if(!custom&&PaintColor(site,channel)==color)return false;
            switch(channel){case 0:car.Mods.ClearCustomPrimaryColor();car.Mods.PrimaryColor=color;break;
                case 1:car.Mods.ClearCustomSecondaryColor();car.Mods.SecondaryColor=color;break;
                case 2:car.Mods.PearlescentColor=color;break;case 3:car.Mods.RimColor=color;break;
                case 4:car.Mods.TrimColor=color;break;case 5:car.Mods.DashboardColor=color;break;}
            return PaintColor(site,channel)==color;
        });
        public bool Rgb(ShopSite site,int channel,Color color) => Purchase(site,Price(site,600),()=> {
            var car=Car(site);if(car==null||channel<0||channel>3)return false;
            Color before=channel==0?car.Mods.CustomPrimaryColor:channel==1?car.Mods.CustomSecondaryColor:channel==2?car.Mods.NeonLightsColor:car.Mods.TireSmokeColor;
            bool active=channel==0?car.Mods.IsPrimaryColorCustom:channel==1?car.Mods.IsSecondaryColorCustom:true;
            if(active&&before.ToArgb()==color.ToArgb())return false;
            if(channel==2&&!car.Mods.HasNeonLights)return false;
            if(channel==3&&!car.Mods[VehicleToggleModType.TireSmoke].IsInstalled)return false;
            switch(channel){case 0:car.Mods.CustomPrimaryColor=color;break;case 1:car.Mods.CustomSecondaryColor=color;break;
                case 2:car.Mods.NeonLightsColor=color;break;case 3:car.Mods.TireSmokeColor=color;break;}
            Color after=channel==0?car.Mods.CustomPrimaryColor:channel==1?car.Mods.CustomSecondaryColor:channel==2?car.Mods.NeonLightsColor:car.Mods.TireSmokeColor;
            return after.ToArgb()==color.ToArgb();
        });
        public bool XenonColor(ShopSite site,int color) => Purchase(site,Price(site,500),()=> {
            var car=Car(site);if(car==null||color < -1||color>12||!car.Mods[VehicleToggleModType.XenonHeadlights].IsInstalled)return false;
            if(Function.Call<int>(Hash.GET_VEHICLE_XENON_LIGHT_COLOR_INDEX,car)==color)return false;
            Function.Call(Hash.SET_VEHICLE_XENON_LIGHT_COLOR_INDEX,car,color);
            return Function.Call<int>(Hash.GET_VEHICLE_XENON_LIGHT_COLOR_INDEX,car)==color;
        });
        private sealed class Wheels
        {
            public int Type,Front,Rear; public bool FrontCustom,RearCustom;
            public Wheels(Vehicle car){Type=Function.Call<int>(Hash.GET_VEHICLE_WHEEL_TYPE,car);Front=Function.Call<int>(Hash.GET_VEHICLE_MOD,car,23);Rear=Function.Call<int>(Hash.GET_VEHICLE_MOD,car,24);
                FrontCustom=Function.Call<bool>(Hash.GET_VEHICLE_MOD_VARIATION,car,23);RearCustom=Function.Call<bool>(Hash.GET_VEHICLE_MOD_VARIATION,car,24);}
            public void Restore(Vehicle car){Function.Call(Hash.SET_VEHICLE_WHEEL_TYPE,car,Type);Function.Call(Hash.SET_VEHICLE_MOD,car,23,Front,FrontCustom);if(car.Model.IsBike)Function.Call(Hash.SET_VEHICLE_MOD,car,24,Rear,RearCustom);}
        }
        // Browsing wheel families must never leave an unpaid rim/axle change behind.
        public int WheelCount(ShopSite site,int family,bool rear)
        {
            var car=Car(site);if(car==null||family<0||family>12||(rear&&!car.Model.IsBike))return 0;car.Mods.InstallModKit();var before=new Wheels(car);
            try{Function.Call(Hash.SET_VEHICLE_WHEEL_TYPE,car,family);return Function.Call<int>(Hash.GET_VEHICLE_WHEEL_TYPE,car)==family?Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS,car,rear?24:23):0;}
            finally{before.Restore(car);}
        }
        public bool Wheel(ShopSite site,int family,bool rear,int index,bool custom) => Purchase(site,Price(site,1200),()=> {
            var car=Car(site);if(car==null||family<0||family>12||index < -1||(rear&&!car.Model.IsBike))return false;car.Mods.InstallModKit();var before=new Wheels(car);int axle=rear?24:23;bool applied=false;
            if(index==-1)custom=false;
            try {
                if(before.Type==family&&(rear?before.Rear:before.Front)==index&&(rear?before.RearCustom:before.FrontCustom)==custom)return false;
                Function.Call(Hash.SET_VEHICLE_WHEEL_TYPE,car,family);
                if(Function.Call<int>(Hash.GET_VEHICLE_WHEEL_TYPE,car)!=family||index>=Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS,car,axle))return false;
                // Slot 24 is hydraulics on some cars, not a rear wheel.
                if(before.Type!=family&&car.Model.IsBike)Function.Call(Hash.SET_VEHICLE_MOD,car,rear?23:24,-1,false);
                Function.Call(Hash.SET_VEHICLE_MOD,car,axle,index,custom);
                applied=Function.Call<int>(Hash.GET_VEHICLE_MOD,car,axle)==index &&
                    Function.Call<bool>(Hash.GET_VEHICLE_MOD_VARIATION,car,axle)==custom;
                return applied;
            } finally {if(!applied)before.Restore(car);}
        });
    }
}

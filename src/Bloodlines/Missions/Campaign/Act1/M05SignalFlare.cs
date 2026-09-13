using System;
using Bloodlines.Core;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Native;
namespace Bloodlines.Missions.Campaign
{
    // A signal from the player's current boat, not another destination marker.
    public sealed class BoatSignalFlare : Objective
    {
        private readonly Func<Vehicle> _boat;
        private int _requested, _fired=-1;
        public BoatSignalFlare(Func<Vehicle> boat):base("Guess: aboard the dinghy, press E / D-pad Right to fire the signal flare."){_boat=boat;}
        public override void Enter(MissionContext c){base.Enter(c);ObjectiveMarkers.Clear();_requested=Game.GameTime;Function.Call(Hash.REQUEST_WEAPON_ASSET,(uint)WeaponHash.FlareGun,31,0);}
        public override void Update(MissionContext c)
        {
            var boat=_boat();if(boat==null||!boat.Exists()||!boat.IsDriveable){Fail("The signal dinghy was lost.");return;}
            if(_fired>=0){Label="Flare away. Watch the signal rise before the approach.";if(Game.GameTime-_fired>=2500)Complete();return;}
            if(!IsOwnerActive(c)||!Game.Player.Character.IsInVehicle(boat))return;
            if(!Function.Call<bool>(Hash.HAS_WEAPON_ASSET_LOADED,(uint)WeaponHash.FlareGun))
            {if(Game.GameTime-_requested>5000)Fail("The signal flare could not load. Retry the mission.");return;}
            if(!Game.IsControlJustPressed(Control.Context))return;
            var p=boat.Position;
            Function.Call(Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS,p.X,p.Y,p.Z+3f,p.X,p.Y,p.Z+90f,0,true,(uint)WeaponHash.FlareGun,Game.Player.Character,true,false,25f);
            _fired=Game.GameTime;
        }
    }
}

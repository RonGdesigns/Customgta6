using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
namespace Bloodlines.Core
{
    public sealed partial class CrewHomes
    {
        private bool _bunkerVisit;
        private Blip _bunkerBlip;
        public bool BunkerVisit => _bunkerVisit;
        public bool BunkerUnlocked => _state.IsUnlocked(BunkerSite.Unlock);
        public Vector3? BunkerEntrance => BunkerUnlocked ? _locations.Get(BunkerSite.EntranceKey)?.Position : null;
        public void RouteBunker()
        {
            var point=BunkerEntrance;
            if(!point.HasValue){GameUtils.Notify("~y~Secure the Senora bunker in M23 first.");return;}
            Function.Call(Hash.SET_NEW_WAYPOINT,point.Value.X,point.Value.Y);
            GameUtils.Notify("Senora bunker: enter on foot at the headquarters marker.");
        }
        public void EnterBunker()
        {
            var ped=Game.Player.Character;var point=BunkerEntrance;
            if(!_crew.IsDeployed||Allowed?.Invoke()==false||!point.HasValue||Apartment.Busy||Apartment.Inside||
                ped==null||!ped.Exists()||ped.IsDead||ped.IsInVehicle()||ped.IsInCombat||Game.Player.WantedLevel>0||
                !GameUtils.IsWithin(ped.Position,point.Value,3f))
            {GameUtils.Notify("~y~Enter the secured bunker on foot between jobs, after losing the police.");return;}
            _bunkerVisit=BunkerSite.Enter(Apartment,_locations);
        }
        private void UpdateBunker(Ped ped)
        {
            var point=BunkerEntrance;
            if(!point.HasValue){ClearBunkerBlip();return;}
            if(_bunkerBlip==null||!_bunkerBlip.Exists())
            {
                // Deliberately without BunkerSite.LoadMaps(). A blip is a marker on a map and
                // needs no map data; loading it here ran the DLC registration native on the
                // first free-roam frame of every session, which is the loading screen Ron saw
                // at every startup.
                _bunkerBlip=World.CreateBlip(point.Value);
                if(_bunkerBlip!=null){_bunkerBlip.Sprite=BlipSprite.Safehouse;_bunkerBlip.Color=BlipColor.Green;_bunkerBlip.Name="Senora bunker - crew headquarters";}
            }
            else _bunkerBlip.Position=point.Value;
            // Near enough to be going there: now the geometry is worth the load.
            if(GameUtils.IsWithinFlat(ped.Position,point.Value,BunkerSite.LoadRange))BunkerSite.LoadMaps();
            if(!GameUtils.IsWithinFlat(ped.Position,point.Value,60f))return;
            GameUtils.DrawObjectiveMarker(point.Value,Color.FromArgb(130,100,210,160));
            if(ped.IsInVehicle()||!GameUtils.IsWithin(ped.Position,point.Value,3f))return;
            GameUtils.Subtitle("E / D-pad Right: enter Senora bunker headquarters.",500);
            if(Game.IsControlJustPressed(Control.Context))EnterBunker();
        }
        private void ClearBunkerBlip(){GameUtils.SafeDelete(_bunkerBlip);_bunkerBlip=null;}
    }
}

using System;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    public sealed partial class DevMenu
    {
        private int _placementOpened, _placementTick;
        private readonly ControllerNavigation _placementNavigation = new ControllerNavigation();
        public Func<bool> PlacementAllowed { get; set; }
        private Page BuildPlacementMissions()
        {
            var page = new Page("Mission placement editor");
            page.Add("Undo last saved placement",()=>_survey.CanUndoPlacement?"available":"none",()=>{if(CanEditPlacement())_survey.UndoPlacement();});
            foreach (var group in _survey.PlacementLocations.GroupBy(l=>l.Key.Split('.')[0]).OrderBy(g=>g.Key))
            {
                string mission=group.Key;
                page.Add(mission,()=>PlacementPreflight.ProgressLabel(_survey.Book,mission),()=>_stack.Push(BuildPlacementItems(mission)));
            }
            return page;
        }
        private Page BuildPlacementItems(string mission)
        {
            var page = new Page(mission+" - select a placement");
            page.Add("Survey all - visit placements in order",()=>"teleport / adjust / save / next",()=>StartPlacement(mission,false,true));
            foreach(var item in _survey.PlacementLocations.Where(l=>l.Key.StartsWith(mission+".",StringComparison.OrdinalIgnoreCase)).OrderBy(l=>l.Key))
            {
                var selected=item;
                page.Add(MissionPlacement.Label(selected),()=>MissionPlacement.HasGroup(selected.Key)?"position / radius / count":"position / facing",
                    ()=>_stack.Push(BuildPlacementActions(selected)));
            }
            return page;
        }
        private Page BuildPlacementActions(MissionLocation item)
        {
            var page=new Page(item.Key);
            page.Add("Preview / edit this position",()=>item.Status.ToString(),()=>StartPlacement(item.Key,false));
            page.Add("Place at my current position",()=>"uses vehicle position if seated",()=>StartPlacement(item.Key,true));
            page.Add("Visit this position",()=>"collision-checked teleport",()=>
            {if(!CanEditPlacement())return;StartPlacement(item.Key,false);_survey.TeleportToCurrent();});
            return page;
        }
        private bool CanEditPlacement()
        {
            if (PlacementAllowed != null && !PlacementAllowed()) { GameUtils.Notify("~y~Finish the current scene or prologue before editing mission placements."); return false; }
            if (_missions.IsRunning) { GameUtils.Notify("~y~Abort the mission before editing placements. Saved changes apply when you restart it."); return false; }
            if (_homes.Apartment.Busy || _homes.Apartment.Inside) { GameUtils.Notify("~y~Exit the apartment before editing mission placements."); return false; }
            return true;
        }
        private Page BuildPlacementSession()
        {
            var page = new Page("Placement survey - " + (_survey.Draft?.Key.Split('.')[0] ?? ""));
            page.Add("Current placement",()=>_survey.PlacementProgress,null);
            page.Add("Teleport to this spot",()=>"menu stays open",()=>_survey.TeleportToCurrent());
            // The camera is the fast way to place anything that is not at head height on
            // walkable ground: a hold forty meters up, a roost with no stair, a deck.
            page.Add("Free camera",()=>_survey.Camera.IsFlying?"flying - "+_config.SurveyCameraKey+" to land":"fly to the spot - "+_config.SurveyCameraKey,
                ()=>_survey.ToggleCamera());
            page.Add("Place at my position",()=>_survey.Camera.IsFlying?"uses the camera, dropped onto the surface":"walk to the correct spot first",()=>_survey.PlaceAtPlayer());
            page.Add("Save this placement",()=>_survey.PlacementDirty?"unsaved changes":"mark verified",()=>_survey.SavePlacement(true));
            page.Add("Save and teleport to next",()=>"capture draft, then advance",()=>_survey.SaveAndNextPlacement());
            page.Add("Next spot - keep existing",()=>"teleport without saving",()=>_survey.MovePlacement(1));
            page.Add("Previous spot",()=>"teleport back",()=>_survey.MovePlacement(-1));
            page.Add("Facing",()=>_survey.Draft?.Heading.ToString("0")+" degrees",null,d=>_survey.AdjustPlacement(0,0,d*5f));
            page.Add("Height",()=>_survey.Draft?.Position.Z.ToString("0.00")+"m",null,d=>_survey.AdjustPlacement(0,0,0,d*.1f));
            page.Add("Enemy radius",()=>_survey.Draft!=null&&MissionPlacement.HasGroup(_survey.Draft.Key)?_survey.Draft.SpawnRadius.ToString("0.0")+"m":"not a group",null,d=>_survey.AdjustPlacement(d,0,0));
            page.Add("Enemy count",()=>_survey.Draft!=null&&MissionPlacement.HasGroup(_survey.Draft.Key)?_survey.Draft.SpawnCount.ToString():"not a group",null,d=>_survey.AdjustPlacement(0,d,0));
            page.Add("Discard unsaved changes",()=>"restore this spot's saved values",()=>_survey.ResetPlacementDraft());
            page.Add("Finish survey",()=>"saved changes are kept",()=>{_survey.Stop();_stack.Pop();});
            return page;
        }
        private void StartPlacement(string key,bool here,bool tour=false)
        {
            if(!CanEditPlacement())return;
            var player=Game.Player.Character;
            if(player==null||!player.Exists()||player.IsDead)return;
            Entity source=player.CurrentVehicle!=null?(Entity)player.CurrentVehicle:player;
            var position=source.Position;float heading=source.Heading;
            _abilities.Stop();_switching.Cancel();
            if(_crew.IsDeployed)_crew.Dismiss();
            if(!(tour?_survey.BeginPlacementTour(key):_survey.BeginPlacement(key)))return;
            if(here){_survey.Draft.Position=position;_survey.Draft.Heading=heading;}
            _stack.Push(BuildPlacementSession());_openedAt=Game.GameTime;
            _placementNavigation.Reset();_placementOpened=_placementTick=Game.GameTime;
            if(tour)_survey.TeleportToCurrent();
        }
        public bool HandlePlacementKey(Keys key)
        {
            if(!_survey.IsEditing||IsOpen)return false;
            switch(key)
            {
                case Keys.Space:_survey.PlaceAtPlayer();return true;
                case Keys.Enter:_survey.SavePlacement();return true;
                case Keys.Escape:_survey.Stop();return true;
                case Keys.Up:_survey.AdjustPlacement(0,1,0);return true;
                case Keys.Down:_survey.AdjustPlacement(0,-1,0);return true;
                case Keys.Left:_survey.AdjustPlacement(0,0,-5);return true;
                case Keys.Right:_survey.AdjustPlacement(0,0,5);return true;
                case Keys.Oemplus:case Keys.Add:_survey.AdjustPlacement(1,0,0);return true;
                case Keys.OemMinus:case Keys.Subtract:_survey.AdjustPlacement(-1,0,0);return true;
                case Keys.PageUp:_survey.AdjustPlacement(0,0,0,.1f);return true;
                case Keys.PageDown:_survey.AdjustPlacement(0,0,0,-.1f);return true;
            }
            return false;
        }
        private void UpdatePlacementControls()
        {
            if(!_survey.IsEditing)return;
            if(IsOpen)
            {
                float seconds=Math.Max(0,Math.Min(.1f,(Game.GameTime-_placementTick)/1000f));_placementTick=Game.GameTime;
                if(Game.GameTime-_placementOpened>=400&&!_survey.IsTeleporting)
                    _survey.AdjustPlacement(((ControllerInput.Pressed(GTA.Control.Attack)?1f:0f)-(ControllerInput.Pressed(GTA.Control.Aim)?1f:0f))*8f*seconds,0,0);
                return;
            }
            if(_missions.IsRunning) { _survey.Stop();return; }
            Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS,0);
            foreach(var control in new[]{GTA.Control.MoveLeftRight,GTA.Control.MoveUpDown,GTA.Control.MoveUpOnly,GTA.Control.MoveDownOnly,
                GTA.Control.MoveLeftOnly,GTA.Control.MoveRightOnly,GTA.Control.LookLeftRight,GTA.Control.LookUpDown})
                Function.Call(Hash.ENABLE_CONTROL_ACTION,0,(int)control,true);
            float elapsed=Math.Max(0,Math.Min(.1f,(Game.GameTime-_placementTick)/1000f));_placementTick=Game.GameTime;
            if(Game.GameTime-_placementOpened<400||_survey.IsTeleporting)return;
            if(ControllerInput.JustPressed(GTA.Control.FrontendCancel)){_survey.Stop();return;}
            if(ControllerInput.JustPressed(GTA.Control.FrontendAccept)){_survey.SavePlacement();return;}
            if(ControllerInput.JustPressed(GTA.Control.FrontendX))_survey.PlaceAtPlayer();
            float growth=(ControllerInput.Pressed(GTA.Control.Attack)?1f:0f)-(ControllerInput.Pressed(GTA.Control.Aim)?1f:0f);
            _survey.AdjustPlacement(growth*8f*elapsed,0,0);
            var d=_placementNavigation.Update(0,0,Game.GameTime,ControllerInput.Pressed(GTA.Control.FrontendUp),ControllerInput.Pressed(GTA.Control.FrontendDown),
                ControllerInput.Pressed(GTA.Control.FrontendLeft),ControllerInput.Pressed(GTA.Control.FrontendRight));
            if(d==MenuDirection.Up)_survey.AdjustPlacement(0,1,0);
            if(d==MenuDirection.Down)_survey.AdjustPlacement(0,-1,0);
            if(d==MenuDirection.Left)_survey.AdjustPlacement(0,0,-5);
            if(d==MenuDirection.Right)_survey.AdjustPlacement(0,0,5);
        }
    }
}

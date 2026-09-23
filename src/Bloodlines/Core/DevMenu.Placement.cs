using System;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    public sealed partial class DevMenu
    {
        /// <summary>A mission's world, staged and standing still. Owned here, ticked by the host.</summary>
        public ScenePreview Preview { get; } = new ScenePreview();

        /// <summary>
        /// Stage a mission, then open its survey with the camera already up. The three
        /// things he needs to fix a placement arrive together: the world as the mission
        /// builds it, the list of that mission's keys, and a way to fly to them.
        /// </summary>
        private void StagePreview(string missionId)
        {
            if (_missions.IsRunning) { GameUtils.Notify("~y~Finish or abort the running mission first."); return; }
            var definition = _catalog.All.FirstOrDefault(d => string.Equals(d.Id, missionId, StringComparison.OrdinalIgnoreCase));
            if (definition == null) { GameUtils.Notify("~r~No mission script for " + missionId + "."); return; }
            if (!Preview.Open(definition, _missions.Context, _survey.Book, _survey.OutputDirectory))
            {
                GameUtils.Notify("~r~" + (Preview.Refusal ?? "The scene could not be staged."));
                return;
            }
            GameUtils.Notify("~g~" + missionId + " staged.~s~ Nothing is running. " +
                (Preview.ReportPath != null ? "Wrote Bloodlines.Staging.txt." : ""));
            StartPlacement(missionId, false, true);
            if (!_survey.Camera.IsFlying) _survey.ToggleCamera();
        }

        /// <summary>
        /// Send the survey through a mission's keys on its own and report what is there. It
        /// opens the ordinary survey, so his usual keys still work: capture takes over from
        /// the sweep at any point, and Escape stops both.
        /// </summary>
        private void StartSweep(string mission)
        {
            if (!CanEditPlacement()) return;
            if (_missions.IsRunning) { GameUtils.Notify("~y~Finish or abort the running mission first."); return; }
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;
            _abilities.Stop(); _switching.Cancel();
            if (_crew.IsDeployed) _crew.Dismiss();
            if (!_survey.BeginSweep(mission)) { GameUtils.Notify("~r~" + mission + " has no placements to check."); return; }
            if (!_survey.Camera.IsFlying) _survey.ToggleCamera();
            Close();
        }

        private int _placementOpened, _placementTick;
        private readonly ControllerNavigation _placementNavigation = new ControllerNavigation();
        public Func<bool> PlacementAllowed { get; set; }
        private Page BuildPlacementMissions()
        {
            var page = new Page("Mission placement editor");
            page.Add("Undo last saved placement",()=>_survey.CanUndoPlacement?"available":"none",()=>{if(CanEditPlacement())_survey.UndoPlacement();});
            page.Add("Close staging preview",()=>Preview.IsActive?"open: "+Preview.MissionId:"nothing staged",()=>Preview.Close());
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
            // Stage the whole mission and look at it, instead of checking one key at a
            // time by playing the mission that uses it.
            page.Add("Stage this mission's world",()=>Preview.IsActive&&Preview.MissionId==mission?"staged - select again to restage":"spawn it all, run nothing",
                ()=>StagePreview(mission));
            page.Add("Survey all - visit placements in order",()=>"teleport / adjust / save / next",()=>StartPlacement(mission,false,true));
            // Anything the mission does not already spawn: more men, a vehicle with men in
            // it, a prop. Opens the survey at this mission's first spot with the camera up.
            page.Add("Add enemies, vehicles, props",()=>MissionAdditions.For(mission).Count+" placed",()=>
            {StartPlacement(mission,false,true);if(_survey.IsEditing)OpenAdditions(mission);});
            // The measurement behind these two rows: 1,062 keys are estimates and most
            // missions have about nine. Flying to all nine to find the two that are wrong is
            // the slow half, so the tool visits them and says which two.
            page.Add("Check every spot in this mission",()=>_survey.SweepProgress,()=>StartSweep(mission));
            page.Add("Accept every spot that checked out",()=>_survey.SweepProgress,()=>_survey.AcceptClean());
            foreach(var item in _survey.PlacementLocations.Where(l=>l.Key.StartsWith(mission+".",StringComparison.OrdinalIgnoreCase)).OrderBy(l=>l.Key))
            {
                var selected=item;
                page.Add(MissionPlacement.Label(selected),()=>MissionPlacement.HasRadius(selected.Key)?"position / radius / count"
                    :MissionPlacement.HasGroup(selected.Key)?"position / facing / count":"position / facing",
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
            var page = new Page("Placement survey - " + (_survey.Draft?.Key.Split('.')[0] ?? "")) { Tag = PlacementTag,
                Hint = "Y: save | X: place here | sticks: fly | LT/RT: down/up" };
            // Ron, September 22: saving was the tenth row of eighteen, so every placement
            // ended with a long scroll down to it. The rows he uses on every spot come first,
            // in the order he uses them - look, place, save - and Y saves from anywhere on
            // the page. The fine adjustments and the stand-in come after.
            page.Add("Current placement",()=>_survey.PlacementProgress+(_survey.PlacementDirty?"  (unsaved)":""),null);
            page.Add("Save this placement",()=>_survey.PlacementDirty?"unsaved changes - Y":"mark verified - Y",()=>_survey.SavePlacement(true));
            page.Add("Save and teleport to next",()=>"capture draft, then advance",()=>_survey.SaveAndNextPlacement());
            page.Add("Place at my position",()=>_survey.Camera.IsFlying?"uses the camera, dropped onto the surface - X":"walk to the correct spot first - X",()=>_survey.PlaceAtPlayer());
            // The camera is the fast way to place anything that is not at head height on
            // walkable ground: a hold forty meters up, a roost with no stair, a deck.
            page.Add("Free camera",()=>_survey.Camera.IsFlying?"flying - "+_config.SurveyCameraKey+" to land":"fly to the spot - "+_config.SurveyCameraKey,
                ()=>_survey.ToggleCamera());
            page.Add("Teleport to this spot",()=>"menu stays open",()=>_survey.TeleportToCurrent());
            page.Add("Next spot - keep existing",()=>"teleport without saving",()=>_survey.MovePlacement(1));
            page.Add("Previous spot",()=>"teleport back",()=>_survey.MovePlacement(-1));
            page.Add("Add enemies, vehicles, props",()=>{var m=_survey.Draft?.Key.Split('.')[0];return m==null?"no mission":MissionAdditions.For(m).Count+" placed in "+m;},
                ()=>{var m=_survey.Draft?.Key.Split('.')[0];if(m!=null)OpenAdditions(m);});
            page.Add("Facing",()=>_survey.Draft?.Heading.ToString("0")+" degrees",null,d=>_survey.AdjustPlacement(0,0,d*5f));
            page.Add("Height",()=>_survey.Draft?.Position.Z.ToString("0.00")+"m",null,d=>_survey.AdjustPlacement(0,0,0,d*.1f));
            // The old answer, 'not a group', was a dead end: it named a state without saying what the key
            // was instead, so a row that did nothing looked like a broken row. These say
            // which of the three a key actually is - a detail he can size, a detail whose
            // shape belongs to the mission, or one man standing at a point.
            page.Add("Enemy count",()=>_survey.Draft==null?"no draft"
                :MissionPlacement.HasGroup(_survey.Draft.Key)?_survey.Draft.SpawnCount+" men"
                :"one man - add more with Add enemies",null,d=>_survey.AdjustPlacement(0,d,0));
            page.Add("Enemy radius",()=>_survey.Draft==null?"no draft"
                :MissionPlacement.HasRadius(_survey.Draft.Key)?_survey.Draft.SpawnRadius.ToString("0.0")+"m"
                :MissionPlacement.HasGroup(_survey.Draft.Key)?"the mission lays this detail out"
                :"one man at this point",null,d=>_survey.AdjustPlacement(d,0,0));
            page.Add("Stand-in",()=>_survey.Ghost.IsShowing?_survey.Ghost.Describe:"off",()=>_stack.Push(BuildStandInPage()));
            page.Add("Accept this spot as correct",()=>_survey.LastReading!=null&&!_survey.LastReading.Fits?"it has not checked out":"keep the coordinates, mark verified",
                ()=>_survey.AcceptCurrent());
            page.Add("Discard unsaved changes",()=>"restore this spot's saved values",()=>_survey.ResetPlacementDraft());
            page.Add("Finish survey",()=>"saved changes are kept",()=>{_survey.Stop();_stack.Pop();});
            return page;
        }

        /// <summary>The flown stand-in's rows, on a page of their own so the survey page stays short.</summary>
        private Page BuildStandInPage()
        {
            var page = new Page("Stand-in") { Tag = PlacementTag, Hint = "Y: save | X: place here | sticks: fly | LT/RT: down/up" };
            // Fly the thing itself into place rather than the empty point.
            page.Add("Place from the stand-in",()=>_survey.Ghost.IsShowing?"write where it is standing":"show the stand-in first",()=>_survey.PlaceAtGhost());
            page.Add("Stand-in",()=>_survey.Ghost.IsShowing?_survey.Ghost.Describe+" - "+_survey.Ghost.Standoff.ToString("0")+"m out":"off - select to show",
                ()=>_survey.ToggleGhost(),d=>{if(_survey.Ghost.IsShowing)_survey.Ghost.Cycle(d);});
            page.Add("Stand-in distance",()=>_survey.Ghost.IsShowing?_survey.Ghost.Standoff.ToString("0")+" m":"no stand-in",null,d=>_survey.Ghost.PushOut(d));
            page.Add("Stand-in facing",()=>_survey.Ghost.IsShowing?_survey.Ghost.Heading.ToString("0")+" degrees":"no stand-in",null,d=>_survey.Ghost.Turn(d*5f));
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

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;

namespace Bloodlines.Core
{
    public sealed partial class DevMenu
    {
        private const string PlacementTag = "placement", AdditionsTag = "additions";

        /// <summary>The choices on the "Add to this mission" page.</summary>
        public AdditionEditor Additions { get; } = new AdditionEditor();
        private int _additionPreviewShown = -1;

        /// <summary>
        /// Open the page that puts extra men, vehicles and props into a mission. The camera
        /// comes up with it, because the camera is how the spot is chosen, and the stand-in
        /// becomes the actual thing being placed.
        /// </summary>
        private void OpenAdditions(string mission)
        {
            if (!_survey.IsActive) { GameUtils.Notify("~y~Open the mission's survey first."); return; }
            Additions.Open(mission);
            _additionPreviewShown = -1;
            if (!_survey.Camera.IsFlying) _survey.ToggleCamera();
            _stack.Push(BuildAdditionsPage());
        }

        private Page BuildAdditionsPage()
        {
            string mission = Additions.Mission;
            var page = new Page("Add to " + mission) { Tag = AdditionsTag,
                Hint = "X: drop it here | Y: next model | sticks: fly | LT/RT: down/up | LB/RB: turn view" };
            page.Add("Drop it here",()=>Additions.Describe()+" - X",DropAddition);
            page.Add("What",()=>Additions.Kind==AdditionKind.Enemy?"enemies on foot":Additions.Kind==AdditionKind.Vehicle?"a vehicle with men in it":"a prop",
                ()=>Additions.CycleKind(1),d=>Additions.CycleKind(d));
            page.Add("Model",()=>MissionAdditions.Label(Additions.PreviewModel)+" - Y",()=>Additions.CycleModel(1),d=>Additions.CycleModel(d));
            page.Add("Crew",()=>Additions.Kind==AdditionKind.Vehicle?MissionAdditions.Label(Additions.Man):"only for a vehicle",null,
                d=>{if(Additions.Kind==AdditionKind.Vehicle)Additions.CycleCrew(d);});
            page.Add("How many",()=>Additions.Kind==AdditionKind.Prop?"one prop":Additions.Count+(Additions.Kind==AdditionKind.Vehicle?" aboard":" men"),null,
                d=>Additions.AdjustCount(d));
            page.Add("Spread",()=>Additions.Kind==AdditionKind.Enemy?Additions.Radius.ToString("0")+" m":"only for enemies on foot",null,
                d=>Additions.AdjustRadius(d));
            page.Add("Weapon",()=>Additions.Kind==AdditionKind.Prop?"none":Additions.Weapon,null,d=>Additions.CycleWeapon(d));
            page.Add("Side",()=>Additions.Kind==AdditionKind.Prop?"none":Additions.Side,null,d=>Additions.CycleSide(d));
            page.Add("Orders",()=>Additions.Kind==AdditionKind.Prop?"none":Additions.Patrol?(Additions.Kind==AdditionKind.Vehicle?"drive around":"patrol the area"):"hold this spot",
                ()=>Additions.TogglePatrol(),d=>Additions.TogglePatrol());
            page.Add("Facing",()=>_survey.Ghost.IsShowing?_survey.Ghost.Heading.ToString("0")+" degrees":"fly the camera first",null,d=>_survey.Ghost.Turn(d*15f));
            page.Add("Distance from camera",()=>_survey.Ghost.IsShowing?_survey.Ghost.Standoff.ToString("0")+" m":"fly the camera first",null,d=>_survey.Ghost.PushOut(d*2f));
            page.Add("Free camera",()=>_survey.Camera.IsFlying?"flying":"down - select to fly",()=>{_survey.ToggleCamera();_additionPreviewShown=-1;});
            page.Add("Placed in "+mission,()=>Additions.Placed.Count+" / "+MissionAdditions.MaxPerMission,()=>_stack.Push(BuildPlacedAdditions()));
            page.Add("Done",()=>"back to the survey",()=>{CloseAdditions();_stack.Pop();});
            return page;
        }

        private Page BuildPlacedAdditions()
        {
            var page = new Page("Placed in " + Additions.Mission) { Tag = AdditionsTag };
            var placed = Additions.Placed;
            if (placed.Count == 0) page.Add("Nothing placed yet",()=>"drop something first",null);
            foreach (var item in placed)
            {
                var selected = item;
                page.Add(selected.Id+"  "+(selected.Kind==AdditionKind.Prop?"prop":selected.Kind==AdditionKind.Vehicle?"vehicle":"enemies"),
                    ()=>selected.Summary,()=>_stack.Push(BuildPlacedAddition(selected)));
            }
            return page;
        }

        private Page BuildPlacedAddition(Addition item)
        {
            var page = new Page(item.Mission + " " + item.Id) { Tag = AdditionsTag };
            page.Add("Go to it",()=>item.Summary,()=>
            {
                if (_survey.Camera.IsFlying) _survey.Camera.MoveTo(item.Position);
                else GameUtils.Notify("~y~Fly the camera to look at it.");
            });
            page.Add("Move it to the stand-in",()=>_survey.Ghost.IsShowing?"write where the stand-in is":"fly the camera first",()=>
            {
                if (!DropPoint(item.Kind, item.Vehicle, out var at, out float heading)) return;
                GameUtils.Notify(MissionAdditions.Move(item, at, heading) ? "~g~Moved " + item.Id + "." : "~r~Could not save the move. See Bloodlines.log.");
            });
            page.Add("Remove it",()=>"deleted from the file at once",()=>
            {
                if (!MissionAdditions.Remove(item)) { GameUtils.Notify("~r~Could not save the removal. See Bloodlines.log."); return; }
                GameUtils.Notify("~y~Removed " + item.Id + " from " + item.Mission + ".");
                _stack.Pop(); if (_stack.Count > 0 && _stack.Peek().Title.StartsWith("Placed in ")) { _stack.Pop(); _stack.Push(BuildPlacedAdditions()); }
            });
            return page;
        }

        /// <summary>Put down what the page describes, where the stand-in is, and save it.</summary>
        private void DropAddition()
        {
            if (!Additions.IsOpen) return;
            if (Additions.Placed.Count >= MissionAdditions.MaxPerMission)
            { GameUtils.Notify("~y~" + Additions.Mission + " already has " + MissionAdditions.MaxPerMission + " additions. Remove one first."); return; }
            if (!DropPoint(Additions.Kind, Additions.VehicleModel, out var at, out float heading)) return;
            var added = Additions.Drop(at, heading);
            if (added == null) { GameUtils.Notify("~r~Not saved. See Bloodlines.log."); return; }
            Logger.Info("Placed addition " + added.Mission + "." + added.Id + ": " + added.Summary + " at " + at + ", heading " + heading.ToString("0"));
            GameUtils.Notify("~g~Added " + added.Id + " to " + added.Mission + ":~s~ " + added.Summary + ". It spawns the next time the mission starts.");
        }

        /// <summary>
        /// Where a drop lands. From the stand-in while flying: a land thing onto the surface
        /// under it, a boat onto the water. Otherwise where he is standing or sitting.
        /// </summary>
        private bool DropPoint(AdditionKind kind, string vehicle, out Vector3 at, out float heading)
        {
            at = Vector3.Zero; heading = 0f;
            bool boat = kind == AdditionKind.Vehicle && MissionAdditions.IsBoat(vehicle);
            if (_survey.Camera.IsFlying && _survey.Ghost.IsShowing)
            {
                at = _survey.Ghost.Commit(boat ? "water" : "land", out float _);
                heading = _survey.Ghost.Heading;
                if (boat)
                {
                    var height = new OutputArgument();
                    if (Function.Call<bool>(Hash.GET_WATER_HEIGHT, at.X, at.Y, at.Z + 20f, height)) at = new Vector3(at.X, at.Y, height.GetResult<float>());
                }
                return true;
            }
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return false;
            Entity source = player.CurrentVehicle != null ? (Entity)player.CurrentVehicle : player;
            at = source.Position; heading = source.Heading;
            return true;
        }

        private void CloseAdditions()
        {
            Additions.Close();
            if (_survey.Ghost.Adopted != null) _survey.Ghost.Hide();
            _additionPreviewShown = -1;
        }

        /// <summary>
        /// Every frame: keep the stand-in the model the page describes, draw what is already
        /// placed, and drop the page's state if it has gone away underneath us.
        /// </summary>
        private void UpdateAdditions()
        {
            if (!Additions.IsOpen) return;
            bool onPage = IsOpen && _stack.Count > 0 && _stack.Peek().Tag == AdditionsTag;
            if (!_survey.IsActive || (IsOpen && _stack.Count > 0 && !_stack.Any(p => p.Tag == AdditionsTag)))
            { CloseAdditions(); return; }
            if (onPage && _survey.Camera.IsFlying && _additionPreviewShown != Additions.PreviewVersion)
            {
                _additionPreviewShown = Additions.PreviewVersion;
                float facing = _survey.Ghost.IsShowing ? _survey.Ghost.Heading : (_survey.Camera.Heading + 180f) % 360f;
                var preview = CreatePreview(Additions.Kind, Additions.PreviewModel);
                if (preview == null || !_survey.Ghost.Adopt(preview, Additions.PreviewModel, facing))
                    GameUtils.Notify("~o~" + Additions.PreviewModel + " would not load as a stand-in.~s~ Try the next model.");
            }
            DrawPlaced();
        }

        private static Entity CreatePreview(AdditionKind kind, string name)
        {
            var model = new Model(name);
            try
            {
                if (!model.IsInCdImage || !model.IsValid || !GameUtils.RequestModel(model)) return null;
                var at = GameplayCamera.Position;
                if (kind == AdditionKind.Prop) return World.CreateProp(model, at, false, false);
                if (kind == AdditionKind.Vehicle) return World.CreateVehicle(model, at, 0f);
                return World.CreatePed(model, at, 0f);
            }
            catch (Exception ex) { Logger.Error("Creating an addition stand-in for " + name, ex); return null; }
            finally { model.MarkAsNoLongerNeeded(); }
        }

        /// <summary>What is already placed in this mission, drawn where it will spawn, with its id over it.</summary>
        private void DrawPlaced()
        {
            var viewer = _survey.Camera.IsFlying ? _survey.Camera.Position : (Game.Player.Character?.Position ?? Vector3.Zero);
            foreach (var a in Additions.Placed)
            {
                if (a.Position.DistanceTo(viewer) > 250f) continue;
                var color = a.Kind == AdditionKind.Prop ? Color.FromArgb(170, 90, 170, 255)
                    : a.Kind == AdditionKind.Vehicle ? Color.FromArgb(170, 255, 150, 40) : Color.FromArgb(170, 235, 70, 60);
                if (a.Kind == AdditionKind.Enemy)
                    for (int i = 0; i < a.Count; i++) GameUtils.DrawObjectiveMarker(MissionAdditions.Spread(a.Position, a.Radius, i, a.Count), color, .4f);
                else GameUtils.DrawObjectiveMarker(a.Position, color, a.Kind == AdditionKind.Vehicle ? 1.6f : .8f);
                double angle = a.Heading * Math.PI / 180;
                Function.Call(Hash.DRAW_LINE, a.Position.X, a.Position.Y, a.Position.Z + .5f,
                    a.Position.X - (float)Math.Sin(angle) * 2.5f, a.Position.Y + (float)Math.Cos(angle) * 2.5f, a.Position.Z + .5f, 70, 220, 255, 255);
                try
                {
                    var screen = GTA.UI.Screen.WorldToScreen(a.Position + new Vector3(0f, 0f, 1.6f));
                    if (screen != PointF.Empty) new TextElement(a.Id + "  " + a.Summary, screen, .26f, Color.White) { Alignment = Alignment.Center, Outline = true }.Draw();
                }
                catch { }
            }
        }

        /// <summary>
        /// The pad's spare face buttons, on the pages where they save him a scroll. The menu
        /// already uses the d-pad, A and B; X and Y did nothing anywhere in it.
        /// </summary>
        private void HandlePageShortcuts()
        {
            if (!IsOpen || _stack.Count == 0 || Game.GameTime == _openedAt) return;
            if (Function.Call<bool>(Hash.IS_USING_KEYBOARD_AND_MOUSE, 2)) return;
            var tag = _stack.Peek().Tag;
            if (tag == AdditionsTag && Additions.IsOpen)
            {
                if (ControllerInput.JustPressed(GTA.Control.FrontendX)) DropAddition();
                else if (ControllerInput.JustPressed(GTA.Control.FrontendY)) Additions.CycleModel(1);
            }
            else if (tag == PlacementTag && _survey.IsEditing)
            {
                if (ControllerInput.JustPressed(GTA.Control.FrontendY)) _survey.SavePlacement(true);
                else if (ControllerInput.JustPressed(GTA.Control.FrontendX)) _survey.PlaceAtPlayer();
            }
        }

        /// <summary>The keyboard's versions on the additions page: Space drops, Tab loads the next model.</summary>
        private bool HandlePageKey(Keys key)
        {
            if (_stack.Count == 0) return false;
            var tag = _stack.Peek().Tag;
            if (tag == AdditionsTag && Additions.IsOpen)
            {
                if (key == Keys.Space) { DropAddition(); return true; }
                if (key == Keys.Tab) { Additions.CycleModel(1); return true; }
            }
            return false;
        }
    }
}

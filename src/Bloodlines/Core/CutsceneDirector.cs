using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Small, skippable in-engine scenes. No mission timers run during a briefing.</summary>
    public sealed class CutsceneDirector
    {
        private sealed class HeldEntity
        {
            public Entity Entity;
            public bool Frozen, Invincible;
        }

        private readonly CrewRoster _crew;
        private readonly DialogueDirector _dialogue;
        private readonly LocationBook _locations;
        private readonly Dictionary<string, List<DialogueCue>> _scenes = new Dictionary<string, List<DialogueCue>>();
        private readonly Dictionary<CrewSlot, Ped> _actors = new Dictionary<CrewSlot, Ped>();
        private readonly Dictionary<string, Ped> _support = new Dictionary<string, Ped>();
        private readonly List<Entity> _temporary = new List<Entity>();
        private bool _dockIntro, _radioScene;
        private Ped _actionActor;
        private Action _sceneAction;
        private bool _actionStarted;
        private readonly List<HeldEntity> _held = new List<HeldEntity>();
        private Camera _camera, _previousCamera;
        private List<DialogueCue> _lines;
        private int _index, _startedAt;
        private bool _hadControl;
        private string _title;
        public static bool IsSceneRunning { get; private set; }
        public bool IsActive => _lines != null;

        public CutsceneDirector(CrewRoster crew, DialogueDirector dialogue, LocationBook locations, string dataDirectory)
        {
            _crew = crew;
            _dialogue = dialogue;
            _locations = locations;
            foreach (var row in DataTable.Load(Path.Combine(dataDirectory, "scenes.tsv")).Rows)
            {
                string key = row.Text("mission") + ":" + row.Text("phase");
                if (!_scenes.TryGetValue(key, out var lines)) _scenes[key] = lines = new List<DialogueCue>();
                lines.Add(new DialogueCue { CueId = row.Text("cue_id"), MissionId = row.Text("mission"),
                    Speaker = row.Text("speaker"), Line = row.Text("line"), Direction = row.Text("direction") });
            }
        }

        public bool Play(string missionId, string phase, string title, Ped actionActor = null, Action sceneAction = null)
        {
            if (IsActive || !_scenes.TryGetValue(missionId + ":" + phase, out var lines) || lines.Count == 0) return false;
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return false;
            _lines = lines;
            IsSceneRunning = true;
            _index = 0;
            _startedAt = Game.GameTime;
            _title = title;
            _dockIntro = missionId == "M01" && phase == "intro";
            _radioScene = false;
            _actionActor = actionActor;
            _sceneAction = sceneAction;
            _actionStarted = false;
            _hadControl = Game.Player.CanControlCharacter;
            try
            {
                _previousCamera = World.RenderingCamera;
                // GET_RENDERING_CAM can return an invalid handle wrapped as a Camera.
                if (_previousCamera != null && !_previousCamera.Exists()) _previousCamera = null;
                if (missionId == "M01" && phase == "intro" && !ProloguePlacement.Prepare(_locations))
                    throw new InvalidOperationException("M01 dock surfaces are not ready. Move near its start marker and retry.");
                Logger.Info("Scene started: " + missionId + ":" + phase);
                _dialogue.Clear();
                Hold(player);
                Hold(player.CurrentVehicle);
                Game.Player.CanControlCharacter = false;
                // Briefings use temporary cast before mission setup. The M01 cold open
                // shows three separate jobs; they must not meet before recognition.
                foreach (var protagonist in Protagonist.All.Where(p => lines.Any(l => l.Speaker.Equals(p.Handle, StringComparison.OrdinalIgnoreCase))))
                {
                    Ped actor = _dockIntro ? null : _crew.PedFor(protagonist.Slot);
                    if (actor != null && (!actor.Exists() || actor.IsDead)) actor = null;
                    if (actor == null && !_dockIntro) { _radioScene = true; continue; }
                    if (actor == null && _dockIntro)
                    {
                        var position = player.Position + new Vector3((int)protagonist.Slot * 1.8f - 1.8f, 3f, 0f);
                        if (missionId == "M01")
                            position = _locations.Position(protagonist.Slot == CrewSlot.Ice ? "M01.IceApproach" :
                                protagonist.Slot == CrewSlot.Gohan ? "M01.GohanApproach" : "M01.GuessApproach");
                        var model = protagonist.Model;
                        if (GameUtils.RequestModel(model, 1000))
                        {
                            actor = World.CreatePed(model, position, player.Heading + 180f);
                            model.MarkAsNoLongerNeeded();
                            if (actor != null && actor.Exists())
                            {
                                CrewAppearance.Apply(actor, protagonist.Slot);
                                _temporary.Add(actor);
                                actor.IsPersistent = true;
                                actor.BlockPermanentEvents = true;
                                if (missionId == "M01" && protagonist.Slot == CrewSlot.Gohan)
                                    actor.Task.StartScenario("WORLD_HUMAN_STAND_MOBILE", position, actor.Heading);
                                else if (_dockIntro && protagonist.Slot == CrewSlot.Guess)
                                {
                                    var car = SceneVehicle("primo", position, _locations.Heading("M01.GuessApproach"));
                                    if (car != null) actor.SetIntoVehicle(car, VehicleSeat.Driver);
                                    else actor.Task.StandStill(-1);
                                }
                                else actor.Task.StandStill(-1);
                            }
                        }
                    }
                    if (actor == null || !actor.Exists()) continue;
                    _actors[protagonist.Slot] = actor;
                    Hold(actor);
                    Hold(actor.CurrentVehicle);
                }
                if (!_dockIntro)
                {
                    foreach (var a in _actors.Values)
                        foreach (var b in _actors.Values)
                            if (a.Position.DistanceTo(b.Position) > 18f) _radioScene = true;
                }
                if (_dockIntro)
                {
                    SceneVehicle("schafter3", _locations.Position("M01.PrototypeCar"), _locations.Heading("M01.PrototypeCar"));
                    var bossModel = new Model("g_m_m_mexboss_01");
                    if (GameUtils.RequestModel(bossModel, 1000))
                    {
                        var boss = World.CreatePed(bossModel, _locations.Position("M01.CapoSpawn"), 90f);
                        bossModel.MarkAsNoLongerNeeded();
                        if (boss != null && boss.Exists())
                        {
                            _temporary.Add(boss); _support["MATEO"] = boss; boss.BlockPermanentEvents = true;
                            boss.Task.StartScenario("WORLD_HUMAN_CLIPBOARD", boss.Position, boss.Heading); Hold(boss);
                        }
                    }
                    var terminal = _locations.Position("M01.ServiceTerminal");
                    SceneProp("prop_table_03", terminal, true);
                    SceneProp("prop_laptop_01a", terminal + new Vector3(0f, 0f, .82f), false);
                    var workerModel = new Model("s_m_m_dockwork_01");
                    if (GameUtils.RequestModel(workerModel, 1000))
                    {
                        try
                        {
                            if (ProloguePlacement.TryLand(_locations.Position("M01.CapoSpawn") + new Vector3(2f, -2f, 0), out var point))
                            {
                                var worker = World.CreatePed(workerModel, point, 270f);
                                if (worker != null && worker.Exists())
                                { _temporary.Add(worker); worker.BlockPermanentEvents = true; worker.Task.StartScenario("WORLD_HUMAN_CLIPBOARD", point, 270f); Hold(worker); }
                            }
                        }
                        finally { workerModel.MarkAsNoLongerNeeded(); }
                    }
                }
                if (phase == "intro" && lines.Any(l => l.Speaker == "KJ"))
                {
                    var model = new Model("a_m_y_stbla_02");
                    if (GameUtils.RequestModel(model, 1000))
                    {
                        var host = _actors.TryGetValue(CrewSlot.Guess, out var guess) ? guess : player;
                        var kj = World.CreatePed(model, host.Position + new Vector3(3f, 3f, 0f), host.Heading + 180f);
                        model.MarkAsNoLongerNeeded();
                        if (kj != null && kj.Exists())
                        {
                            _temporary.Add(kj); _support["KJ"] = kj;
                            kj.IsPersistent = true; kj.BlockPermanentEvents = true;
                            kj.Task.StandStill(-1); Hold(kj);
                        }
                    }
                }
                // Hide the ordinary player during the temporary-cast briefing with
                // framing, without altering its visibility or position.
                _camera = World.CreateCamera(player.Position + new Vector3(0, -3, 2), Vector3.Zero, 48f);
                if (_camera == null || !_camera.Exists()) throw new InvalidOperationException("Camera creation failed.");
                World.RenderingCamera = _camera;
                NextLine();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Scene setup failed: " + missionId + ":" + phase, ex);
                Stop();
                return false;
            }
        }

        /// <summary>A short camera observation of an action already happening in the world.
        /// Skipping changes presentation only; no actor is moved or event postponed.</summary>
        public bool PlayMoment(string missionId, string title, string speaker, string line, Ped actor)
        {
            if (IsActive || actor == null || !actor.Exists()) return false;
            _scenes[missionId + ":moment"] = new List<DialogueCue> { new DialogueCue {
                CueId = missionId + "_MOMENT", MissionId = missionId, Speaker = speaker, Line = line } };
            return Play(missionId, "moment", title, actor, () => { });
        }

        private void Hold(Entity entity)
        {
            if (entity == null || !entity.Exists() || _held.Any(h => h.Entity.Handle == entity.Handle)) return;
            _held.Add(new HeldEntity { Entity = entity, Frozen = entity.IsPositionFrozen, Invincible = entity.IsInvincible });
            entity.IsPositionFrozen = true;
            entity.IsInvincible = true;
        }

        private Vehicle SceneVehicle(string name, Vector3 position, float heading)
        {
            var model = new Model(name);
            if (!GameUtils.RequestModel(model, 1000)) return null;
            var vehicle = World.CreateVehicle(model, position, heading);
            model.MarkAsNoLongerNeeded();
            if (vehicle == null || !vehicle.Exists()) return null;
            _temporary.Add(vehicle); vehicle.IsPersistent = true; Hold(vehicle);
            return vehicle;
        }

        private void SceneProp(string name, Vector3 point, bool ground)
        {
            var model = new Model(name);
            try
            {
                if (!GameUtils.RequestModel(model, 1000)) return;
                var prop = World.CreateProp(model, point, false, ground);
                if (prop == null || !prop.Exists()) return;
                _temporary.Add(prop); Hold(prop);
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }

        private void NextLine()
        {
            if (_index >= _lines.Count) { Stop(); return; }
            var cue = _lines[_index++];
            Ped actor = null;
            if (Enum.TryParse(cue.Speaker, true, out CrewSlot slot)) _actors.TryGetValue(slot, out actor);
            if (actor == null) _support.TryGetValue(cue.Speaker, out actor);
            // Solo aftermath voices come over radio; never invent an off-screen companion.
            if (actor == null || !actor.Exists()) actor = Game.Player.Character;
            if (_radioScene && !_dockIntro && actor != null && actor.Position.DistanceTo(Game.Player.Character.Position) > 18f) actor = Game.Player.Character;
            if (actor != null && actor.Exists())
            {
                var facing = actor.ForwardVector;
                // A side window view keeps seated actors in context without clearing their tasks.
                var offset = actor.IsInVehicle() ? new Vector3(facing.Y * 3.8f, -facing.X * 3.8f, 1.1f)
                    : facing * 2.8f + new Vector3(0.8f, 0, 1.1f);
                _camera.Position = actor.Position + offset;
                _camera.PointAt(actor.Position + new Vector3(0, 0, 0.75f));
                Function.Call(Hash.SET_FOCUS_POS_AND_VEL, actor.Position.X, actor.Position.Y, actor.Position.Z, 0f, 0f, 0f);
            }
            if (_dockIntro && (_index == 1 || _index == 5 || _index == 7))
            {
                var point = _locations.Position(_index == 1 ? "M01.RegroupPoint" : _index == 5 ? "M01.PrototypeCar" : "M01.CapoSpawn");
                _camera.Position = point + (_index == 1 ? new Vector3(25f, 24f, 18f) : new Vector3(8f, -10f, 5f));
                _camera.PointAt(point + new Vector3(0f, 0f, 1f));
                Function.Call(Hash.SET_FOCUS_POS_AND_VEL, point.X, point.Y, point.Z, 0f, 0f, 0f);
            }
            _dialogue.Play(cue);
            if (_index == _lines.Count && _sceneAction != null && !_actionStarted)
            {
                _sceneAction();
                _actionStarted = true;
            }
        }

        public void Update()
        {
            if (!IsActive) return;
            try
            {
                // A bad cue, missing actor, or another mod taking the player cannot strand control.
                if (Game.GameTime - _startedAt > 240000 || Game.Player.Character == null || Game.Player.Character.IsDead)
                { Stop(); return; }
                Game.DisableControlThisFrame(GTA.Control.CharacterWheel);
                Game.DisableControlThisFrame(GTA.Control.Attack);
                Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
                new GTA.UI.ContainerElement(new PointF(640, 30), new SizeF(1280, 60), Color.Black).Draw();
                new GTA.UI.TextElement(_title + (_radioScene ? " — phone / radio" : "") + "   |   Enter / controller A: skip", new PointF(35, 15), 0.32f, Color.White).Draw();
                if (Game.IsControlJustPressed(GTA.Control.FrontendAccept)) { Stop(); return; }
                _dialogue.Update();
                if (_actionStarted && _actionActor != null && _actionActor.Exists())
                {
                    var point = _actionActor.Position;
                    _camera.Position = point - _actionActor.ForwardVector * 5f + new Vector3(2f, 0f, 2f);
                    _camera.PointAt(point + new Vector3(0f, 0f, .8f));
                    Function.Call(Hash.SET_FOCUS_POS_AND_VEL, point.X, point.Y, point.Z, 0f, 0f, 0f);
                }
                if (!_dialogue.HasPending) NextLine();
            }
            catch (Exception ex)
            {
                Logger.Error("Scene update failed", ex);
                Stop();
            }
        }

        public void Stop()
        {
            if (!IsActive) return;
            Logger.Info("Scene ended; restoring player camera and controls.");
            _lines = null;
            _dockIntro = false;
            _radioScene = false;
            _actionActor = null;
            _sceneAction = null;
            _actionStarted = false;
            IsSceneRunning = false;
            Release("dialogue", _dialogue.Clear);
            Release("gameplay camera", () => World.RenderingCamera = null);
            Release("previous scripted camera", () =>
            {
                if (_previousCamera != null && _previousCamera.Exists()) World.RenderingCamera = _previousCamera;
            });
            Release("camera delete", () => _camera?.Delete());
            _camera = null;
            _previousCamera = null;
            Release("streaming focus", () => Function.Call(Hash.CLEAR_FOCUS));
            foreach (var held in _held)
                Release("actor state", () => { if (held.Entity.Exists()) { held.Entity.IsPositionFrozen = held.Frozen; held.Entity.IsInvincible = held.Invincible; } });
            _held.Clear();
            foreach (var actor in _temporary) Release("temporary actor", () => GameUtils.SafeDelete(actor));
            _temporary.Clear();
            _actors.Clear();
            _support.Clear();
            Release("player control", () => Game.Player.CanControlCharacter = _hadControl);
        }

        private static void Release(string name, Action action)
        {
            try { action(); } catch (Exception ex) { Logger.Error("Scene cleanup: " + name, ex); }
        }
    }
}

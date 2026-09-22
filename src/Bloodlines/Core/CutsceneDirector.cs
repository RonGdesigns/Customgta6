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
    /// <summary>How the last scene ended. Only Completed and Skipped mean its blocking reached the end state.</summary>
    public enum SceneOutcome
    {
        None,
        Completed,
        Skipped,
        Canceled,
        Failed
    }

    /// <summary>Small, skippable in-engine scenes. No mission timers run during a briefing.</summary>
    public sealed class CutsceneDirector
    {
        /// <summary>Outcome of the most recently ended scene; callers that must not treat a cancellation as success read this.</summary>
        public SceneOutcome LastOutcome { get; private set; } = SceneOutcome.None;
        private bool _naturalEnd;
        public int SceneSequence { get; private set; }
        public int FinishedSequence { get; private set; }
        public bool LastRequired { get; private set; }
        private bool _pendingRequired, _required;
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
        private SceneBlocking _blocking;
        private bool _skipping;
        private Ped _hiddenPlayer;
        private readonly List<HeldEntity> _held = new List<HeldEntity>();
        private Camera _camera, _previousCamera;
        /// <summary>
        /// A camera kept alive for the length of its hand-back to gameplay. Deleting a
        /// camera the moment the interpolation starts is a hard cut with extra steps.
        /// </summary>
        private Camera _retiring;
        private int _retireAt;
        /// <summary>How long the view takes to ease back into the player's own camera.</summary>
        public const int HandoffMs = 1100;
        /// <summary>How long a shot takes to slide into the next one instead of cutting.</summary>
        public const int ShotEaseMs = 700;

        /// <summary>Where the actors are looking and what their faces are doing.</summary>
        public ScenePerformance Performance { get; } = new ScenePerformance();

        private Vector3 _shotFrom, _shotTo, _lookFrom, _lookTo;
        private int _easeStartedAt, _easeMs;

        /// <summary>
        /// Move to a new shot by sliding rather than teleporting. Every camera move in this
        /// class used to be an assignment to Position, which is a cut on every line of
        /// dialogue — three men talking looked like a security-camera montage. The first
        /// shot of a scene still arrives instantly: there is nothing to ease from.
        /// </summary>
        private void Frame(Vector3 position, Vector3 lookAt, bool ease = true)
        {
            if (_camera == null || !_camera.Exists()) return;
            if (!ease || _easeMs == 0 && _easeStartedAt == 0)
            {
                _camera.Position = position;
                _camera.PointAt(lookAt);
                _shotFrom = _shotTo = position; _lookFrom = _lookTo = lookAt;
                _easeStartedAt = Game.GameTime; _easeMs = 0;
                return;
            }
            _shotFrom = _camera.Position; _lookFrom = _lookTo;
            _shotTo = position; _lookTo = lookAt;
            _easeStartedAt = Game.GameTime; _easeMs = ShotEaseMs;
        }

        /// <summary>Advance an easing shot. A tracking shot writes every frame and is not eased.</summary>
        private void EaseShot()
        {
            if (_camera == null || !_camera.Exists() || _easeMs <= 0) return;
            float t = (Game.GameTime - _easeStartedAt) / (float)_easeMs;
            if (t >= 1f) { t = 1f; _easeMs = 0; }
            // Smoothstep: leaves and arrives slowly, which is what a camera operator does
            // and what a linear slide conspicuously does not.
            float e = t * t * (3f - 2f * t);
            _camera.Position = _shotFrom + (_shotTo - _shotFrom) * e;
            _camera.PointAt(_lookFrom + (_lookTo - _lookFrom) * e);
        }

        /// <summary>
        /// Delete a camera whose hand-back has finished. Called every frame by the host,
        /// including while no scene is running, because that is when it matters.
        /// </summary>
        public void RetireCameras()
        {
            if (_retiring == null) return;
            if (Game.GameTime < _retireAt) return;
            ForceRetire();
        }

        /// <summary>
        /// Delete the camera that is handing back now, whatever its clock says. A new scene
        /// calls this before it looks at what is rendering, and so does a second hand-back:
        /// a camera still sliding home is this director's own, never a previous owner's
        /// view to restore.
        /// </summary>
        private void ForceRetire()
        {
            var going = _retiring;
            _retiring = null;
            if (going == null) return;
            try
            {
                // Still the rendering camera when its slide should be long over means the
                // view is stranded on it. Hand the view back before the camera goes, or the
                // screen stays on a shot that no longer exists.
                var rendering = World.RenderingCamera;
                if (IsScene(rendering, going) && !IsActive)
                {
                    Logger.Warn("Scene camera was still rendering after its hand-back; returning the view to gameplay.");
                    Function.Call(Hash.RENDER_SCRIPT_CAMS, false, false, 0, true, false, 0);
                }
                if (going.Exists()) going.Delete();
            }
            catch (Exception ex) { Logger.Error("Retiring a scene camera", ex); }
        }

        private static bool IsScene(Camera candidate, Camera ours) =>
            candidate != null && ours != null && candidate.Handle == ours.Handle;

        /// <summary>One word for the diagnostics line: who has the view right now.</summary>
        public string CameraReport()
        {
            try
            {
                var rendering = World.RenderingCamera;
                if (rendering == null || !rendering.Exists()) return "gameplay";
                if (IsActive && IsScene(rendering, _camera)) return "scene";
                if (IsScene(rendering, _retiring)) return "handing-back";
                return "script:" + rendering.Handle;
            }
            catch { return "unknown"; }
        }
        private List<DialogueCue> _lines;
        private int _index, _startedAt;
        private bool _hadControl;
        private string _title;
        public static bool IsSceneRunning { get; private set; }
        public bool IsActive => _lines != null;

        /// <summary>A skip press counts only after this long into the scene: the button that ended the drive, held into the door scene, skipped it 50 ms in (Ron, September 11).</summary>
        public const int SkipGraceMs = 1000;
        /// <summary>True once a skip press may end the running scene: past the grace, so a press carried in from gameplay is not a skip.</summary>
        public bool SkipInputAllowed => IsActive && Game.GameTime - _startedAt >= SkipGraceMs;

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

        /// <summary>
        /// Plays a scene. <paramref name="blocking"/> is the moving part: actors it
        /// names are left unfrozen and walk, enter vehicles and use phones while the
        /// lines play; the camera follows whichever step is running; the scene does
        /// not end until both the dialogue and the blocking have finished, and a skip
        /// completes the remaining blocking instantly so gameplay resumes in the
        /// same state either way.
        /// </summary>
        /// <summary>Play a specified scene: its lines, its support cast, its blocking and shots.</summary>
        public bool Play(SceneSpec spec)
        {
            if (spec == null) return false;
            _pendingSupport = spec.Support;
            _pendingRequired = spec.RequiresCompletion;
            try
            {
                bool started = Play(spec.MissionId, spec.Phase, spec.Title, spec.ActionActor, spec.SceneAction, spec.Blocking);
                if (started) Logger.Info("Scene " + spec.MissionId + ":" + spec.Phase + " establishes: " + spec.Reason);
                return started;
            }
            finally { _pendingSupport = null; _pendingRequired = false; }
        }

        private Dictionary<string, Ped> _pendingSupport;

        /// <summary>
        /// A gameplay stage's own bible lines, played as a scene: the same cues the
        /// dialogue director would queue, but with a cast, shots and blocking. The
        /// spec's phase names the scene; the cues are registered under it for this
        /// play. Nothing is duplicated in the data: the lines stay in dialogue.tsv.
        /// </summary>
        public bool PlayStaged(SceneSpec spec, IEnumerable<DialogueCue> cues)
        {
            if (spec == null || cues == null) return false;
            var lines = cues.Where(c => c != null).ToList();
            if (lines.Count == 0) return false;
            _scenes[spec.MissionId + ":" + spec.Phase] = lines;
            return Play(spec);
        }

        public bool Play(string missionId, string phase, string title, Ped actionActor = null, Action sceneAction = null, SceneBlocking blocking = null)
        {
            if (IsActive || !_scenes.TryGetValue(missionId + ":" + phase, out var lines) || lines.Count == 0) return false;
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return false;
            _lines = lines;
            SceneSequence++;
            LastOutcome = SceneOutcome.None;
            _required = _pendingRequired;
            IsSceneRunning = true;
            _index = 0;
            _startedAt = Game.GameTime;
            _title = title;
            _dockIntro = missionId == "M01" && phase == "intro";
            _radioScene = false;
            _actionActor = actionActor;
            _sceneAction = sceneAction;
            _actionStarted = false;
            _blocking = blocking;
            _hadControl = Game.Player.CanControlCharacter;
            try
            {
                // A scene that starts while the last one is still sliding home would
                // otherwise read that camera as somebody else's view and restore it when
                // it ends: the view then sits on a dead shot and never comes back to the
                // player. Every Act One briefing followed by its first staged scene did
                // this, a quarter of a second apart (Ron, September 22).
                var handingBack = _retiring;
                ForceRetire();
                _previousCamera = World.RenderingCamera;
                // GET_RENDERING_CAM can return an invalid handle wrapped as a Camera.
                if (_previousCamera != null && !_previousCamera.Exists()) _previousCamera = null;
                if (IsScene(_previousCamera, handingBack)) _previousCamera = null;
                if (missionId == "M01" && phase == "intro" && !ProloguePlacement.Prepare(_locations))
                    throw new InvalidOperationException("M01 dock surfaces are not ready. Move near its start marker and retry.");
                Logger.Info("Scene started: " + missionId + ":" + phase);
                _dialogue.Clear();
                Hold(player);
                Hold(player.CurrentVehicle);
                Game.Player.CanControlCharacter = false;
                // The mission's own cast for speakers who are not brothers. Held like
                // the rest unless the blocking moves them; released on every exit.
                if (_pendingSupport != null)
                    foreach (var pair in _pendingSupport)
                        if (pair.Value != null && pair.Value.Exists()) { _support[pair.Key.ToUpperInvariant()] = pair.Value; Hold(pair.Value); }
                // Briefings use temporary cast before mission setup. The M01 cold open
                // shows three separate jobs; they must not meet before recognition.
                var speakers = Protagonist.All.Where(p => lines.Any(l => l.Speaker.Equals(p.Handle, StringComparison.OrdinalIgnoreCase))).ToList();
                var staged = new Dictionary<CrewSlot, Ped>();
                if (!_dockIntro && !_crew.IsDeployed)
                {
                    // Nobody is deployed yet, so the speakers are temporary actors. Ron
                    // (Guess) is the one already at the start point; the others pull up
                    // in the crew's four-door and talk from the curb. If there is no
                    // street to arrive by, they are staged on foot instead.
                    var missing = speakers.Where(p => { var ped = _crew.PedFor(p.Slot); return ped == null || !ped.Exists() || ped.IsDead; }).ToList();
                    if (missing.Count > 0)
                    {
                        var host = missing.FirstOrDefault(p => p.Slot == CrewSlot.Guess) ?? missing[0];
                        // No invented arrival car or traffic-dependent drive-up. Remote
                        // speakers use radio; the actual arriving car remains the set.
                        var hostActor = player.IsInVehicle() ? player : StageHost(host, player, null);
                        if (hostActor != null) staged[host.Slot] = hostActor;
                    }
                }
                foreach (var protagonist in speakers)
                {
                    Ped actor = _dockIntro ? null : _crew.PedFor(protagonist.Slot);
                    if (actor != null && (!actor.Exists() || actor.IsDead)) actor = null;
                    if (actor == null && !_dockIntro)
                    {
                        // A briefing plays before the mission deploys anyone, so no crew
                        // ped exists. Without this the director framed the only ped it
                        // had: the story character, Franklin, standing in for the crew.
                        // Stage the hero as a temporary actor beside the start point and
                        // hide the story ped for the scene; a deployed crew that is merely
                        // far away is still a radio call.
                        if (_crew.IsDeployed) { _radioScene = true; continue; }
                        if (!staged.TryGetValue(protagonist.Slot, out actor)) { _radioScene = true; continue; }
                        if (actor == null) { _radioScene = true; continue; }
                        if (actor != player && _hiddenPlayer == null) { _hiddenPlayer = player; player.IsVisible = false; }
                    }
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
                    SceneVehicle("schafter3", _locations.Position("M01.PrototypeCar"), ((_locations.Heading("M01.PrototypeCar") + 90f) % 360f));
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
                    var desk = SceneProp("prop_table_03", terminal, true);
                    if (desk != null) SceneProp("prop_laptop_01a", PropPlacement.OnTop(desk, new Model("prop_table_03"), new Model("prop_laptop_01a")), false);
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
                // On-foot radio scenes use a real sustained call. Leave seated drivers
                // and authored movement alone: they talk over hands-free radio.
                if (_radioScene && _blocking == null && _crew.IsDeployed && !player.IsInVehicle())
                    _blocking = new SceneBlocking().Then(new UsePhoneStep(player, 1800));
                _blocking?.BindDialogue(() => _lines != null && _index >= _lines.Count && !_dialogue.HasPending);
                // Hide the ordinary player during the temporary-cast briefing with
                // framing, without altering its visibility or position.
                _camera = World.CreateCamera(player.Position + new Vector3(0, -3, 2), Vector3.Zero, 48f);
                if (_camera == null || !_camera.Exists()) throw new InvalidOperationException("Camera creation failed.");
                World.RenderingCamera = _camera;
                _easeStartedAt = 0; _easeMs = 0;
                // Who is in the room. Everyone who might speak or be spoken to, so a line
                // turns the heads of the people it is aimed at.
                Performance.Begin(_actors.Values.Concat(_support.Values));
                // Moving actors stay movable. Their held entry still restores the
                // original frozen/invincible flags when the scene ends.
                if (_blocking != null)
                    foreach (var mover in _blocking.Actors)
                        if (mover != null && mover.Exists()) mover.IsPositionFrozen = false;
                if (_blocking == null || !_blocking.HoldsDialogue) NextLine();
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

        /// <summary>The hero already at the start point: beside where the story character stood, facing the arrival.</summary>
        private Ped StageHost(Protagonist protagonist, Ped player, Vector3? facing)
        {
            var model = protagonist.Model;
            if (!GameUtils.RequestModel(model, 1000)) return null;
            var forward = player.ForwardVector;
            var right = new Vector3(forward.Y, -forward.X, 0f);
            var position = player.Position + right * 1.4f;
            float heading = facing.HasValue ? DriveUpStep.HeadingBetween(position, facing.Value) : player.Heading;
            var actor = World.CreatePed(model, position, heading);
            model.MarkAsNoLongerNeeded();
            if (actor == null || !actor.Exists()) return null;
            CrewAppearance.Apply(actor, protagonist.Slot);
            _temporary.Add(actor);
            actor.IsPersistent = true;
            actor.BlockPermanentEvents = true;
            actor.Task.StandStill(-1);
            return actor;
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

        private Prop SceneProp(string name, Vector3 point, bool ground)
        {
            var model = new Model(name);
            try
            {
                if (!GameUtils.RequestModel(model, 1000)) return null;
                var prop = World.CreateProp(model, point, false, ground);
                if (prop == null || !prop.Exists()) return null;
                _temporary.Add(prop); Hold(prop);
                return prop;
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }

        private void NextLine()
        {
            // Lines finished but someone is still walking to a car: hold the scene
            // on the blocking, then end. Skip still completes it instantly.
            if (_index >= _lines.Count) { if (_blocking != null && !_blocking.IsFinished) return; _naturalEnd = true; Stop(); return; }
            var cue = _lines[_index++];
            Ped actor = null;
            if (Enum.TryParse(cue.Speaker, true, out CrewSlot slot)) _actors.TryGetValue(slot, out actor);
            if (actor == null) _support.TryGetValue(cue.Speaker, out actor);
            // Solo aftermath voices come over radio; never invent an off-screen companion.
            if (actor == null || !actor.Exists()) actor = Game.Player.Character;
            if (_radioScene && !_dockIntro && actor != null && actor.Position.DistanceTo(Game.Player.Character.Position) > 18f) actor = Game.Player.Character;
            bool shotInCharge = _blocking != null && _blocking.Current != null && _blocking.Current.HasStarted && _blocking.Current.DriveCamera(_camera);
            if (actor != null && actor.Exists() && !shotInCharge)
            {
                var facing = actor.ForwardVector;
                // A side window view keeps seated actors in context without clearing their tasks.
                var offset = actor.IsInVehicle() ? new Vector3(facing.Y * 3.8f, -facing.X * 3.8f, 1.1f)
                    : facing * 2.8f + new Vector3(0.8f, 0, 1.1f);
                Frame(actor.Position + offset, actor.Position + new Vector3(0, 0, 0.75f));
                Function.Call(Hash.SET_FOCUS_POS_AND_VEL, actor.Position.X, actor.Position.Y, actor.Position.Z, 0f, 0f, 0f);
            }
            if (_dockIntro && (_index == 1 || _index == 5 || _index == 7))
            {
                var point = _locations.Position(_index == 1 ? "M01.RegroupPoint" : _index == 5 ? "M01.PrototypeCar" : "M01.CapoSpawn");
                Frame(point + (_index == 1 ? new Vector3(25f, 24f, 18f) : new Vector3(8f, -10f, 5f)),
                    point + new Vector3(0f, 0f, 1f));
                Function.Call(Hash.SET_FOCUS_POS_AND_VEL, point.X, point.Y, point.Z, 0f, 0f, 0f);
            }
            // Everyone turns to whoever is about to speak, and he looks at one of them.
            Performance.Speak(actor);
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
                new GTA.UI.TextElement(_title + (_radioScene ? " — phone / radio" : "") + "   |   Enter / controller A: skip", new PointF(35, 15), 0.32f, Color.White).Draw();
                if (SkipInputAllowed && Game.IsControlJustPressed(GTA.Control.FrontendAccept)) { Skip(); return; }
                _dialogue.Update();
                if (_blocking != null)
                {
                    _blocking.Update();
                    var current = _blocking.Current;
                    if (current != null && current.HasStarted && current.DriveCamera(_camera)) { }
                    else if (current?.CameraTarget is Entity subject && subject.Exists())
                    {
                        // Tracking shot: behind and above the subject, looking through it.
                        var forward = subject is Ped mover ? mover.ForwardVector : subject is Vehicle ride ? ride.ForwardVector : new Vector3(0f, 1f, 0f);
                        // A tracking shot writes every frame; easing it would make the camera
                        // lag the thing it is following.
                        Frame(subject.Position - forward * 4.5f + new Vector3(1.2f, 0f, 1.6f),
                            subject.Position + new Vector3(0f, 0f, 0.7f), ease: false);
                        Function.Call(Hash.SET_FOCUS_POS_AND_VEL, subject.Position.X, subject.Position.Y, subject.Position.Z, 0f, 0f, 0f);
                    }
                }
                EaseShot();
                if (_actionStarted && _actionActor != null && _actionActor.Exists())
                {
                    var point = _actionActor.Position;
                    _camera.Position = point - _actionActor.ForwardVector * 5f + new Vector3(2f, 0f, 2f);
                    _camera.PointAt(point + new Vector3(0f, 0f, .8f));
                    Function.Call(Hash.SET_FOCUS_POS_AND_VEL, point.X, point.Y, point.Z, 0f, 0f, 0f);
                }
                if (!_dialogue.HasPending && (_blocking == null || !_blocking.HoldsDialogue)) NextLine();
            }
            catch (Exception ex)
            {
                Logger.Error("Scene update failed", ex);
                Stop();
            }
        }

        /// <summary>
        /// The player chose to skip. Whatever blocking has not played is finished
        /// instantly so the world ends up exactly where watching would have left it.
        /// </summary>
        public void Skip()
        {
            if (!IsActive) return;
            _skipping = true;
            Stop();
        }

        /// <summary>
        /// End the scene without completing it: abort, error, death, teardown.
        /// Camera and control come back; unfinished blocking is canceled, not
        /// executed. A scene that ran to its natural end has nothing left to cancel.
        /// </summary>
        public void Stop()
        {
            if (!IsActive) return;
            bool skipping = _skipping;
            bool natural = _naturalEnd;
            _skipping = false;
            _naturalEnd = false;
            var blockingForOutcome = _blocking;
            FinishedSequence = SceneSequence;
            LastRequired = _required;
            _required = false;
            LastOutcome = skipping ? SceneOutcome.Skipped : natural ? SceneOutcome.Completed : SceneOutcome.Canceled;
            Logger.Info(skipping ? "Scene skipped; finishing its blocking and restoring player camera and controls." : "Scene ended; restoring player camera and controls.");
            _lines = null;
            _dockIntro = false;
            _radioScene = false;
            _actionActor = null;
            _sceneAction = null;
            _actionStarted = false;
            IsSceneRunning = false;
            // Skip: finish what has not played, so watched and skipped agree.
            // Anything else: stand the actors down where they are.
            var blocking = _blocking;
            _blocking = null;
            if (blocking != null)
                Release("scene blocking", () =>
                {
                    var player = Game.Player.Character;
                    if (skipping && player != null && player.Exists() && !player.IsDead) blocking.Complete();
                    else if (!blocking.IsFinished) blocking.Cancel();
                });
            // A blocking that could not reach its end state is a failure whatever
            // the player pressed; a natural end with a failed step is one too.
            if (blockingForOutcome != null && !blockingForOutcome.Succeeded && LastOutcome != SceneOutcome.Canceled) LastOutcome = SceneOutcome.Failed;
            Release("dialogue", _dialogue.Clear);
            Release("scene performance", Performance.End);
            // The view eases back into the player's own camera instead of cutting to it.
            // Only when nobody else owns a scripted camera: handing an interpolation to a
            // camera another system is about to restore is how a player ends up looking at
            // the sky. That path keeps the old hard release.
            bool smooth = _previousCamera == null && _camera != null && _camera.Exists();
            Release("gameplay camera", () =>
            {
                if (smooth) Function.Call(Hash.RENDER_SCRIPT_CAMS, false, true, HandoffMs, true, false, 0);
                else World.RenderingCamera = null;
            });
            Release("previous scripted camera", () =>
            {
                if (_previousCamera != null && _previousCamera.Exists()) World.RenderingCamera = _previousCamera;
            });
            // A camera deleted mid-interpolation is a hard cut with extra steps. Keep it
            // alive for the length of the hand-back; RetireCameras deletes it after, and
            // the next scene forces the retirement if that never ran.
            if (smooth) { Release("earlier scene camera", ForceRetire); _retiring = _camera; _retireAt = Game.GameTime + HandoffMs + 250; }
            else Release("camera delete", () => _camera?.Delete());
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
            var hidden = _hiddenPlayer;
            _hiddenPlayer = null;
            if (hidden != null) Release("player visibility", () => { if (hidden.Exists()) hidden.IsVisible = true; });
            Release("player control", () => Game.Player.CanControlCharacter = _hadControl);
        }

        private static void Release(string name, Action action)
        {
            try { action(); } catch (Exception ex) { Logger.Error("Scene cleanup: " + name, ex); }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using Bloodlines.Core;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Objectives
{
    /// <summary>Board a mission ride without attempting to drag its protected crew driver out.</summary>
    public static class MissionBoarding
    {
        public static VehicleSeat FreeSeat(Vehicle car, VehicleSeat desired)
        {
            if(desired!=VehicleSeat.Any)return car.IsSeatFree(desired)?desired:VehicleSeat.None;
            // A crew driver keeps his seat. Front passenger is index zero, not one.
            for(int i=0;i<car.PassengerCapacity;i++)if(car.IsSeatFree((VehicleSeat)i))return (VehicleSeat)i;
            return car.IsSeatFree(VehicleSeat.Driver)?VehicleSeat.Driver:VehicleSeat.None;
        }
        public static void Update(Vehicle vehicle, VehicleSeat desired, ref int nextBoard)
        {
            var player=Game.Player.Character;
            if(vehicle==null||!vehicle.Exists()||vehicle.IsDead||player==null||!player.Exists()||player.IsInVehicle()||vehicle.Speed>3f)return;
            // Aircraft and subs have larger hulls than a sedan; measure the nearby
            // hatch area rather than demanding that the player stand in its origin.
            float range=vehicle.Model.IsHelicopter?12f:7f;
            if(player.Position.DistanceTo(vehicle.Position)>range)return;
            vehicle.LockStatus=VehicleLockStatus.Unlocked;
            bool pressed=Game.IsControlJustPressed(GTA.Control.Enter)||Game.IsControlJustPressed(GTA.Control.Context);
            if(!pressed||Game.GameTime<nextBoard)return;
            var seat=FreeSeat(vehicle,desired);if(seat==VehicleSeat.None)return;
            Game.DisableControlThisFrame(GTA.Control.Enter);
            player.Task.ClearAll();
            player.Task.EnterVehicle(vehicle,seat,8000,2f,EnterVehicleFlags.None);
            nextBoard=Game.GameTime+2000;
            Logger.Info("Mission boarding: normal door entry to seat "+seat+"; existing crew seats retained.");
        }
    }

    public sealed class DialogueFinishedObjective : Objective
    {
        public DialogueFinishedObjective(string label = "Listen to Mateo. Stay with the boats."):base(label){}
        public override void Update(MissionContext c){if(!c.Dialogue.HasPending)Complete();}
    }

    /// <summary>Explicit context-button interaction with a visible, interruptible work timer.</summary>
    public sealed class MissionInteraction : Objective
    {
        private readonly string _action;
        private readonly Func<Vector3> _position;
        private readonly Func<Vehicle> _vehicle;
        private readonly float _radius;
        private readonly int _duration;
        private int _started = -1, _nextBoard, _lastTick = -1, _lastSteady = -1;
        /// <summary>
        /// How fast a boat may be moving and still count as stopped. Storm swell alone moves an
        /// idle dinghy past the 1 m/s a car is held to, so M05's "take him aboard" could not be
        /// started and, once started, kept restarting (Ron, September 22).
        /// </summary>
        public const float AfloatStopSpeed = 3.5f;
        /// <summary>How long a boat may be pushed out of reach or over that speed before a hold in progress is lost. The bar pauses meanwhile; it does not restart.</summary>
        public const int AfloatGraceMs = 2500;
        private readonly bool _stopVehicle;
        private readonly string _animation;
        private readonly Func<Vector3> _face;
        private bool _animating;
        private Ped _worker;
        public MissionInteraction(string action, Func<Vector3> position, int seconds, float radius = 3f, Func<Vehicle> vehicle = null, bool stopVehicle = false, string animation = null, Func<Vector3> face = null) : base(action)
        { _action = action; _position = position; _duration = seconds * 1000; _radius = radius; _vehicle = vehicle; _stopVehicle = stopVehicle; _animation = animation; _face = face; }

        // ---- What he does with his hands while the bar fills --------------------------------
        //
        // A hold with no animation is a man standing still while a bar fills, which in play
        // reads as nothing happening at all (SM02's root terminal, Ron, September 23). Every
        // pair below is either already played by this mod or belongs to one of the game's own
        // ambient scenarios, and each is checked when it is played: a dictionary the game does
        // not have, one that will not load, or a clip that does not take falls back once to
        // ReachInside, which this mod has played since Act One, and says so in the log.

        /// <summary>Bent over, both hands inside something at waist height: a car window, a bin, a crate.</summary>
        public const string ReachInside = "amb@prop_human_bum_bin@idle_a|idle_a";
        /// <summary>Typing at a keypad, terminal or laptop. The clip M01's shipping terminal and M07's relay already play.</summary>
        public const string Typing = "anim@heists@humane_labs@emp@hack_door|hack_loop";
        /// <summary>Down on one knee with the hands at the ground: planting, placing, setting something low.</summary>
        public const string Kneel = "amb@medic@standing@kneel@idle_a|idle_a";
        /// <summary>A torch held to the work: welding, cutting, burning through.</summary>
        public const string Welding = "amb@world_human_welding@male@base|base";
        /// <summary>Swinging at something at chest height: bracing, nailing, knocking loose.</summary>
        public const string Hammering = "amb@world_human_hammering@male@base|base";
        /// <summary>Bent over an open engine bay: repairs, hotwiring, splicing a harness.</summary>
        public const string Repair = "mini@repair|fixing_a_ped";
        /// <summary>Standing and checking, as the clipboard scenario does: inspecting, counting, verifying.</summary>
        public const string Inspect = "amb@world_human_clipboard@male@base|base";
        /// <summary>Head down over a phone: texting, dialing, reading.</summary>
        public const string Phone = "cellphone@|cellphone_text_read_base";
        /// <summary>A hand on a wall panel at chest height: a lift call, a door release, a breaker, a valve.</summary>
        public const string Operate = "amb@prop_human_parking_meter@male@idle_a|idle_a";
        /// <summary>Glasses raised to the eyes: observing, spotting, identifying at range.</summary>
        public const string Watch = "amb@world_human_binoculars@male@base|base";
        /// <summary>A hand held out to be given something, or to give it.</summary>
        public const string Handover = "mp_common|givetake1_a";
        /// <summary>
        /// The work is done swimming, so nothing is played. Every clip above is a standing pose,
        /// and played on a swimmer it takes him out of the swim (M61's cut at the bottom of the
        /// basin). A swimmer is never given one whatever was asked for; this says it on purpose.
        /// </summary>
        public const string InWater = "none";
        /// <summary>What a hold falls back to when the text names nothing the inference knows.</summary>
        public const string Generic = Operate;
        /// <summary>How long a dictionary may take to load after the press before the fallback is used.</summary>
        public const int LoadTimeoutMs = 2000;
        /// <summary>How long after the clip is started it is checked for actually playing.</summary>
        public const int VerifyAfterMs = 750;

        private sealed class Rule
        {
            public readonly string Animation; public readonly string[] Words;
            public Rule(string animation, params string[] words) { Animation = animation; Words = words; }
        }

        // First rule with a matching word wins, so the order is the judgment: a lift is called,
        // not phoned; codes are demanded of a man before they are typed; a server is cut out
        // before it is typed on. A trailing * matches any word that starts with the stem.
        private static readonly Rule[] Rules =
        {
            new Rule(InWater, "swim*", "dive", "diving", "underwater"),
            new Rule(Watch, "observ*", "glass", "binocular*", "spot", "scout*", "identify", "watch"),
            new Rule(Operate, "elevator*", "lift", "lifts", "door", "doors", "doorway", "gate", "gates", "stair*", "access", "button*", "intercom"),
            new Rule(Handover, "demand*", "interrogat*", "question*", "ask", "bribe*", "pay", "hand", "hands", "handover", "give", "receive"),
            new Rule(Welding, "weld*", "cut", "cuts", "cutting", "cutter*", "torch*", "thermite", "burn*"),
            new Rule(Typing, "laptop*", "terminal*", "hack*", "keypad*", "console*", "computer*", "type", "typing", "upload*", "download*", "inject*", "worm", "password*", "code", "codes", "server*", "bypass*", "reprogram*", "firmware", "files", "copy", "override*", "network*", "feed", "encrypt*", "decrypt*"),
            new Rule(Kneel, "plant*", "charge", "charges", "limpet*", "mine", "mines", "explosive*", "bomb*", "tripwire*", "place", "bury"),
            new Rule(Hammering, "hammer*", "nail*", "brace*", "barricade*", "reinforce*", "pry", "prise", "crowbar"),
            new Rule(Repair, "repair*", "fix*", "engine*", "hood", "hotwir*", "wire", "wires", "wiring", "splice*", "rewire*", "tune*", "mechanic*"),
            new Rule(Phone, "phone*", "text", "texts", "dial*", "call", "radio"),
            new Rule(Inspect, "inspect*", "check*", "examine*", "verify*", "count*", "survey*", "photograph*", "document*", "read", "review*"),
            new Rule(ReachInside, "search*", "grab*", "collect*", "take", "takes", "pick*", "load*", "unload*", "retriev*", "recover*", "open", "crate*", "case", "cases", "box*", "bag*", "locker*", "trunk*", "stow*", "carry*", "package*", "loot*", "steal*", "fit", "fits", "attach*", "install*", "connect*", "plug*"),
            new Rule(Operate, "panel*", "valve*", "breaker*", "switch*", "lever*", "cabinet*", "fuse*", "generator*", "control*", "power", "pump*"),
        };

        /// <summary>
        /// The animation an on-foot hold with no explicit one plays, read from its action text:
        /// the first rule with a matching word, or <see cref="Generic"/> when nothing matches,
        /// so no hold is ever a man standing still.
        /// </summary>
        public static string Infer(string action)
        {
            var words = new List<string>();
            var word = new System.Text.StringBuilder();
            foreach (char ch in (action ?? "").ToLowerInvariant() + " ")
            {
                if (char.IsLetterOrDigit(ch)) { word.Append(ch); continue; }
                if (word.Length > 0) { words.Add(word.ToString()); word.Clear(); }
            }
            foreach (var rule in Rules)
                foreach (var key in rule.Words)
                {
                    bool stem = key.EndsWith("*", StringComparison.Ordinal);
                    string text = stem ? key.Substring(0, key.Length - 1) : key;
                    foreach (var w in words)
                        if (stem ? w.StartsWith(text, StringComparison.Ordinal) : w == text) return rule.Animation;
                }
            return Generic;
        }

        /// <summary>
        /// What a hold will play: nothing from a vehicle, nothing for <see cref="InWater"/>, the
        /// explicit animation when one was given (it always wins), and otherwise the inference.
        /// </summary>
        public static string Resolve(string animation, string action, bool fromVehicle)
        {
            if (fromVehicle) return null;
            string chosen = string.IsNullOrEmpty(animation) ? Infer(action) : animation;
            return chosen == InWater ? null : chosen;
        }

        private bool _inferenceLogged, _swimLogged, _fellBack, _wantAnimation, _verified;
        private string _playing, _fallbackFrom;
        private int _requestedAt, _playedAt;
        private Vector3 _workPoint;
        private readonly HashSet<string> _requestedDicts = new HashSet<string>();

        /// <summary>The animation this hold plays on foot, explicit or inferred; null from a vehicle or in the water.</summary>
        public string Animation
        {
            get
            {
                string resolved = Resolve(_animation, _action, _vehicle != null);
                if (resolved != null && string.IsNullOrEmpty(_animation) && !_inferenceLogged)
                {
                    _inferenceLogged = true;
                    Logger.Info("Mission interaction \"" + _action + "\" names no animation; inferred " + resolved + " from its text.");
                }
                return resolved;
            }
        }
        /// <summary>Whether the animation was read from the action text rather than given.</summary>
        public bool AnimationInferred => _vehicle == null && string.IsNullOrEmpty(_animation);
        /// <summary>The clip actually playing on the worker now, or null.</summary>
        public string PlayingAnimation => _animating ? _playing : null;

        private static bool Split(string animation, out string dict, out string clip)
        {
            dict = clip = null;
            if (string.IsNullOrEmpty(animation)) return false;
            var parts = animation.Split('|');
            if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0) return false;
            dict = parts[0]; clip = parts[1];
            return true;
        }
        private void Request(string animation)
        {
            if (!Split(animation, out var dict, out _)) return;
            GTA.Native.Function.Call(GTA.Native.Hash.REQUEST_ANIM_DICT, dict);
            _requestedDicts.Add(dict);
        }
        private void StopAnimation(Ped ped)
        {
            _wantAnimation = false;
            if (!_animating) return;
            _animating = false;
            if (_worker != null && _worker.Exists() && Split(_playing, out var dict, out var clip))
                GTA.Native.Function.Call(GTA.Native.Hash.STOP_ANIM_TASK, _worker, dict, clip, 2f);
            _worker = null;
        }
        /// <summary>
        /// The press. The clip is issued now; its dictionary was asked for on <see cref="Enter"/>,
        /// so it is normally resident by the time he reaches the marker. Nothing here waits on
        /// the script thread, and the bar runs from the press either way.
        /// </summary>
        private void StartAnimation(Ped ped, Vector3 point)
        {
            _playing = _playing ?? Animation;
            if (_playing == null) return;
            _wantAnimation = true; _requestedAt = Game.GameTime; _workPoint = point;
            MaintainAnimation(ped, Game.GameTime);
        }
        private void FallBack(string reason)
        {
            if (_fellBack || _playing == ReachInside)
            {
                Logger.Warn("Mission interaction \"" + _action + "\": " + _playing + " " + reason + "; no animation this hold.");
                _wantAnimation = false;
                return;
            }
            Logger.Warn("Mission interaction \"" + _action + "\": " + _playing + " " + reason + "; falling back to " + ReachInside + ".");
            _fellBack = true; _fallbackFrom = _playing; _playing = ReachInside; _requestedAt = Game.GameTime;
            Request(_playing);
        }
        /// <summary>The clip this hold fell back from, when its own did not play.</summary>
        public string FellBackFrom => _fallbackFrom;
        private void MaintainAnimation(Ped ped, int now)
        {
            if (ped == null || !ped.Exists()) return;
            if (_animating)
            {
                if (_verified || now - _playedAt < VerifyAfterMs) return;
                if (!Split(_playing, out var playingDict, out var playingClip)) return;
                if (GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_ENTITY_PLAYING_ANIM, _worker, playingDict, playingClip, 3))
                { _verified = true; return; }
                if (now - _requestedAt <= LoadTimeoutMs)
                {
                    // Still inside the load window: the dictionary may have arrived since the
                    // press, and a clip asked for before it did simply never started.
                    if (GTA.Native.Function.Call<bool>(GTA.Native.Hash.HAS_ANIM_DICT_LOADED, playingDict)) { Play(_worker, playingDict, playingClip); _playedAt = now; }
                    return;
                }
                StopAnimation(ped);
                _wantAnimation = true;
                FallBack("did not play within " + LoadTimeoutMs + " ms of the press");
                return;
            }
            if (!_wantAnimation) return;
            // A standing pose played on a swimmer takes him out of the swim.
            if (ped.IsSwimming || ped.IsSwimmingUnderWater)
            {
                if (!_swimLogged) { _swimLogged = true; Logger.Info("Mission interaction \"" + _action + "\": he is swimming, so no animation is played."); }
                _wantAnimation = false;
                return;
            }
            if (!Split(_playing, out var dict, out var clip)) { FallBack("is not a dictionary|clip pair"); return; }
            if (!GTA.Native.Function.Call<bool>(GTA.Native.Hash.DOES_ANIM_DICT_EXIST, dict)) { FallBack("names a dictionary the game does not have"); return; }
            Request(_playing);
            ped.Heading = Core.DriveUpStep.HeadingBetween(ped.Position, _face?.Invoke() ?? _workPoint);
            Play(ped, dict, clip);
            _animating = true; _worker = ped; _playedAt = now; _verified = false;
        }
        /// <summary>
        /// The native task, not <c>ped.Task.PlayAnimation</c>: SHVDN's wrapper requests the
        /// dictionary itself and yields the script for up to a second waiting on it. The
        /// dictionary was asked for on <see cref="Enter"/>; a clip issued before it has
        /// arrived does not start, and the check above issues it again once it has.
        /// </summary>
        private static void Play(Ped ped, string dict, string clip)
            => GTA.Native.Function.Call(GTA.Native.Hash.TASK_PLAY_ANIM, ped, dict, clip, 4f, -4f, -1, (int)AnimationFlags.Loop, 0f, false, false, false);
        public override void Exit(MissionContext c)
        {
            StopAnimation(Game.Player.Character);
            foreach (var dict in _requestedDicts) GTA.Native.Function.Call(GTA.Native.Hash.REMOVE_ANIM_DICT, dict);
            _requestedDicts.Clear();
            base.Exit(c);
        }
        public override Vector3? AssignmentPosition => _vehicle == null ? (Vector3?)_position() : null;
        public override void Enter(MissionContext c)
        {
            base.Enter(c); _started = -1; _lastTick = -1; _lastSteady = -1; Label = _action + " — go to the yellow marker; press E / D-pad Right.";
            // Asked for on the way to the marker so the clip is resident by the press.
            Request(_playing ?? Animation);
        }
        public override void Update(MissionContext c)
        {
            int now = Game.GameTime;
            int dt = _lastTick < 0 ? 0 : Math.Max(0, Math.Min(250, now - _lastTick));
            _lastTick = now;
            var requiredVehicle = _vehicle?.Invoke();
            if (_vehicle != null && (requiredVehicle == null || !requiredVehicle.Exists() || requiredVehicle.IsDead))
            { Fail("The required work vehicle is lost. Restart this mission."); return; }
            var point = _position(); var ped = Game.Player.Character;
            bool underwater = UnderwaterGuidance.IsSub(requiredVehicle);
            if (underwater) UnderwaterGuidance.Draw(requiredVehicle, point, _radius);
            else { ObjectiveMarkers.Navigation(point, _vehicle == null ? RequiredCharacter : null, requiredVehicle); GameUtils.DrawObjectiveMarker(point, Color.Yellow, Math.Max(1f, _radius * .4f)); }
            if (!IsOwnerActive(c)) { _started = -1; StopAnimation(ped); Label = "Switch to " + Crew.Protagonist.Of(RequiredCharacter.Value).Handle + ": " + _action; return; }
            if (requiredVehicle != null) MissionBoarding.Update(requiredVehicle, VehicleSeat.Any, ref _nextBoard);
            bool seated = _vehicle != null && ped != null && ped.IsInVehicle(_vehicle());
            // Afloat, the reach is measured flat: the swell lifts one boat past the other and
            // a three-dimensional distance counted that as drifting apart.
            bool afloat = requiredVehicle != null && requiredVehicle.Model.IsBoat;
            bool near = ped != null && ped.Exists() && (_vehicle != null
                ? seated && (afloat ? GameUtils.IsWithinFlat(requiredVehicle.Position, point, _radius) : requiredVehicle.Position.DistanceTo(point) <= _radius)
                : !ped.IsInVehicle() && ped.Position.DistanceTo(point) <= _radius);
            bool steady = !_stopVehicle || requiredVehicle == null || requiredVehicle.Speed <= (afloat ? AfloatStopSpeed : 1f);
            if (near && steady) _lastSteady = now;
            else if (afloat && seated && _started >= 0 && now - _lastSteady <= AfloatGraceMs)
            {
                // A wave is not the player letting go. Hold the bar where it is for a moment
                // rather than throwing away the work.
                _started += dt;
                Label = _action + " — hold steady; the swell moved the boat.";
                GameUtils.DrawProgressBar((now - _started) / (float)_duration);
                return;
            }
            if (!near) { _started = -1; StopAnimation(ped); Label = _action + (_vehicle == null ? " — get out and reach the yellow marker." : (seated ? " — take the marked vehicle to the yellow marker." : " — board the marked vehicle first (F / Y or E / D-pad Right).")); return; }
            if (!steady) { _started = -1; Label = _action + (afloat ? " — ease off the throttle and let the boat settle." : " — stop the vehicle to begin unloading."); return; }
            if (_started < 0)
            {
                Label = _action + " — press E / D-pad Right to start.";
                if (!Game.IsControlJustPressed(GTA.Control.Context)) return;
                _started = now;
                if (_vehicle == null) StartAnimation(ped, point);
            }
            if (_vehicle == null) MaintainAnimation(ped, now);
            int elapsed = now - _started;
            Label = _action;
            GameUtils.DrawProgressBar(elapsed / (float)_duration);
            if (elapsed >= _duration) { StopAnimation(ped); Complete(); }
        }
    }

    public sealed class OccupiedVehicleDestination : Objective
    {
        private readonly Func<Vehicle> _vehicle;
        private readonly Func<Vector3> _destination;
        private readonly float _radius;
        public OccupiedVehicleDestination(string label, Func<Vehicle> vehicle, Func<Vector3> destination, float radius = 20f) : base(label)
        { _vehicle = vehicle; _destination = destination; _radius = radius; }
        public override void Update(MissionContext c)
        {
            var vehicle = _vehicle();
            if (vehicle == null || !vehicle.Exists() || !vehicle.IsDriveable) { Fail("The required vehicle is lost."); return; }
            var target = _destination();
            ObjectiveMarkers.Navigation(target, null, vehicle); GameUtils.DrawObjectiveMarker(target, Color.Yellow, 4f);
            var player = Game.Player.Character;
            if (IsOwnerActive(c) && player != null && player.IsInVehicle(vehicle) && GameUtils.IsWithinFlat(vehicle.Position, target, _radius)) Complete();
        }
    }

    /// <summary>Capture by sustained close pursuit, with a living target required for the next scene.</summary>
    public sealed class CaptureBoatObjective : Objective
    {
        private readonly Func<Ped> _target;
        private readonly Func<Vehicle> _boat, _pursuer;
        private int _started, _close = -1;
        public Func<bool> CaptureReady { get; set; }
        public Func<string> DrivingHint { get; set; }
        public int DeadlineMs { get; set; } = 180000;
        public CaptureBoatObjective(Func<Ped> target, Func<Vehicle> boat, Func<Vehicle> pursuer) : base("Stay aboard the dinghy. Close within 25m of Mateo for 5 seconds; take him alive.")
        { _target = target; _boat = boat; _pursuer = pursuer; }
        public override void Enter(MissionContext c) { base.Enter(c); _started = Game.GameTime; _close = -1; }
        public override void Update(MissionContext c)
        {
            var target = _target(); var boat = _boat(); var chase = _pursuer();
            if (target == null || !target.Exists() || target.IsDead || boat == null || !boat.Exists() || !boat.IsDriveable) { Fail("Mateo must survive to explain the setup. Restart the mission."); return; }
            if (chase == null || !chase.Exists() || !chase.IsDriveable) { Fail("The crew's dinghy is lost."); return; }
            if (Game.GameTime - _started > DeadlineMs) { Fail("Mateo escaped into open water."); return; }
            ObjectiveMarkers.Navigation(boat.Position, null, chase); GameUtils.DrawObjectiveMarker(boat.Position, Color.Yellow, 3f);
            if(CaptureReady!=null&&!CaptureReady()) { _close=-1;Label=("Follow Mateo through the offshore run. Stay aboard. " + DrivingHint?.Invoke()).Trim();return; }
            Label="Stay aboard and close within 25m of Mateo for 5 seconds to stop him alive.";
            if (!IsOwnerActive(c) || !Game.Player.Character.IsInVehicle(chase) || chase.Position.DistanceTo(boat.Position) > 25f) { _close = -1; return; }
            if (_close < 0) _close = Game.GameTime;
            if (Game.GameTime - _close >= 5000) Complete();
        }
    }
}

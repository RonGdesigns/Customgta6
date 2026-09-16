using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Where the actors in a scene are looking, and what their faces are doing.
    ///
    /// Until now: nothing. There was not one <c>TASK_LOOK_AT_ENTITY</c> or facial call
    /// anywhere in the director, the blocking or the steps. Three men stood in a triangle
    /// facing whichever way they arrived, staring through each other with the neutral idle
    /// face a ped wears when nobody has told it anything, while lines of dialogue played.
    /// That is the single biggest reason the scenes read as scripted rather than acted, and
    /// it costs two natives to fix.
    ///
    /// What it does, per line:
    ///
    ///  * **Everyone looks at whoever is talking.** A listener turns his head and tracks
    ///    the speaker. Eye contact is most of what makes a conversation look like one.
    ///  * **The speaker glances back.** Not at the camera and not into the middle distance
    ///    — at one of the people he is talking to, changing between lines, which is what a
    ///    person does.
    ///  * **Faces move.** The talker gets a talking mood, listeners an attentive one, and
    ///    the scene's own tone can override both for a whole scene.
    ///
    /// Two rules, both learned elsewhere in this project. A look is issued **once per
    /// change of speaker** and left alone — re-issuing a head-track every frame restarts
    /// it before the neck has moved, the same fault that made guards stand still in M31.
    /// And every actor is put back: moods are cleared and looks released on every exit
    /// path, because a brother left wearing a scene's angry face walks around the rest of
    /// the session in it.
    /// </summary>
    public sealed class ScenePerformance
    {
        /// <summary>How long a head-track is asked for. Refreshed on the next speaker change.</summary>
        public const int LookMs = 8000;
        /// <summary>Past this there is nobody to look at; a look across a dockyard reads as a stare.</summary>
        public const float LookRange = 30f;
        /// <summary>How hard the head turns: 2048 is the head and neck without the whole torso.</summary>
        public const int LookFlags = 2048;
        /// <summary>Priority. 2 is "medium" — it yields to anything the mission is doing.</summary>
        public const int LookPriority = 2;

        /// <summary>The face of somebody mid-sentence.</summary>
        public const string TalkingMood = "mood_talking_1";
        /// <summary>The face of somebody being talked to.</summary>
        public const string ListeningMood = "mood_normal_1";

        private readonly List<Ped> _cast = new List<Ped>();
        private readonly Dictionary<int, string> _moods = new Dictionary<int, string>();
        private Ped _speaker;
        private string _tone;
        private readonly Random _random = new Random();

        /// <summary>Whether a scene's cast is currently being performed.</summary>
        public bool IsActive => _cast.Count > 0;
        /// <summary>Who this thinks is talking, for a test and for the log.</summary>
        public Ped Speaker => _speaker;
        /// <summary>The mood laid over the whole scene, if one was asked for.</summary>
        public string Tone => _tone;

        /// <summary>
        /// Take the cast. Called once as a scene opens, with everybody who might speak or
        /// be spoken to — the brothers and any support actor the scene created.
        /// </summary>
        /// <param name="tone">
        /// A mood for the whole scene, overriding the per-line ones: <c>mood_stressed_1</c>
        /// for a scene going wrong, <c>mood_angry_1</c> for an argument. Null leaves each
        /// actor to the talking and listening faces.
        /// </param>
        public void Begin(IEnumerable<Ped> cast, string tone = null)
        {
            End();
            _tone = tone;
            if (cast == null) return;
            foreach (var actor in cast)
            {
                if (actor == null || !actor.Exists() || actor.IsDead) continue;
                if (_cast.Any(p => p.Handle == actor.Handle)) continue;
                _cast.Add(actor);
                Wear(actor, _tone ?? ListeningMood);
            }
        }

        /// <summary>
        /// A new line is starting. Turn the room toward whoever is delivering it.
        /// Does nothing when the speaker has not changed, so a scene of six lines from one
        /// man is six lines, not six restarted head-turns.
        /// </summary>
        public void Speak(Ped speaker)
        {
            if (!IsActive) return;
            if (speaker == null || !speaker.Exists() || speaker.IsDead) return;
            if (_speaker != null && _speaker.Exists() && _speaker.Handle == speaker.Handle) return;

            // The previous speaker stops talking with his face.
            if (_speaker != null && _speaker.Exists()) Wear(_speaker, _tone ?? ListeningMood);
            _speaker = speaker;
            Wear(speaker, _tone ?? TalkingMood);

            foreach (var actor in _cast)
            {
                if (actor == null || !actor.Exists() || actor.IsDead) continue;
                if (actor.Handle == speaker.Handle) continue;
                if (actor.Position.DistanceTo(speaker.Position) > LookRange) continue;
                Look(actor, speaker);
            }

            // And he looks at one of them rather than through all of them. A different one
            // each line, which is the difference between a man talking and a man reciting.
            var listeners = _cast.Where(p => p != null && p.Exists() && !p.IsDead &&
                p.Handle != speaker.Handle && p.Position.DistanceTo(speaker.Position) <= LookRange).ToList();
            if (listeners.Count > 0) Look(speaker, listeners[_random.Next(listeners.Count)]);
        }

        /// <summary>Put every face and every head back, on every exit path.</summary>
        public void End()
        {
            foreach (var actor in _cast)
            {
                if (actor == null || !actor.Exists()) continue;
                Attempt(() => Function.Call(Hash.TASK_CLEAR_LOOK_AT, actor));
                Attempt(() => Function.Call(Hash.CLEAR_FACIAL_IDLE_ANIM_OVERRIDE, actor));
            }
            _cast.Clear();
            _moods.Clear();
            _speaker = null;
            _tone = null;
        }

        private void Look(Ped actor, Ped at)
        {
            Attempt(() => Function.Call(Hash.TASK_LOOK_AT_ENTITY, actor, at, LookMs, LookFlags, LookPriority));
        }

        /// <summary>
        /// Set a face, once. A mood re-applied every line restarts the animation, and the
        /// result is a face that twitches rather than one that acts.
        /// </summary>
        private void Wear(Ped actor, string mood)
        {
            if (actor == null || !actor.Exists() || string.IsNullOrEmpty(mood)) return;
            if (_moods.TryGetValue(actor.Handle, out string worn) && worn == mood) return;
            _moods[actor.Handle] = mood;
            Attempt(() => Function.Call(Hash.SET_FACIAL_IDLE_ANIM_OVERRIDE, actor, mood, 0));
        }

        private static void Attempt(Action action)
        {
            try { action(); }
            catch (Exception ex) { Logger.Warn("A scene performance call failed: " + ex.Message); }
        }
    }
}

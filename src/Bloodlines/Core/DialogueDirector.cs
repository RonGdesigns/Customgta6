using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using Bloodlines.Crew;
using GTA;

namespace Bloodlines.Core
{
    /// <summary>
    /// The bible's BloodlinesAudioManager: plays a dialogue cue as a speaker-colored
    /// subtitle, with the generated WAV underneath it when one exists.
    ///
    /// Audio is optional by design. The campaign has 255 written lines and no
    /// recordings yet, so every cue has to read correctly as text alone — the mod
    /// must never depend on a voice pack that may never be generated. Drop
    /// &lt;CUE_ID&gt;.wav into scripts/Bloodlines/audio/ and that line gains a voice
    /// with no code change (see tools/generate_voice.py).
    ///
    /// Lines queue rather than overlap: two characters talking over each other in a
    /// firefight is the fastest way to make scripted dialogue unreadable.
    /// </summary>
    public sealed class DialogueDirector
    {
        private const int MinimumLineMs = 1800;
        private const int MsPerCharacter = 55;

        private readonly CampaignData _data;
        private readonly string _root;
        private readonly Queue<DialogueCue> _queue = new Queue<DialogueCue>();

        private SoundPlayer _player;
        private DialogueCue _speaking;
        private int _speakingUntil;

        public DialogueDirector(CampaignData data, string bloodlinesRoot)
        {
            _data = data;
            _root = bloodlinesRoot;
            Directory.CreateDirectory(Path.Combine(bloodlinesRoot, "audio"));
        }

        /// <summary>
        /// Where a cue's WAV lives. The audio bank is partitioned per act and mission
        /// (audio/Act1/M01/M01_S1_01_ICE.wav) so no directory ends up holding hundreds
        /// of files; a flat audio/ folder still works as a fallback for quick tests.
        /// </summary>
        private string ResolveAudioPath(DialogueCue cue)
        {
            var info = _data.Mission(cue.MissionId);
            string fileName = cue.CueId + ".wav";

            if (info != null && !string.IsNullOrEmpty(info.AudioDirectory))
            {
                string partitioned = Path.Combine(_root, info.AudioDirectory.Replace('/', Path.DirectorySeparatorChar),
                    fileName);
                if (File.Exists(partitioned)) return partitioned;
            }

            string flat = Path.Combine(_root, "audio", fileName);
            return File.Exists(flat) ? flat : null;
        }

        public bool IsSpeaking => _speaking != null;
        public bool HasPending => _speaking != null || _queue.Count > 0;

        /// <summary>Queues one cue by its bible id (for example "M01_S1_01_ICE").</summary>
        public void Play(string cueId)
        {
            var cue = _data.Cue(cueId);
            if (cue == null)
            {
                Logger.Warn("Unknown dialogue cue: " + cueId);
                return;
            }

            Play(cue);
        }

        public void Play(DialogueCue cue)
        {
            if (cue == null) return;

            // A cue fired twice by a stage that re-enters should not stack up.
            if (_speaking != null && _speaking.CueId == cue.CueId) return;
            foreach (var queued in _queue)
            {
                if (queued.CueId == cue.CueId) return;
            }

            _queue.Enqueue(cue);
        }

        /// <summary>Queues every line the bible assigns to one stage of a mission.</summary>
        public void PlayStage(string missionId, int stage)
        {
            foreach (var cue in _data.Stage(missionId, stage)) Play(cue);
        }

        public void Update()
        {
            if (_speaking != null)
            {
                if (Game.GameTime < _speakingUntil)
                {
                    GTA.UI.Screen.ShowSubtitle(Format(_speaking), 100);
                    return;
                }

                _speaking = null;
            }

            if (_queue.Count == 0) return;

            _speaking = _queue.Dequeue();
            string audio = ResolveAudioPath(_speaking);
            _speakingUntil = Game.GameTime + Math.Max(DurationFor(_speaking), WaveTiming.DurationMs(audio) + 250);
            GTA.UI.Screen.ShowSubtitle(Format(_speaking), 100);
            PlayAudio(_speaking, audio);
            Logger.Debug("Cue " + _speaking.CueId + ": " + _speaking.Line);
        }

        private static int DurationFor(DialogueCue cue)
        {
            int length = (cue.Line ?? string.Empty).Length * MsPerCharacter;
            return Math.Max(MinimumLineMs, Math.Min(9000, length));
        }

        private static string Format(DialogueCue cue)
        {
            return ColourFor(cue.Speaker) + cue.Speaker + ":~s~ " + cue.Line;
        }

        /// <summary>
        /// Speaker colors match each character's blip so the switch HUD, the map and
        /// the subtitles all identify a character the same way.
        /// </summary>
        private static string ColourFor(string speaker)
        {
            if (string.IsNullOrEmpty(speaker)) return "~s~";

            switch (speaker.ToUpperInvariant())
            {
                case "ICE": return "~b~";
                case "GOHAN": return "~g~";
                case "GUESS": return "~o~";
                case "KJ": return "~p~";
                default: return "~r~"; // antagonists and one-off speakers
            }
        }

        private void PlayAudio(DialogueCue cue, string path)
        {
            try
            {
                _player?.Stop();
                _player?.Dispose();
                _player = null;
                if (path == null) return;
                _player = new SoundPlayer(path);
                _player.Play();
            }
            catch (Exception ex)
            {
                // A bad WAV must never take the mission down with it.
                Logger.Warn("Could not play " + cue.CueId + ".wav: " + ex.Message);
            }
        }

        /// <summary>Drops anything queued — used on mission abort and teardown.</summary>
        public void Clear()
        {
            _queue.Clear();
            _speaking = null;

            try
            {
                _player?.Stop();
                _player?.Dispose();
            }
            catch (Exception)
            {
            }
            finally
            {
                _player = null;
            }
        }

        public static string SpeakerFor(CrewSlot slot)
        {
            switch (slot)
            {
                case CrewSlot.Gohan: return "GOHAN";
                case CrewSlot.Guess: return "GUESS";
                default: return "ICE";
            }
        }
    }
}

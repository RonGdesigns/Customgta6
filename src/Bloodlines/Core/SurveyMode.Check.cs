using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Reading a spot, and checking a whole mission's worth of them without being asked one
    /// at a time.
    ///
    /// The measurement said where the work is: 1,062 of 1,091 keys are estimates, but 282 of
    /// those are one solo's race route and the rest are a median of nine per mission. So the
    /// cost is not the total — it is judging nine spots one at a time with nothing on screen
    /// saying whether any of them is already right.
    ///
    /// <see cref="Read"/> is the answer to that: everything the probes know about a point,
    /// judged against its key's kind by <see cref="SurveyReading"/>. The sweep runs it over
    /// every key of a mission and writes a worklist, so he flies to the two that are wrong
    /// instead of all nine — and <see cref="AcceptCurrent"/> records a key that checked out
    /// at the coordinates it already has, which is the honest meaning of having looked.
    ///
    /// **The sweep drives the ordinary teleport rather than a path of its own.** Collision
    /// loads around the focus and the man, so a point read from where he is standing reads as
    /// nothing; the teleport already moves both, waits on the collision and gives up cleanly
    /// when the terrain never arrives. A second traversal would be a second set of those
    /// mistakes.
    /// </summary>
    public sealed partial class SurveyMode
    {
        /// <summary>Whether something solid is within this much of the point's own height.</summary>
        public static Func<Vector3, float, bool> HeadroomProbe;
        /// <summary>Whether an interior loads at a point.</summary>
        public static Func<Vector3, bool> InteriorProbe;
        /// <summary>How far down a reading looks for the surface under a point.</summary>
        public const float ReadReach = 60f;
        /// <summary>And how far up it looks for a deckhead. Past this it is calling it open sky.</summary>
        public const float ReadCeiling = 12f;
        /// <summary>How often the readout is recomputed while he flies. Probes are rays, not free.</summary>
        public const int ReadIntervalMs = 250;
        /// <summary>How long the sweep lets collision settle after a teleport lands.</summary>
        public const int SweepDwellMs = 450;

        private SurveyReading _reading;
        private int _readAt;
        private Vector3 _readFrom;

        private readonly List<SurveyReading> _sweep = new List<SurveyReading>();
        private bool _sweeping;
        private int _sweepIndex, _sweepLanded;
        private string _sweepMission = "";

        /// <summary>True while the sweep is walking a mission's keys on its own.</summary>
        public bool IsSweeping => _sweeping;
        /// <summary>What the last sweep found, newest run only.</summary>
        public IEnumerable<SurveyReading> SweepResults => _sweep;
        /// <summary>The last reading taken, for a menu row that wants to state it.</summary>
        public SurveyReading LastReading => _reading;

        /// <summary>
        /// Everything the probes can say about a point, judged against the key it belongs to.
        /// Every probe is reached through an injected delegate for the same reason the two
        /// older ones are: this file compiles in a harness with no site-probing module behind
        /// it, and a missing probe has to read as "did not measure" rather than as a fault.
        /// </summary>
        public SurveyReading Read(MissionLocation location, Vector3 point)
        {
            var reading = new SurveyReading
            {
                Key = location?.Key ?? "", Kind = location?.Kind ?? "land", Point = point,
                Surface = Probe(point), SurfaceProbed = SurfaceProbe != null,
                Headroom = Above(point), HeadroomProbed = HeadroomProbe != null,
                Interior = InteriorProbe != null ? Inside(point) : (bool?)null,
                Room = Clearance(point), Water = WaterAt(point), Zone = ZoneAt(point),
            };
            if (location != null) Nearest(location, point, reading);
            return reading.Judge();
        }

        /// <summary>The current reading, recomputed on a clock rather than every frame.</summary>
        public SurveyReading Reading(MissionLocation location, Vector3 point)
        {
            if (_reading != null && _reading.Key == (location?.Key ?? "") &&
                Game.GameTime - _readAt < ReadIntervalMs && point.DistanceTo(_readFrom) < .5f)
                return _reading;
            _readAt = Game.GameTime;
            _readFrom = point;
            return _reading = Read(location, point);
        }

        private static float? Probe(Vector3 at)
        {
            try { return SurfaceProbe != null ? SurfaceProbe(at, ReadReach) : null; }
            catch (Exception ex) { Logger.Warn("A surface probe could not run: " + ex.Message); return null; }
        }

        private static float Above(Vector3 at)
        {
            // The probe answers yes or no at a height, so the distance comes from asking at
            // a few of them. Three rays, not a loop: this runs on a quarter-second clock
            // while he is flying, and a reading is worth less than a frame rate.
            try
            {
                if (HeadroomProbe == null) return float.MaxValue;
                foreach (float height in new[] { SurveyReading.PedHeadroom, 4f, ReadCeiling })
                    if (!HeadroomProbe(at, height)) return height;
                return float.MaxValue;
            }
            catch (Exception ex) { Logger.Warn("A headroom probe could not run: " + ex.Message); return float.MaxValue; }
        }

        private static bool Inside(Vector3 at)
        {
            try { return InteriorProbe != null && InteriorProbe(at); }
            catch (Exception ex) { Logger.Warn("An interior probe could not run: " + ex.Message); return false; }
        }

        private static float? WaterAt(Vector3 at)
        {
            try
            {
                var height = new OutputArgument();
                return Function.Call<bool>(Hash.GET_WATER_HEIGHT, at.X, at.Y, at.Z, height)
                    ? height.GetResult<float>() : (float?)null;
            }
            catch (Exception ex) { Logger.Warn("A water probe could not run: " + ex.Message); return null; }
        }

        private static string ZoneAt(Vector3 at)
        {
            // The zone is machine-read from the district text offline, and a key in the wrong
            // one makes validate_locations resolve somewhere the mission is not. City Hall's
            // plaza is in Burton whatever the building is called.
            try { return Function.Call<string>(Hash.GET_NAME_OF_ZONE, at.X, at.Y, at.Z) ?? ""; }
            catch { return ""; }
        }

        /// <summary>
        /// The nearest other key of the same mission. A site's shape is the thing an
        /// individual coordinate cannot show: M48's cordon spawned on top of the crew
        /// because one surveyed key ended up eighteen meters from another.
        /// </summary>
        private void Nearest(MissionLocation location, Vector3 point, SurveyReading reading)
        {
            int dot = location.Key.IndexOf('.');
            if (dot <= 0) return;
            string prefix = location.Key.Substring(0, dot + 1);
            MissionLocation best = null;
            float nearest = float.MaxValue;
            foreach (var other in _book.All)
            {
                if (other == location || !other.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                float away = other.Position.DistanceTo(point);
                if (away >= nearest) continue;
                nearest = away; best = other;
            }
            if (best == null) return;
            reading.NearestKey = best.Key.Substring(dot + 1);
            reading.NearestMeters = nearest;
        }

        // ===================================================================== the sweep

        /// <summary>
        /// Visit every key of one mission and record what is there. Opens the ordinary survey
        /// queue, so stopping it, capturing inside it and the camera all behave as they always
        /// do; the sweep only supplies the presses.
        /// </summary>
        public bool BeginSweep(string mission)
        {
            if (string.IsNullOrEmpty(mission)) return false;
            Start(mission);
            if (!IsActive) return false;
            _sweep.Clear();
            _sweepMission = mission;
            _sweeping = true;
            _sweepIndex = -1;
            _sweepLanded = 0;
            Select(0);
            GameUtils.Notify("~g~Checking " + _queue.Count + " spots in " + mission + ".~s~\n" +
                "It teleports through them; anything that needs a look goes in the report.");
            return true;
        }

        public void CancelSweep()
        {
            if (!_sweeping) return;
            _sweeping = false;
            GameUtils.Notify("~y~Check stopped after " + _sweep.Count + " of " + _queue.Count + " spots.");
        }

        /// <summary>How the sweep is getting on, for a menu row.</summary>
        public string SweepProgress
        {
            get
            {
                if (!_sweeping && _sweep.Count == 0) return "not run yet";
                int bad = _sweep.Count(r => !r.Fits);
                string where = _sweeping ? "checking " + (_sweep.Count + 1) + " of " + _queue.Count : _sweep.Count + " checked";
                return where + ", " + (bad == 0 ? "none need a look" : bad + " need a look");
            }
        }

        /// <summary>
        /// One step of the sweep, driven from <see cref="Update"/>. A key is read once its
        /// teleport has landed and the collision has had a moment to settle; a teleport that
        /// rolled back records that rather than a measurement nobody took.
        /// </summary>
        private void SweepStep()
        {
            if (!_sweeping) return;
            if (!IsActive || Current == null) { FinishSweep(); return; }
            if (IsTeleporting) return;

            if (_sweepIndex != _index)
            {
                // A fresh key: send him to it and wait. The camera goes too, so the point is
                // in frame and the streaming focus is on it.
                _sweepIndex = _index;
                _sweepLanded = 0;
                TeleportToCurrent();
                if (IsTeleporting) return;
                // Nothing to teleport with — no player, or a dead one. Read where it stands
                // rather than stalling the sweep forever.
            }
            if (_sweepLanded == 0) _sweepLanded = Game.GameTime;
            if (Game.GameTime - _sweepLanded < SweepDwellMs) return;

            var location = Current;
            var reading = Read(location, location.Position);
            _sweep.Add(reading);
            Logger.Info("Survey check " + reading.Line());
            if (_index + 1 >= _queue.Count) { FinishSweep(); return; }
            Select(_index + 1);
        }

        private void FinishSweep()
        {
            if (!_sweeping) return;
            _sweeping = false;
            int bad = _sweep.Count(r => !r.Fits);
            string path = WriteSweepReport();
            GameUtils.Notify(bad == 0
                ? "~g~All " + _sweep.Count + " spots in " + _sweepMission + " check out.~s~\nAccept them in place, or fly one and place it yourself."
                : "~o~" + bad + " of " + _sweep.Count + " spots in " + _sweepMission + " need a look.~s~\n" +
                  (path == null ? "See Bloodlines.log." : "Written to " + Path.GetFileName(path) + "."));
        }

        /// <summary>
        /// The worklist. Written beside the survey ini, the way the staging report is written
        /// beside it, because a report nobody can find is a report nobody reads.
        /// </summary>
        public string WriteSweepReport()
        {
            try
            {
                string directory = OutputDirectory;
                if (string.IsNullOrEmpty(directory)) return null;
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "Bloodlines.Survey-Check.txt");
                var text = new StringBuilder();
                text.AppendLine("Bloodlines survey check - " + _sweepMission + " - " +
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                text.AppendLine("A spot that checks out is placeable. Whether it is the right");
                text.AppendLine("place for the beat is still yours to say.");
                text.AppendLine();
                foreach (var reading in _sweep.Where(r => !r.Fits)) text.AppendLine(reading.Line());
                if (_sweep.All(r => r.Fits)) text.AppendLine("Nothing needs a look.");
                text.AppendLine();
                text.AppendLine("--- everything checked ---");
                foreach (var reading in _sweep.Where(r => r.Fits)) text.AppendLine(reading.Line());
                File.WriteAllText(path, text.ToString());
                Logger.Info("Wrote the survey check to " + path + ".");
                return path;
            }
            catch (Exception ex) { Logger.Error("Writing the survey check", ex); return null; }
        }

        // ================================================================ accepting in place

        /// <summary>
        /// Record the key under the cursor at the coordinates it already has. This is a
        /// capture that does not move the point: the camera survey is already a coordinate
        /// placed by looking at it rather than by standing on it, so a spot he has looked at
        /// and found right is surveyed, and the status stops calling it a guess.
        ///
        /// It refuses a spot the probes complained about, because accepting one of those is
        /// recording the estimate as verified while it is still wrong.
        /// </summary>
        public bool AcceptCurrent()
        {
            var location = Current;
            if (!IsActive || location == null || IsTeleporting || IsEditing) return false;
            var reading = Read(location, location.Position);
            if (!reading.Fits)
            {
                GameUtils.Notify("~r~" + location.Key + " has not checked out.~s~\n" + reading.Complaint +
                    "\nFly to it and place it instead.");
                return false;
            }
            _book.Record(location.Key, location.Position, location.Heading);
            if (!Write()) return false;
            _captured.Add(location.Key);
            Logger.Info("Accepted " + location.Key + " in place at " + location.Position + "; " + reading.Hud() + ".");
            GameUtils.Notify("~g~" + location.Key + " accepted where it is.~s~ " + reading.Hud());
            Next();
            return true;
        }

        /// <summary>
        /// Record every key the last sweep found nothing wrong with, at the coordinates it
        /// already has. The one that turns nine judgments into one press and a short list.
        /// </summary>
        public int AcceptClean()
        {
            if (_sweep.Count == 0) { GameUtils.Notify("~y~Check the mission's spots first."); return 0; }
            int taken = 0;
            foreach (var reading in _sweep.Where(r => r.Fits))
            {
                var location = _book.Get(reading.Key);
                if (location == null || location.Status == LocationStatus.Surveyed) continue;
                _book.Record(location.Key, location.Position, location.Heading);
                _captured.Add(location.Key);
                taken++;
            }
            if (taken == 0) { GameUtils.Notify("~y~Nothing left to accept."); return 0; }
            if (!Write()) return 0;
            int left = _sweep.Count(r => !r.Fits);
            Logger.Info("Accepted " + taken + " checked spots in place for " + _sweepMission + ".");
            GameUtils.Notify("~g~" + taken + " spots accepted where they are.~s~\n" +
                (left == 0 ? "That is the whole mission." : left + " still need a look."));
            return taken;
        }
    }
}

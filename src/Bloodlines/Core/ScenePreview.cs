using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Bloodlines.Missions;
using GTA;
using GTA.Math;
using GTA.UI;

namespace Bloodlines.Core
{
    /// <summary>
    /// A mission's world, staged and standing still, with nothing running.
    ///
    /// Until now the only way to see where a mission puts things was to play it: start
    /// the attempt, sit through the briefing, and find out on arrival that a guard is
    /// inside a hull or a marker is over water. That loop is why 1,061 of 1,091
    /// coordinates are still estimates — checking one costs a mission.
    ///
    /// This runs a mission's <c>Setup</c> and stops there. No stages, no objectives, no
    /// timers, no progression, no rewards. The Cargobob, the hauler, the crates and the
    /// guards stand where the mission would have put them, pacified, and the survey
    /// camera flies around them.
    ///
    /// **It owns nothing of its own.** The tempting design is a parallel entity pool that
    /// the preview deletes on the way out. That is a second owner for the same peds, which
    /// is the exact class of bug this project keeps hitting. Instead the mission tracks its
    /// own staging exactly as it always does, and closing the preview calls the mission's
    /// own <see cref="Mission.Cleanup"/> — the teardown path every attempt already uses and
    /// every teardown test already covers. There is no new ownership to get wrong.
    ///
    /// The crew are put back where they were standing. A <c>Setup</c> is allowed to deploy
    /// them, and a tool that leaves three brothers on an airfield has moved the player's
    /// world to look at it.
    /// </summary>
    public sealed class ScenePreview
    {
        /// <summary>How far from a staged entity an authored key still explains it.</summary>
        public const float AnchorMatchMeters = 12f;
        /// <summary>Peds and vehicles this far apart are drawn with different markers.</summary>
        public const float MarkerHeight = 1.2f;

        private Mission _mission;
        private readonly List<Vector3> _crewWas = new List<Vector3>();
        private readonly List<Crew.CrewSlot> _crewSlots = new List<Crew.CrewSlot>();
        private MissionContext _context;
        private LocationBook _book;

        public bool IsActive => _mission != null;
        public string MissionId { get; private set; }
        public string MissionTitle { get; private set; }
        /// <summary>What the staging pass produced, written the moment it finishes.</summary>
        public IReadOnlyList<string> Report { get; private set; } = new List<string>();
        /// <summary>Where the report was written, so it can be read outside the game.</summary>
        public string ReportPath { get; private set; }
        /// <summary>Why the last attempt to stage a mission did not work.</summary>
        public string Refusal { get; private set; }

        /// <summary>
        /// Stage a mission and stop. Returns false when the mission's own Setup refused,
        /// which is itself worth knowing: a mission that will not stage will not play.
        /// </summary>
        public bool Open(MissionDefinition definition, MissionContext context, LocationBook book, string outputDirectory)
        {
            Close();
            Refusal = null;
            if (definition == null || context == null) { Refusal = "No mission was selected."; return false; }
            Mission mission;
            try { mission = definition.Factory(); }
            catch (Exception ex)
            {
                Refusal = definition.Id + "'s script could not be constructed: " + ex.Message;
                Logger.Error("Scene preview could not construct " + definition.Id, ex);
                return false;
            }
            if (mission == null) { Refusal = definition.Id + " has no gameplay script to stage."; return false; }

            _context = context;
            _book = book;
            RememberCrew(context);
            book?.BeginUse();
            bool staged;
            try { staged = mission.StageForPreview(context); }
            catch (Exception ex)
            {
                Logger.Error("Scene preview staging " + definition.Id, ex);
                try { mission.Cleanup(); } catch (Exception inner) { Logger.Error("Scene preview teardown after a failed stage", inner); }
                RestoreCrew();
                Refusal = definition.Id + " threw while staging: " + ex.Message;
                return false;
            }
            if (!staged)
            {
                try { mission.Cleanup(); } catch (Exception ex) { Logger.Error("Scene preview teardown after a refusal", ex); }
                RestoreCrew();
                Refusal = definition.Id + " refused to stage. Its Setup returned false — see the log.";
                Logger.Warn("Scene preview: " + Refusal);
                return false;
            }

            _mission = mission;
            MissionId = definition.Id;
            MissionTitle = definition.Title;
            Pacify();
            Report = Describe(book);
            ReportPath = Write(outputDirectory);
            Logger.Info("Scene preview open: " + MissionId + " staged " + _mission.Staged.Count(e => e != null && e.Exists()) +
                        " entities" + (ReportPath != null ? "; wrote " + ReportPath : "") + ".");
            return true;
        }

        /// <summary>
        /// Put it all away. The mission's own teardown does the deleting, so a preview
        /// cannot leave a second Cargobob behind for the real attempt to collide with.
        /// </summary>
        public void Close()
        {
            if (_mission == null) { _context = null; return; }
            var mission = _mission;
            _mission = null;
            // Out of the staged vehicles first. Mission teardown deliberately hands back a
            // vehicle a brother is sitting in rather than deleting it under him — right for
            // a mission that just passed, and a leak here: a previewed truck with Gohan in
            // the cab would still be standing when the real attempt spawns its own.
            try { Disembark(mission); }
            catch (Exception ex) { Logger.Error("Scene preview could not clear the crew from the staging", ex); }
            try { mission.Cleanup(); }
            catch (Exception ex) { Logger.Error("Scene preview teardown", ex); }
            RestoreCrew();
            Logger.Info("Scene preview closed: " + MissionId + ".");
            MissionId = null; MissionTitle = null; _context = null;
        }

        /// <summary>
        /// Take the brothers and the player out of anything this staging created, so
        /// teardown has no reason to spare it.
        /// </summary>
        private void Disembark(Mission mission)
        {
            if (_context?.Crew == null) return;
            var staged = new HashSet<int>(mission.Staged.Where(e => e is Vehicle && e.Exists()).Select(e => e.Handle));
            var riders = new List<Ped>();
            foreach (var hero in Crew.Protagonist.All)
            {
                var ped = _context.Crew.PedFor(hero.Slot);
                if (ped != null && ped.Exists()) riders.Add(ped);
            }
            var player = Game.Player.Character;
            if (player != null && player.Exists() && !riders.Any(p => p.Handle == player.Handle)) riders.Add(player);
            foreach (var ped in riders)
            {
                var ride = ped.CurrentVehicle;
                if (ride == null || !ride.Exists() || !staged.Contains(ride.Handle)) continue;
                try { ped.Task.ClearAllImmediately(); ped.Task.LeaveVehicle(LeaveVehicleFlags.WarpOut); }
                catch (Exception ex) { Logger.Warn("A rider could not be taken out of a staged vehicle: " + ex.Message); }
            }
        }

        /// <summary>
        /// Nothing staged for looking at should be able to act. A guard who opens fire on
        /// the surveyor is a guard he cannot stand next to and measure.
        /// </summary>
        private void Pacify()
        {
            foreach (var entity in _mission.Staged.ToList())
            {
                if (entity == null || !entity.Exists()) continue;
                try
                {
                    if (entity is Ped ped)
                    {
                        ped.Task.ClearAllImmediately();
                        ped.BlockPermanentEvents = true;
                        ped.IsInvincible = true;
                        ped.Task.StandStill(-1);
                    }
                    else if (entity is Vehicle vehicle)
                    {
                        vehicle.IsEngineRunning = false;
                        vehicle.IsInvincible = true;
                        vehicle.IsPositionFrozen = true;
                    }
                }
                catch (Exception ex) { Logger.Warn("Scene preview could not pacify an entity: " + ex.Message); }
            }
        }

        /// <summary>
        /// What is standing where, and which of it came from a key.
        ///
        /// This is the part no data file can answer. M18 works out its rear interaction
        /// point from the hauler's own dimensions and the ground under it; M45 probes the
        /// deck; M49 takes its lane from the nearest road node. Those positions exist only
        /// in memory during an attempt. Here they exist on a line, with the distance to the
        /// nearest authored key beside them, so a derived point is visibly derived.
        /// </summary>
        private List<string> Describe(LocationBook book)
        {
            var lines = new List<string>
            {
                "Staging preview: " + MissionId + " — " + MissionTitle,
                "Nothing is running. Positions are where this mission's Setup actually put things.",
                ""
            };
            var keys = book?.All.ToList() ?? new List<MissionLocation>();
            int index = 0;
            foreach (var entity in _mission.Staged)
            {
                if (entity == null || !entity.Exists()) continue;
                index++;
                var at = entity.Position;
                string what = entity is Ped ? "ped" : entity is Vehicle ? "vehicle" : "prop";
                string model = "?";
                try
                {
                    if (entity is Ped person) model = person.Model.Hash.ToString();
                    else if (entity is Vehicle car) model = car.Model.Hash.ToString();
                    else if (entity is Prop thing) model = thing.Model.Hash.ToString();
                }
                catch { model = "?"; }
                var nearest = keys
                    .Select(k => new { k.Key, Away = k.Position.DistanceTo(at) })
                    .OrderBy(k => k.Away).FirstOrDefault();
                string anchor = nearest != null && nearest.Away <= AnchorMatchMeters
                    ? nearest.Key + " +" + nearest.Away.ToString("0.0") + " m"
                    : "derived — no key within " + AnchorMatchMeters.ToString("0") + " m" +
                      (nearest != null ? " (nearest " + nearest.Key + " at " + nearest.Away.ToString("0") + " m)" : "");
                lines.Add(index.ToString().PadLeft(3) + "  " + what.PadRight(8) + " " + model.PadRight(12) +
                          " (" + at.X.ToString("0.00") + ", " + at.Y.ToString("0.00") + ", " + at.Z.ToString("0.00") + ")" +
                          "  h " + entity.Heading.ToString("0") + "  " + anchor);
            }
            if (index == 0) lines.Add("Nothing was staged. This mission's Setup places nothing, or it placed and failed.");

            var unverified = book?.Unverified.ToList() ?? new List<MissionLocation>();
            if (unverified.Count > 0)
            {
                lines.Add("");
                lines.Add("Keys this mission used that have never been surveyed (" + unverified.Count + "):");
                foreach (var key in unverified) lines.Add("    " + key.Key + "  " + key.Kind);
            }
            return lines;
        }

        private string Write(string directory)
        {
            if (string.IsNullOrEmpty(directory)) return null;
            try
            {
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "Bloodlines.Staging.txt");
                File.WriteAllLines(path, Report);
                return path;
            }
            catch (Exception ex) { Logger.Warn("The staging report could not be written: " + ex.Message); return null; }
        }

        private void RememberCrew(MissionContext context)
        {
            _crewWas.Clear(); _crewSlots.Clear();
            if (context?.Crew == null) return;
            foreach (var hero in Crew.Protagonist.All)
            {
                var ped = context.Crew.PedFor(hero.Slot);
                if (ped == null || !ped.Exists()) continue;
                _crewSlots.Add(hero.Slot); _crewWas.Add(ped.Position);
            }
        }

        /// <summary>
        /// A Setup is allowed to deploy the crew, and a tool that leaves three brothers on
        /// an airfield has moved the player's world in order to look at it.
        /// </summary>
        private void RestoreCrew()
        {
            if (_context?.Crew == null) { _crewWas.Clear(); _crewSlots.Clear(); return; }
            for (int i = 0; i < _crewSlots.Count && i < _crewWas.Count; i++)
            {
                try
                {
                    var ped = _context.Crew.PedFor(_crewSlots[i]);
                    if (ped == null || !ped.Exists() || ped.IsDead) continue;
                    if (ped.Position.DistanceTo(_crewWas[i]) < 2f) continue;
                    ped.Position = _crewWas[i];
                }
                catch (Exception ex) { Logger.Warn("A brother could not be put back after a preview: " + ex.Message); }
            }
            _crewWas.Clear(); _crewSlots.Clear();
        }

        /// <summary>Markers over everything staged, and a line saying what is open.</summary>
        public void Update()
        {
            if (_mission == null) return;
            foreach (var entity in _mission.Staged)
            {
                if (entity == null || !entity.Exists()) continue;
                var color = entity is Ped ? Color.FromArgb(150, 232, 96, 72)
                    : entity is Vehicle ? Color.FromArgb(150, 96, 168, 232)
                    : Color.FromArgb(150, 232, 168, 56);
                GameUtils.DrawObjectiveMarker(entity.Position + new Vector3(0f, 0f, MarkerHeight), color, .3f);
            }
            // The mission's routes, if it has any numbered keys.
            if (_book != null && MissionId != null)
                RouteRibbons.Draw(_book, MissionId, Game.Player.Character?.Position ?? Vector3.Zero);
            new TextElement("STAGING PREVIEW — " + MissionId + " — nothing is running. B / Backspace closes it.",
                new PointF(40f, 34f), .3f, Color.FromArgb(235, 232, 168, 56)).Draw();
        }
    }
}

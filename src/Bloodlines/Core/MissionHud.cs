using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;

namespace Bloodlines.Core
{
    /// <summary>
    /// The mission HUD: what to do, whose job it is, how far away, and how it is going.
    ///
    /// It used to be three lines of yellow text. Everything else the player needed was
    /// either not drawn at all - which brother the beat belongs to, how far the marker is -
    /// or drawn somewhere it could not survive: <c>TimerObjective</c> pushed its clock as a
    /// subtitle, and <c>ComposedMission</c> pushes the objective text into the same subtitle
    /// slot every tick, so M55's five minutes never showed. The timer is a bar here, owned
    /// by the HUD, and the objective still reads the way it did.
    ///
    /// The 1280 by 720 drawing space is SHVDN's, the same one the dev menu and the customs
    /// card use. Everything here is read from the running mission; nothing is stored.
    /// </summary>
    public static class MissionHud
    {
        // Ron's first look at it: too big, taking up too much of the screen, the
        // information solid. Every size here is about two thirds of what it was.
        public const float Left = 24f, Top = 74f, Width = 400f;
        public const int WrapColumns = 56;
        public const float HeadingScale = .21f, ChipScale = .21f, ObjectiveScale = .24f, SmallScale = .18f, TimerScale = .23f;
        /// <summary>A timer this close to running out is drawn in red.</summary>
        public const int UrgentSeconds = 30;

        private static readonly Color Panel = Color.FromArgb(150, 12, 14, 18);
        private static readonly Color Muted = Color.FromArgb(255, 178, 195, 211);
        private static readonly Color Track = Color.FromArgb(220, 40, 44, 52);
        private static readonly Color Objective = Color.FromArgb(255, 245, 222, 90);
        private static readonly Color Good = Color.FromArgb(255, 150, 230, 160);
        private static readonly Color Urgent = Color.FromArgb(255, 245, 110, 90);

        /// <summary>Everything the HUD says about this frame, so a test can read it without a screen.</summary>
        public sealed class Frame
        {
            public string Heading = "", Objective = "", Owner = "", Distance = "", Progress = "", Timer = "";
            public CrewSlot? OwnerSlot;
            public float ProgressFraction = -1f, TimerFraction = -1f;
            public bool TimerUrgent;
            public IReadOnlyList<string> Rules = new string[0];
        }

        /// <summary>Work out the frame from the running mission. Pure, and the whole of the logic.</summary>
        public static Frame Compose(MissionManager missions, CrewRoster crew)
        {
            var frame = new Frame();
            if (missions == null || !missions.IsRunning) return frame;
            var definition = missions.LastAttempted;
            string stage = missions.CurrentStageName;
            frame.Heading = (definition != null ? definition.Id + "  |  " : "") + (missions.CurrentTitle ?? "") +
                            (string.IsNullOrEmpty(stage) ? "" : "  |  " + stage);
            frame.Objective = missions.CurrentObjective ?? "";
            int bar = frame.Objective.IndexOf(" | ", StringComparison.Ordinal);
            if (bar > 0)
            {
                frame.Rules = frame.Objective.Substring(bar + 3).Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries);
                frame.Objective = frame.Objective.Substring(0, bar);
            }

            var objectives = missions.CurrentObjectives;
            if (objectives == null || crew == null) return frame;
            var active = crew.ActiveSlot;
            // The beat being shown is the one the objective text describes: the first live
            // objective this brother owns, otherwise the first live one anybody owns.
            var current = objectives.FirstOrDefault(o => !o.IsPassive && !o.IsFinished &&
                              (!o.RequiredCharacter.HasValue || o.RequiredCharacter.Value == active))
                          ?? objectives.FirstOrDefault(o => !o.IsPassive && !o.IsFinished);
            if (current != null)
            {
                var owner = current.RequiredCharacter ?? active;
                frame.OwnerSlot = owner;
                frame.Owner = Protagonist.Of(owner).Handle + (missions.RequiredSwitch.HasValue ? "  -  switch" : "");
                var at = current.AssignmentPosition;
                var player = Game.Player.Character;
                if (at.HasValue && player != null && player.Exists())
                    frame.Distance = Meters(player.Position.DistanceTo(at.Value));
                Describe(current, frame);
            }

            var timer = objectives.OfType<TimerObjective>().FirstOrDefault(t => !t.IsFinished && t.ShowsClock);
            if (timer != null && timer.Total > 0)
            {
                frame.Timer = timer.Remaining / 60 + ":" + (timer.Remaining % 60).ToString("00");
                frame.TimerFraction = Math.Max(0f, Math.Min(1f, timer.Remaining / (float)timer.Total));
                frame.TimerUrgent = timer.Remaining <= UrgentSeconds;
            }
            return frame;
        }

        /// <summary>How the live beat is going, for the kinds that have a number to show.</summary>
        private static void Describe(Objective current, Frame frame)
        {
            var gauge = current as GaugeObjective;
            if (gauge != null)
            {
                float span = Math.Max(.001f, gauge.Maximum - gauge.Minimum);
                frame.ProgressFraction = Math.Max(0f, Math.Min(1f, (gauge.Value - gauge.Minimum) / span));
                frame.Progress = gauge.Value.ToString("0.0") + (string.IsNullOrEmpty(gauge.Unit) ? "" : " " + gauge.Unit) +
                                 (gauge.InBand ? "  -  in the band" : "");
                return;
            }
            var align = current as AlignObjective;
            if (align != null)
            {
                frame.ProgressFraction = Math.Max(0f, Math.Min(1f, align.Strength));
                frame.Progress = "signal " + (int)Math.Round(align.Strength * 100f) + "%" + (align.Locked ? "  -  locked" : "");
                return;
            }
            var hull = current as ShootDownObjective;
            if (hull != null)
            {
                // The meter Ron asked for: how much of the aircraft is left to shoot.
                frame.ProgressFraction = Math.Max(0f, Math.Min(1f, hull.Hull));
                frame.Progress = "hull " + (int)Math.Round(hull.Hull * 100f) + "%";
                return;
            }
            var holds = current as MultiHoldObjective;
            if (holds != null && holds.Total > 0)
            {
                int done = holds.Total - holds.Remaining;
                frame.ProgressFraction = done / (float)holds.Total;
                frame.Progress = done + " of " + holds.Total;
            }
        }

        private static string Meters(float distance) =>
            distance >= 1000f ? (distance / 1000f).ToString("0.0") + " km" : ((int)Math.Round(distance)) + " m";

        /// <summary>Draw this frame. Called by the host once a tick while a mission runs and no menu is open.</summary>
        public static void Draw(MissionManager missions, CrewRoster crew)
        {
            var frame = Compose(missions, crew);
            if (frame.Heading.Length == 0 && frame.Objective.Length == 0) return;

            var lines = MissionObjectiveHud.Wrap(frame.Objective, WrapColumns).Take(3).ToList();
            float y = Top;
            float height = 17f + (frame.OwnerSlot.HasValue ? 18f : 0f) + lines.Count * 18f +
                           (frame.Rules.Count > 0 ? 14f : 0f) + (frame.ProgressFraction >= 0f ? 17f : 0f) + (frame.TimerFraction >= 0f ? 18f : 0f) + 6f;
            new ContainerElement(new PointF(Left - 6f, y - 4f), new SizeF(Width, height), Panel).Draw();

            new TextElement(frame.Heading, new PointF(Left, y), HeadingScale, Color.White).Draw();
            y += 17f;

            if (frame.OwnerSlot.HasValue)
            {
                // A chip in his color and his name, so a glance says whose beat this is.
                new ContainerElement(new PointF(Left, y + 3f), new SizeF(9f, 9f), CrewColors.Of(frame.OwnerSlot.Value)).Draw();
                new TextElement(frame.Owner, new PointF(Left + 14f, y), ChipScale, CrewColors.Of(frame.OwnerSlot.Value)).Draw();
                if (frame.Distance.Length > 0)
                    new TextElement(frame.Distance, new PointF(Left + Width - 18f, y), ChipScale, Muted) { Alignment = Alignment.Right }.Draw();
                y += 18f;
            }

            foreach (var line in lines)
            {
                new TextElement(line, new PointF(Left, y), ObjectiveScale, Objective).Draw();
                y += 18f;
            }
            if (frame.Rules.Count > 0)
            {
                new TextElement(string.Join("   ", frame.Rules.ToArray()), new PointF(Left, y), SmallScale, Muted).Draw();
                y += 14f;
            }
            if (frame.ProgressFraction >= 0f)
            {
                Bar(Left, y + 5f, Width - 30f, 5f, frame.ProgressFraction, frame.Progress.Contains("band") || frame.Progress.Contains("locked") ? Good : Objective);
                new TextElement(frame.Progress, new PointF(Left + Width - 18f, y - 2f), SmallScale, Muted) { Alignment = Alignment.Right }.Draw();
                y += 17f;
            }
            if (frame.TimerFraction >= 0f)
            {
                Bar(Left, y + 6f, Width - 30f, 5f, frame.TimerFraction, frame.TimerUrgent ? Urgent : Color.White);
                new TextElement(frame.Timer, new PointF(Left + Width - 18f, y - 2f), TimerScale, frame.TimerUrgent ? Urgent : Color.White) { Alignment = Alignment.Right }.Draw();
            }
        }

        private static void Bar(float x, float y, float width, float height, float fraction, Color fill)
        {
            new ContainerElement(new PointF(x, y), new SizeF(width, height), Track).Draw();
            if (fraction > 0f) new ContainerElement(new PointF(x, y), new SizeF(width * fraction, height), fill).Draw();
        }
    }
}

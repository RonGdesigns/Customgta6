using System;
using System.Collections.Generic;
using System.Linq;

namespace Bloodlines.Core
{
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>One structured fact collected while a mission is being prepared or run.</summary>
    public sealed class MissionDiagnostic
    {
        public int Sequence { get; internal set; }
        public DiagnosticSeverity Severity { get; internal set; }
        public string Category { get; internal set; }
        public string Subject { get; internal set; }
        public string Message { get; internal set; }

        public override string ToString()
        {
            string subject = string.IsNullOrWhiteSpace(Subject) ? "" : " [" + Subject + "]";
            return Severity + " " + Category + subject + ": " + Message;
        }
    }

    /// <summary>
    /// Small bounded diagnostic ledger for the currently inspected mission. It is
    /// deliberately separate from CampaignState: diagnostics never alter progress,
    /// saves, coordinates or gameplay. New placement/operation code can report the
    /// exact contract it checked and the dev UI can surface the same ledger later.
    /// </summary>
    public sealed class MissionDoctor
    {
        public const int Capacity = 64;
        private readonly List<MissionDiagnostic> _entries = new List<MissionDiagnostic>();
        private int _sequence;

        public string MissionId { get; private set; } = "";
        public string MissionTitle { get; private set; } = "";
        public IReadOnlyList<MissionDiagnostic> Entries => _entries;
        public int ErrorCount => _entries.Count(e => e.Severity == DiagnosticSeverity.Error);
        public int WarningCount => _entries.Count(e => e.Severity == DiagnosticSeverity.Warning);
        public MissionDiagnostic Last => _entries.Count == 0 ? null : _entries[_entries.Count - 1];

        public void BeginMission(string id, string title)
        {
            _entries.Clear();
            _sequence = 0;
            MissionId = id ?? "";
            MissionTitle = title ?? "";
            Info("mission", MissionId, "diagnostic session started");
        }

        public void Clear()
        {
            _entries.Clear();
            _sequence = 0;
            MissionId = "";
            MissionTitle = "";
        }

        public void Info(string category, string subject, string message) =>
            Record(DiagnosticSeverity.Info, category, subject, message);

        public void Warn(string category, string subject, string message) =>
            Record(DiagnosticSeverity.Warning, category, subject, message);

        public void Error(string category, string subject, string message) =>
            Record(DiagnosticSeverity.Error, category, subject, message);

        public void Record(DiagnosticSeverity severity, string category, string subject, string message)
        {
            if (_entries.Count >= Capacity) _entries.RemoveAt(0);
            var entry = new MissionDiagnostic
            {
                Sequence = ++_sequence,
                Severity = severity,
                Category = string.IsNullOrWhiteSpace(category) ? "general" : category.Trim(),
                Subject = subject?.Trim() ?? "",
                Message = string.IsNullOrWhiteSpace(message) ? "no detail supplied" : message.Trim()
            };
            _entries.Add(entry);

            // Keep the normal Bloodlines log useful even before the dev-menu page is
            // wired to this ledger. Diagnostic logging is intentionally one line per
            // event rather than a tick-rate stream.
            string text = "Doctor " + (string.IsNullOrEmpty(MissionId) ? "" : MissionId + " ") + entry;
            if (severity == DiagnosticSeverity.Error) Logger.Error(text);
            else if (severity == DiagnosticSeverity.Warning) Logger.Warn(text);
            else Logger.Debug(text);
        }

        public string Summary()
        {
            string name = string.IsNullOrEmpty(MissionId) ? "No mission" : MissionId + (string.IsNullOrEmpty(MissionTitle) ? "" : " — " + MissionTitle);
            return name + ": " + ErrorCount + " error(s), " + WarningCount + " warning(s), " + _entries.Count + " diagnostic event(s).";
        }

        public IEnumerable<string> Lines(int max = 12)
        {
            if (max < 1) yield break;
            foreach (var entry in _entries.Skip(Math.Max(0, _entries.Count - max))) yield return entry.ToString();
        }
    }
}

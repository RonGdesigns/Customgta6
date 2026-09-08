using System;
using System.Collections.Generic;
using System.IO;

namespace Bloodlines.Core
{
    /// <summary>
    /// Minimal tab-separated table reader for the campaign data files.
    ///
    /// TSV rather than JSON on purpose: .NET Framework 4.8 has no built-in JSON
    /// reader, and pulling a serializer into a script mod means shipping another
    /// DLL that has to be version-matched against everyone else's. Tabs are
    /// stripped at generation time, so a split is a complete parse.
    /// </summary>
    public sealed class DataTable
    {
        private readonly List<string[]> _rows = new List<string[]>();
        private readonly Dictionary<string, int> _columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public int Count => _rows.Count;

        public static DataTable Load(string path)
        {
            var table = new DataTable();

            if (!File.Exists(path))
            {
                Logger.Error("Data file missing: " + path);
                return table;
            }

            var lines = File.ReadAllLines(path);
            if (lines.Length == 0)
            {
                Logger.Error("Data file empty: " + path);
                return table;
            }

            var header = lines[0].Split('\t');
            for (int i = 0; i < header.Length; i++) table._columns[header[i].Trim()] = i;

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                table._rows.Add(lines[i].Split('\t'));
            }

            Logger.Info("Loaded " + table._rows.Count + " rows from " + Path.GetFileName(path));
            return table;
        }

        public IEnumerable<Row> Rows
        {
            get
            {
                foreach (var values in _rows) yield return new Row(this, values);
            }
        }

        internal string Value(string[] values, string column)
        {
            if (!_columns.TryGetValue(column, out int index)) return string.Empty;
            return index < values.Length ? values[index] : string.Empty;
        }

        public struct Row
        {
            private readonly DataTable _table;
            private readonly string[] _values;

            internal Row(DataTable table, string[] values)
            {
                _table = table;
                _values = values;
            }

            public string Text(string column) => _table.Value(_values, column);

            public int Int(string column)
            {
                return int.TryParse(Text(column), out int value) ? value : 0;
            }

            public float Float(string column)
            {
                return float.TryParse(Text(column), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float value)
                    ? value
                    : 0f;
            }
        }
    }
}

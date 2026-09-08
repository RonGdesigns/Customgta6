using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bloodlines.Core
{
    /// <summary>
    /// A very small JSON reader and writer, sufficient for the save file and the
    /// campaign registry.
    ///
    /// .NET Framework 4.8 has no built-in JSON support, and pulling a serializer into
    /// a script mod means shipping another DLL that every other mod in the folder has
    /// to agree with. This handles objects, arrays, strings, numbers, booleans and
    /// null — everything the save format uses and nothing more.
    /// </summary>
    public static class Json
    {
        // ---------- writing ----------

        public static string Write(object value)
        {
            var builder = new StringBuilder();
            WriteValue(builder, value, 0);
            return builder.ToString();
        }

        private static void WriteValue(StringBuilder builder, object value, int depth)
        {
            switch (value)
            {
                case null:
                    builder.Append("null");
                    return;
                case string text:
                    WriteString(builder, text);
                    return;
                case bool flag:
                    builder.Append(flag ? "true" : "false");
                    return;
                case float number:
                    builder.Append(number.ToString("0.####", CultureInfo.InvariantCulture));
                    return;
                case double number:
                    builder.Append(number.ToString("0.####", CultureInfo.InvariantCulture));
                    return;
                case IDictionary<string, object> map:
                    WriteObject(builder, map, depth);
                    return;
                case IEnumerable list when !(value is string):
                    WriteArray(builder, list, depth);
                    return;
                default:
                    builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    return;
            }
        }

        private static void WriteObject(StringBuilder builder, IDictionary<string, object> map, int depth)
        {
            string pad = new string(' ', (depth + 1) * 2);
            builder.Append("{\n");

            bool first = true;
            foreach (var pair in map)
            {
                if (!first) builder.Append(",\n");
                first = false;
                builder.Append(pad);
                WriteString(builder, pair.Key);
                builder.Append(": ");
                WriteValue(builder, pair.Value, depth + 1);
            }

            builder.Append("\n").Append(new string(' ', depth * 2)).Append("}");
        }

        private static void WriteArray(StringBuilder builder, IEnumerable list, int depth)
        {
            builder.Append("[");
            bool first = true;
            foreach (var item in list)
            {
                if (!first) builder.Append(", ");
                first = false;
                WriteValue(builder, item, depth + 1);
            }
            builder.Append("]");
        }

        private static void WriteString(StringBuilder builder, string text)
        {
            builder.Append('"');
            foreach (char character in text ?? string.Empty)
            {
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 0x20) builder.Append("\\u").Append(((int)character).ToString("x4"));
                        else builder.Append(character);
                        break;
                }
            }
            builder.Append('"');
        }

        // ---------- reading ----------

        /// <summary>Parses JSON into dictionaries, lists, strings, doubles, bools and nulls.</summary>
        public static object Read(string text)
        {
            int index = 0;
            var value = ReadValue(text, ref index);
            return value;
        }

        private static object ReadValue(string text, ref int index)
        {
            SkipWhitespace(text, ref index);
            if (index >= text.Length) throw new FormatException("Unexpected end of JSON.");

            char character = text[index];
            switch (character)
            {
                case '{': return ReadObject(text, ref index);
                case '[': return ReadArray(text, ref index);
                case '"': return ReadString(text, ref index);
                case 't': Expect(text, ref index, "true"); return true;
                case 'f': Expect(text, ref index, "false"); return false;
                case 'n': Expect(text, ref index, "null"); return null;
                default: return ReadNumber(text, ref index);
            }
        }

        private static Dictionary<string, object> ReadObject(string text, ref int index)
        {
            var map = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            index++; // '{'

            SkipWhitespace(text, ref index);
            if (index < text.Length && text[index] == '}') { index++; return map; }

            while (index < text.Length)
            {
                SkipWhitespace(text, ref index);
                string key = ReadString(text, ref index);
                SkipWhitespace(text, ref index);
                if (index >= text.Length || text[index] != ':') throw new FormatException("Expected ':'.");
                index++;
                map[key] = ReadValue(text, ref index);
                SkipWhitespace(text, ref index);

                if (index >= text.Length) break;
                if (text[index] == ',') { index++; continue; }
                if (text[index] == '}') { index++; return map; }
                throw new FormatException("Expected ',' or '}'.");
            }

            throw new FormatException("Unterminated object.");
        }

        private static List<object> ReadArray(string text, ref int index)
        {
            var list = new List<object>();
            index++; // '['

            SkipWhitespace(text, ref index);
            if (index < text.Length && text[index] == ']') { index++; return list; }

            while (index < text.Length)
            {
                list.Add(ReadValue(text, ref index));
                SkipWhitespace(text, ref index);

                if (index >= text.Length) break;
                if (text[index] == ',') { index++; continue; }
                if (text[index] == ']') { index++; return list; }
                throw new FormatException("Expected ',' or ']'.");
            }

            throw new FormatException("Unterminated array.");
        }

        private static string ReadString(string text, ref int index)
        {
            if (text[index] != '"') throw new FormatException("Expected a string.");
            index++;

            var builder = new StringBuilder();
            while (index < text.Length)
            {
                char character = text[index++];
                if (character == '"') return builder.ToString();

                if (character != '\\')
                {
                    builder.Append(character);
                    continue;
                }

                if (index >= text.Length) break;
                char escape = text[index++];
                switch (escape)
                {
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'u':
                        builder.Append((char)Convert.ToInt32(text.Substring(index, 4), 16));
                        index += 4;
                        break;
                    default: builder.Append(escape); break;
                }
            }

            throw new FormatException("Unterminated string.");
        }

        private static double ReadNumber(string text, ref int index)
        {
            int start = index;
            while (index < text.Length && "+-.eE0123456789".IndexOf(text[index]) >= 0) index++;

            string slice = text.Substring(start, index - start);
            if (double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                return value;
            }

            throw new FormatException("Bad number: " + slice);
        }

        private static void Expect(string text, ref int index, string literal)
        {
            if (index + literal.Length > text.Length ||
                string.CompareOrdinal(text, index, literal, 0, literal.Length) != 0)
            {
                throw new FormatException("Expected " + literal + ".");
            }

            index += literal.Length;
        }

        private static void SkipWhitespace(string text, ref int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
        }

        // ---------- typed access helpers ----------

        public static Dictionary<string, object> Object(object value)
        {
            return value as Dictionary<string, object> ?? new Dictionary<string, object>();
        }

        public static string String(Dictionary<string, object> map, string key, string fallback = "")
        {
            return map.TryGetValue(key, out var value) && value != null ? value.ToString() : fallback;
        }

        public static int Int(Dictionary<string, object> map, string key, int fallback = 0)
        {
            return map.TryGetValue(key, out var value) && value is double number ? (int)number : fallback;
        }

        public static float Float(Dictionary<string, object> map, string key, float fallback = 0f)
        {
            return map.TryGetValue(key, out var value) && value is double number ? (float)number : fallback;
        }

        public static bool Bool(Dictionary<string, object> map, string key, bool fallback = false)
        {
            return map.TryGetValue(key, out var value) && value is bool flag ? flag : fallback;
        }

        public static List<object> Array(Dictionary<string, object> map, string key)
        {
            return map.TryGetValue(key, out var value) ? value as List<object> ?? new List<object>()
                : new List<object>();
        }
    }
}

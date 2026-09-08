using System;
using System.IO;
using System.Text;

namespace Bloodlines.Core
{
    /// <summary>
    /// File logger. SHVDN swallows most exceptions into its own log, which makes
    /// mission debugging miserable; every subsystem in this mod writes here instead.
    /// </summary>
    public static class Logger
    {
        private static readonly object Gate = new object();

        // Written with a BOM on purpose. Without one, Windows PowerShell's `type`
        // and Notepad read a UTF-8 file as ANSI, so a single em dash in a logged
        // dialogue line turns the header into "Bloodlines a?? session" and every
        // reader assumes the mod is corrupt. The BOM costs three bytes.
        private static readonly Encoding LogEncoding = new UTF8Encoding(true);

        private static string _path = "Bloodlines.log";
        private static bool _verbose;

        // A fault inside OnTick repeats at the frame rate. The first run of this mod
        // produced ~100,000 identical stack traces in about ninety seconds, which
        // buries the one line before it that says what was actually happening. An
        // error identical to the last one is counted instead of written, and the
        // total is flushed when something else happens or the log closes.
        private static string _lastError;
        private static int _repeatCount;

        public static void Configure(string path, bool verbose)
        {
            _path = path;
            _verbose = verbose;
            lock (Gate)
            {
                try
                {
                    File.WriteAllText(_path,
                        "=== Los Santos: Bloodlines - session " + DateTime.Now.ToString("u") + " ===" + Environment.NewLine,
                        LogEncoding);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    // A locked log file is never worth taking the game down for.
                }
            }
        }

        public static void Info(string message) => Write("INFO ", message);

        public static void Warn(string message) => Write("WARN ", message);

        public static void Error(string message, Exception ex = null)
        {
            var text = ex == null ? message : message + " :: " + ex;
            lock (Gate)
            {
                if (text == _lastError)
                {
                    _repeatCount++;
                    return;
                }
                FlushRepeats();
                _lastError = text;
            }
            Write("ERROR", text);
        }

        /// <summary>Report and clear any suppressed repeat run. Caller holds the gate.</summary>
        private static void FlushRepeats()
        {
            if (_repeatCount == 0) return;
            var count = _repeatCount;
            _repeatCount = 0;
            _lastError = null;
            WriteLocked("ERROR", "(the previous error repeated " + count + " more time(s))");
        }

        public static void Debug(string message)
        {
            if (_verbose) Write("DEBUG", message);
        }

        private static void Write(string level, string message)
        {
            lock (Gate)
            {
                if (level != "ERROR") FlushRepeats();
                WriteLocked(level, message);
            }
        }

        private static void WriteLocked(string level, string message)
        {
            {
                try
                {
                    File.AppendAllText(_path,
                        DateTime.Now.ToString("HH:mm:ss.fff") + " [" + level + "] " + message + Environment.NewLine,
                        LogEncoding);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                }
            }
        }
    }
}

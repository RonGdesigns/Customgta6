using System;
using System.IO;

namespace Bloodlines.Core
{
    /// <summary>
    /// File logger. SHVDN swallows most exceptions into its own log, which makes
    /// mission debugging miserable; every subsystem in this mod writes here instead.
    /// </summary>
    public static class Logger
    {
        private static readonly object Gate = new object();
        private static string _path = "Bloodlines.log";
        private static bool _verbose;

        public static void Configure(string path, bool verbose)
        {
            _path = path;
            _verbose = verbose;
            lock (Gate)
            {
                try
                {
                    File.WriteAllText(_path,
                        "=== Los Santos: Bloodlines — session " + DateTime.Now.ToString("u") + " ===" + Environment.NewLine);
                }
                catch (IOException)
                {
                    // A locked log file is never worth taking the game down for.
                }
            }
        }

        public static void Info(string message) => Write("INFO ", message);

        public static void Warn(string message) => Write("WARN ", message);

        public static void Error(string message, Exception ex = null)
        {
            Write("ERROR", ex == null ? message : message + " :: " + ex);
        }

        public static void Debug(string message)
        {
            if (_verbose) Write("DEBUG", message);
        }

        private static void Write(string level, string message)
        {
            lock (Gate)
            {
                try
                {
                    File.AppendAllText(_path,
                        DateTime.Now.ToString("HH:mm:ss.fff") + " [" + level + "] " + message + Environment.NewLine);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}

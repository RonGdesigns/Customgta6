using System;
using System.IO;

namespace Bloodlines.Core
{
    /// <summary>Reads RIFF chunk lengths without loading the recording into memory.</summary>
    public static class WaveTiming
    {
        public static int DurationMs(string path)
        {
            if (path == null) return 0;
            try
            {
                using var stream = File.OpenRead(path);
                using var reader = new BinaryReader(stream);
                if (stream.Length < 12 || reader.ReadUInt32() != 0x46464952) return 0;
                reader.ReadUInt32();
                if (reader.ReadUInt32() != 0x45564157) return 0;
                uint rate = 0, bytes = 0;
                while (stream.Position + 8 <= stream.Length)
                {
                    uint kind = reader.ReadUInt32(), length = reader.ReadUInt32();
                    long end = stream.Position + length;
                    if (end > stream.Length) return 0;
                    if (kind == 0x20746d66 && length >= 16)
                    {
                        reader.ReadUInt16(); reader.ReadUInt16(); reader.ReadUInt32();
                        rate = reader.ReadUInt32();
                    }
                    else if (kind == 0x61746164) bytes = length;
                    stream.Position = end + (length % 2);
                }
                return rate == 0 ? 0 : (int)Math.Min(120000L, (long)bytes * 1000 / rate);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return 0;
            }
        }
    }
}

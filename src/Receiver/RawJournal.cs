using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
namespace Visep.Receiver
{
    internal sealed class RawJournal
    {
        internal const int MaximumBytes = 1048576;
        private readonly string directory;
        internal RawJournal(string inbox) { directory = Path.Combine(inbox, "journal"); }
        internal IEnumerable<string> Entries() { return Directory.EnumerateFiles(directory, "*.raw").OrderBy(File.GetLastWriteTimeUtc).ThenBy(Path.GetFileName, StringComparer.Ordinal); }
        internal static byte[] ReadBounded(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > MaximumBytes) throw new IOException("Mensagem excede limite de bytes; preservada na origem.");
                using (var output = new MemoryStream())
                {
                    var buffer = new byte[8192]; int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (output.Length + read > MaximumBytes) throw new IOException("Limite de bytes excedido.");
                        output.Write(buffer, 0, read);
                    }
                    return output.ToArray();
                }
            }
        }
        private static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        internal byte[] Read(string path)
        {
            byte[] bytes = ReadBounded(path);
            if (!String.Equals(Path.GetFileNameWithoutExtension(path), Hash(bytes), StringComparison.Ordinal))
                throw new InvalidDataException("Journal corrompido.");
            return bytes;
        }
        internal void Append(byte[] bytes)
        {
            Directory.CreateDirectory(directory);
            string target = Path.Combine(directory, Hash(bytes) + ".raw");
            if (File.Exists(target)) { Read(target); return; }
            string temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                File.Move(temporary, target);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}

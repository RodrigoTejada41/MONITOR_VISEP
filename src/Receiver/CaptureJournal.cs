using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
namespace Visep.Receiver
{
    internal sealed class CaptureJournal
    {
        private readonly string directory;
        internal CaptureJournal(string inbox) { directory = Path.Combine(inbox, "captures"); }
        internal string Append(string source, byte[] payload)
        {
            Directory.CreateDirectory(directory);
            string id = Guid.NewGuid().ToString("N"), hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(payload)).Replace("-", "").ToLowerInvariant();
            string path = Path.Combine(directory, id + ".xml");
            Save(path, new XElement("capture", new XAttribute("id", id),
                new XAttribute("source", Path.GetFileName(source)),
                new XAttribute("capturedUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)),
                new XAttribute("payloadSha256", hash), new XAttribute("state", "pending")));
            return path;
        }
        internal void SetState(string path, string state)
        {
            XElement current = Load(path);
            var updated = new XElement(current);
            updated.SetAttributeValue("state", state);
            Save(path, updated);
        }
        internal Dictionary<string, List<string>> PendingIndex(Action<string> reportFailure)
        {
            var index = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            if (!Directory.Exists(directory))
            {
                if (File.Exists(directory)) reportFailure(directory);
                return index;
            }
            try
            {
                foreach (string path in Directory.EnumerateFiles(directory, "*.xml"))
                {
                    try
                    {
                        XElement capture = Load(path);
                        if ((string)capture.Attribute("state") != "pending") continue;
                        string hash = (string)capture.Attribute("payloadSha256");
                        List<string> paths;
                        if (!index.TryGetValue(hash, out paths)) index.Add(hash, new List<string> { path });
                        else paths.Add(path);
                    }
                    catch (Exception) { reportFailure(path); }
                }
            }
            catch (Exception) { reportFailure(directory); }
            return index;
        }
        internal void DeliverPending(string hash, Dictionary<string, List<string>> index, Action<string> reportFailure)
        {
            List<string> paths;
            if (!index.TryGetValue(hash, out paths)) return;
            foreach (string path in paths)
            {
                try { SetState(path, "delivered"); }
                catch (Exception) { reportFailure(path); }
            }
        }
        internal static XElement Load(string path)
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 8192 };
            XElement capture;
            using (var reader = XmlReader.Create(path, settings)) capture = XElement.Load(reader);
            string id = (string)capture.Attribute("id"), hash = (string)capture.Attribute("payloadSha256");
            string state = (string)capture.Attribute("state"), source = (string)capture.Attribute("source");
            string capturedUtc = (string)capture.Attribute("capturedUtc");
            Guid parsedId; DateTime parsedUtc;
            if (capture.Name != "capture" || capture.HasElements || !String.IsNullOrWhiteSpace(capture.Value) ||
                !Guid.TryParseExact(id, "N", out parsedId) || id != Path.GetFileNameWithoutExtension(path) ||
                hash == null || hash.Length != 64 || (state != "pending" && state != "captured" && state != "delivered" && state != "invalid") ||
                String.IsNullOrEmpty(source) || source == "." || source == ".." || source != Path.GetFileName(source) ||
                source.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                !DateTime.TryParseExact(capturedUtc, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsedUtc) ||
                parsedUtc.Kind != DateTimeKind.Utc) throw new InvalidDataException("Envelope de captura invalido.");
            foreach (char character in hash)
                if ((character < '0' || character > '9') && (character < 'a' || character > 'f'))
                    throw new InvalidDataException("Hash de captura invalido.");
            return capture;
        }
        private static void Save(string path, XElement capture)
        {
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { capture.Save(stream); stream.Flush(true); }
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}

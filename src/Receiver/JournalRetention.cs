using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Visep.Receiver
{
    [DataContract]
    internal sealed class BackupEntry
    {
        [DataMember] public string Path { get; set; }
        [DataMember] public string Sha256 { get; set; }
    }

    internal sealed class RetentionResult
    {
        internal int CandidateCaptures { get; private set; }
        internal int CandidatePayloads { get; private set; }
        internal int DeletedCaptures { get; private set; }
        internal int DeletedPayloads { get; private set; }
        internal RetentionResult(int captures, int payloads) { CandidateCaptures = captures; CandidatePayloads = payloads; }
        internal void CaptureDeleted() { DeletedCaptures++; }
        internal void PayloadDeleted() { DeletedPayloads++; }
    }

    internal static class JournalRetention
    {
        private sealed class CaptureRecord
        {
            internal string Path;
            internal string Hash;
            internal string State;
            internal DateTime CapturedUtc;
        }

        internal static RetentionResult Execute(string dataFile, string backupDirectory, DateTime cutoffUtc, bool apply)
        {
            if (cutoffUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Cutoff deve ser UTC.");
            string dataDirectory = NormalizeDirectory(Path.GetDirectoryName(Path.GetFullPath(dataFile)));
            string backup = NormalizeDirectory(backupDirectory);
            if (SamePath(dataDirectory, backup) || IsNested(dataDirectory, backup) || IsNested(backup, dataDirectory))
                throw new InvalidDataException("Backup deve ficar fora do diretorio de dados.");
            string inbox = ReceiverProgram.Inbox(dataFile);
            if (File.Exists(inbox)) throw new InvalidDataException("Diretorio inbox invalido.");
            if (!Directory.Exists(inbox))
            {
                ValidateBackup(dataDirectory, backup, Enumerable.Empty<string>());
                return new RetentionResult(0, 0);
            }
            using (new FileStream(Path.Combine(inbox, "receiver.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                List<CaptureRecord> captures = ReadCaptures(Path.Combine(inbox, "captures"));
                var candidateCaptures = new List<CaptureRecord>();
                var candidatePayloads = new List<string>();
                string journalDirectory = Path.Combine(inbox, "journal");
                foreach (var group in captures.GroupBy(x => x.Hash, StringComparer.Ordinal))
                {
                    if (group.Any(x => (x.State != "delivered" && x.State != "invalid") || x.CapturedUtc >= cutoffUtc)) continue;
                    string payload = Path.Combine(journalDirectory, group.Key + ".raw");
                    if (!File.Exists(payload)) continue;
                    new RawJournal(inbox).Read(payload);
                    candidateCaptures.AddRange(group);
                    candidatePayloads.Add(payload);
                }
                ValidateBackup(dataDirectory, backup, candidateCaptures.Select(x => x.Path).Concat(candidatePayloads));
                var result = new RetentionResult(candidateCaptures.Count, candidatePayloads.Count);
                if (!apply) return result;
                foreach (CaptureRecord capture in candidateCaptures) { File.Delete(capture.Path); result.CaptureDeleted(); }
                foreach (string payload in candidatePayloads) { File.Delete(payload); result.PayloadDeleted(); }
                return result;
            }
        }

        private static List<CaptureRecord> ReadCaptures(string directory)
        {
            var result = new List<CaptureRecord>();
            if (File.Exists(directory)) throw new InvalidDataException("Diretorio de capturas invalido.");
            if (!Directory.Exists(directory)) return result;
            foreach (string path in Directory.EnumerateFiles(directory, "*.xml"))
            {
                try
                {
                    XElement capture = CaptureJournal.Load(path);
                    result.Add(new CaptureRecord {
                        Path = path,
                        Hash = (string)capture.Attribute("payloadSha256"),
                        State = (string)capture.Attribute("state"),
                        CapturedUtc = DateTime.Parse((string)capture.Attribute("capturedUtc"), null, System.Globalization.DateTimeStyles.RoundtripKind)
                    });
                }
                catch (Exception ex) { throw new InvalidDataException("Metadata de captura invalida; retencao cancelada.", ex); }
            }
            return result;
        }

        private static void ValidateBackup(string dataDirectory, string backup, IEnumerable<string> candidates)
        {
            string manifestPath = Path.Combine(backup, "manifest.json");
            if (!File.Exists(manifestPath)) throw new InvalidDataException("Manifesto de backup ausente.");
            List<BackupEntry> entries;
            try
            {
                byte[] content = File.ReadAllBytes(manifestPath);
                if (content.Length > 16 * 1024 * 1024) throw new InvalidDataException("Manifesto excede limite de 16 MiB.");
                int offset = content.Length >= 3 && content[0] == 0xef && content[1] == 0xbb && content[2] == 0xbf ? 3 : 0;
                using (var stream = new MemoryStream(content, offset, content.Length - offset, false))
                    entries = (List<BackupEntry>)new DataContractJsonSerializer(typeof(List<BackupEntry>)).ReadObject(stream);
            }
            catch (Exception ex) { throw new InvalidDataException("Manifesto de backup invalido.", ex); }
            if (entries == null) throw new InvalidDataException("Manifesto de backup vazio.");
            var manifest = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (BackupEntry entry in entries)
            {
                if (entry == null || String.IsNullOrEmpty(entry.Path) || String.IsNullOrEmpty(entry.Sha256) || manifest.ContainsKey(entry.Path))
                    throw new InvalidDataException("Entrada de backup invalida ou duplicada.");
                manifest.Add(entry.Path, entry.Sha256);
            }
            foreach (string current in candidates)
            {
                string relative = current.Substring(dataDirectory.Length + 1);
                string expected;
                if (!manifest.TryGetValue(relative, out expected)) throw new InvalidDataException("Candidato ausente no manifesto de backup.");
                string backupFile = Path.GetFullPath(Path.Combine(backup, relative));
                if (!IsNested(backup, backupFile) || !File.Exists(backupFile)) throw new InvalidDataException("Candidato ausente no backup.");
                string backupHash = Hash(backupFile), currentHash = Hash(current);
                if (!String.Equals(backupHash, expected, StringComparison.OrdinalIgnoreCase) ||
                    !String.Equals(currentHash, expected, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Candidato diverge do backup verificado.");
            }
        }

        private static string Hash(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }

        private static string NormalizeDirectory(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool SamePath(string left, string right)
        {
            return String.Equals(NormalizeDirectory(left), NormalizeDirectory(right), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNested(string parent, string child)
        {
            string prefix = NormalizeDirectory(parent) + Path.DirectorySeparatorChar;
            return Path.GetFullPath(child).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}

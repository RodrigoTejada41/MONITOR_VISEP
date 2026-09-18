using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Xml.Linq;
using Visep.Receiver;

class RetentionTests
{
    static int count;
    static void Check(bool ok, string message) { count++; if (!ok) throw new Exception(message); }
    static string Hash(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }
    static void SetCapture(string path, string state, DateTime capturedUtc)
    {
        XElement xml = XElement.Load(path); xml.SetAttributeValue("state", state); xml.SetAttributeValue("capturedUtc", capturedUtc.ToString("o")); xml.Save(path);
    }
    static void Backup(string dataDirectory, string backup)
    {
        Directory.CreateDirectory(backup);
        var entries = new StringBuilder("["); bool first = true;
        foreach (string source in Directory.GetFiles(dataDirectory, "*", SearchOption.AllDirectories))
        {
            string relative = source.Substring(dataDirectory.Length + 1);
            string target = Path.Combine(backup, relative); Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(source, target);
            if (!first) entries.Append(','); first = false;
            entries.Append("{\"Path\":\"").Append(relative.Replace("\\", "\\\\")).Append("\",\"Sha256\":\"").Append(Hash(source)).Append("\"}");
        }
        entries.Append(']'); File.WriteAllText(Path.Combine(backup, "manifest.json"), entries.ToString(), new UTF8Encoding(true));
    }
    static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "visep-retention-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string dataDirectory = Path.Combine(root, "data"), data = Path.Combine(dataDirectory, "data.xml");
            string inbox = Path.Combine(dataDirectory, "inbox"); Directory.CreateDirectory(inbox); File.WriteAllText(data, "<Visep />");
            var raw = new RawJournal(inbox); var captures = new CaptureJournal(inbox);
            DateTime cutoff = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

            byte[] oldBytes = Encoding.UTF8.GetBytes("old-terminal"); raw.Append(oldBytes);
            string oldCapture = captures.Append("old.xml", oldBytes); SetCapture(oldCapture, "delivered", cutoff.AddDays(-1));
            byte[] pendingBytes = Encoding.UTF8.GetBytes("old-pending"); raw.Append(pendingBytes);
            string pendingCapture = captures.Append("pending.xml", pendingBytes); SetCapture(pendingCapture, "pending", cutoff.AddDays(-1));
            byte[] sharedBytes = Encoding.UTF8.GetBytes("shared"); raw.Append(sharedBytes);
            string sharedOld = captures.Append("shared-old.xml", sharedBytes); SetCapture(sharedOld, "delivered", cutoff.AddDays(-2));
            string sharedNew = captures.Append("shared-new.xml", sharedBytes); SetCapture(sharedNew, "delivered", cutoff.AddDays(1));
            byte[] orphanBytes = Encoding.UTF8.GetBytes("legacy-orphan"); raw.Append(orphanBytes);
            byte[] capturedBytes = Encoding.UTF8.GetBytes("sg3-captured"); raw.Append(capturedBytes);
            string capturedCapture = captures.Append("sg3-tcp", capturedBytes); SetCapture(capturedCapture, "captured", cutoff.AddDays(-1));

            string backup = Path.Combine(root, "backup"); Backup(dataDirectory, backup);
            bool sameDirectoryRejected = false;
            try { JournalRetention.Execute(data, dataDirectory, cutoff, false); } catch (InvalidDataException) { sameDirectoryRejected = true; }
            Check(sameDirectoryRejected, "data directory cannot be its own backup");
            string emptyData = Path.Combine(root, "empty", "data.xml");
            JournalRetention.Execute(emptyData, backup, cutoff, false);
            Check(!Directory.Exists(Path.Combine(root, "empty", "inbox")), "preview with no inbox does not mutate data directory");
            Check(File.Exists(oldCapture) && raw.Entries().Count() == 5, "same-directory backup rejection has no deletion");

            RetentionResult preview = JournalRetention.Execute(data, backup, cutoff, false);
            Check(preview.CandidateCaptures == 1 && preview.CandidatePayloads == 1, "only complete old terminal family planned");
            Check(File.Exists(oldCapture), "preview does not delete capture");
            Check(raw.Entries().Count() == 5, "preview does not delete payload");

            RetentionResult applied = JournalRetention.Execute(data, backup, cutoff, true);
            Check(applied.DeletedCaptures == 1 && applied.DeletedPayloads == 1, "apply deletes planned family");
            Check(!File.Exists(oldCapture), "old terminal capture deleted");
            Check(File.Exists(pendingCapture), "pending capture preserved");
            Check(File.Exists(capturedCapture), "unparsed SG3 capture preserved");
            Check(File.Exists(sharedOld) && File.Exists(sharedNew), "family with new capture preserved");
            Check(raw.Entries().Count() == 4, "pending captured shared and orphan payloads preserved");
            RetentionResult repeated = JournalRetention.Execute(data, backup, cutoff, true);
            Check(repeated.DeletedCaptures == 0 && repeated.DeletedPayloads == 0, "second apply is idempotent");

            byte[] blockedBytes = Encoding.UTF8.GetBytes("backup-mismatch"); raw.Append(blockedBytes);
            string blockedCapture = captures.Append("blocked.xml", blockedBytes); SetCapture(blockedCapture, "invalid", cutoff.AddDays(-1));
            bool backupRejected = false;
            try { JournalRetention.Execute(data, backup, cutoff, true); } catch (InvalidDataException) { backupRejected = true; }
            Check(backupRejected, "missing backup entry rejects apply");
            Check(File.Exists(blockedCapture), "backup rejection has no deletion");

            string corrupt = Path.Combine(inbox, "captures", "corrupt.xml"); File.WriteAllText(corrupt, "corrupt");
            bool corruptRejected = false;
            try { JournalRetention.Execute(data, backup, cutoff, false); } catch (InvalidDataException) { corruptRejected = true; }
            Check(corruptRejected, "corrupt metadata blocks whole plan");
            File.Delete(corrupt);

            using (var held = new FileStream(Path.Combine(inbox, "receiver.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                bool locked = false;
                try { JournalRetention.Execute(data, backup, cutoff, false); } catch (IOException) { locked = true; }
                Check(locked, "receiver lock blocks retention");
            }
            Console.WriteLine("Retention tests passed: " + count); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Xml.Linq;
using Visep.Receiver;

class MonitoringTests
{
    static int count;
    static void Check(bool ok, string message) { count++; if (!ok) throw new Exception(message); }
    static void SetCaptured(string path, DateTime value)
    {
        XElement xml = XElement.Load(path); xml.SetAttributeValue("capturedUtc", value.ToString("o")); xml.Save(path);
    }
    static string Hash(byte[] value)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(value)).Replace("-", "").ToLowerInvariant();
    }

    static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "visep-monitoring-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string data = Path.Combine(root, "data.xml"), inbox = Path.Combine(root, "inbox");
            Directory.CreateDirectory(inbox);
            DateTime now = new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);

            ReceiverHealth empty = ReceiverMonitor.Inspect(data, 100, TimeSpan.FromMinutes(5), now, 1000);
            Check(empty.IsHealthy, "empty inbox with enough disk is healthy");
            Check(empty.AvailableBytes == 1000, "available bytes reported");
            Check(!ReceiverMonitor.Inspect(data, 1000, TimeSpan.FromMinutes(5), now, 1000).LowDiskSpace, "equal free-space threshold is healthy");

            byte[] raw = Encoding.UTF8.GetBytes("<simulation id='health' account='1' code='130' zone='0' partition='1' />");
            new RawJournal(inbox).Append(raw);
            var captures = new CaptureJournal(inbox);
            string delivered = captures.Append("delivered.xml", raw);
            captures.SetState(delivered, "delivered");
            SetCaptured(delivered, now);
            byte[] invalidBytes = Encoding.UTF8.GetBytes("invalid");
            new RawJournal(inbox).Append(invalidBytes);
            string invalid = captures.Append("invalid.xml", invalidBytes);
            captures.SetState(invalid, "invalid");
            SetCaptured(invalid, now);
            string pending = captures.Append("pending.xml", raw);
            SetCaptured(pending, now.AddMinutes(-10));
            string captured = captures.Append("sg3-tcp", raw);
            captures.SetState(captured, "captured");
            SetCaptured(captured, now.AddMinutes(-10));
            File.WriteAllText(Path.Combine(inbox, "waiting.xml"), "pending input");

            ReceiverHealth degraded = ReceiverMonitor.Inspect(data, 100, TimeSpan.FromMinutes(5), now, 1000);
            Check(degraded.DeliveredCaptures == 1, "delivered count");
            Check(degraded.InvalidCaptures == 1, "invalid count");
            Check(degraded.PendingCaptures == 1, "pending count");
            Check(degraded.CapturedCaptures == 1, "SG3 captured count");
            Check(degraded.StalePendingCaptures == 1, "stale pending count");
            Check(degraded.InboxFiles == 1, "inbox pending file count");
            Check(!degraded.IsHealthy, "stale pending is unhealthy");

            ReceiverHealth lowDisk = ReceiverMonitor.Inspect(data, 2000, TimeSpan.FromHours(1), now, 1000);
            Check(lowDisk.LowDiskSpace, "low disk detected");
            Check(!lowDisk.IsHealthy, "low disk is unhealthy");

            string orphanRoot = Path.Combine(root, "orphan-only"), orphanData = Path.Combine(orphanRoot, "data.xml"), orphanInbox = Path.Combine(orphanRoot, "inbox");
            Directory.CreateDirectory(orphanInbox);
            new RawJournal(orphanInbox).Append(Encoding.UTF8.GetBytes("orphan-only"));
            ReceiverHealth orphanOnly = ReceiverMonitor.Inspect(orphanData, 0, TimeSpan.FromHours(1), now, 1000);
            Check(orphanOnly.UnreferencedPayloads == 1, "unreferenced payload counted");
            Check(!orphanOnly.IsHealthy, "unreferenced payload is unhealthy");

            byte[] missingBytes = Encoding.UTF8.GetBytes("missing raw");
            string missing = captures.Append("missing.xml", missingBytes);
            captures.SetState(missing, "delivered");
            SetCaptured(missing, now);
            byte[] orphanBytes = Encoding.UTF8.GetBytes("legacy raw without capture");
            new RawJournal(inbox).Append(orphanBytes);
            string future = captures.Append("future.xml", raw);
            SetCaptured(future, now.AddMinutes(1));
            ReceiverHealth relationships = ReceiverMonitor.Inspect(data, 0, TimeSpan.FromHours(1), now, 1000);
            Check(relationships.MissingPayloads == 1, "capture without raw detected");
            Check(relationships.UnreferencedPayloads == 1, "legacy raw without capture reported");
            Check(relationships.FutureCaptures == 1, "future capture detected");
            Check(!relationships.IsHealthy, "missing raw and clock skew are unhealthy");

            File.WriteAllText(Path.Combine(inbox, "captures", "broken.xml"), "broken");
            string journalEntry = Path.Combine(inbox, "journal", Hash(raw) + ".raw");
            File.WriteAllText(journalEntry, "tampered");
            ReceiverHealth corrupt = ReceiverMonitor.Inspect(data, 0, TimeSpan.FromHours(1), now, 1000);
            Check(corrupt.CorruptCaptures == 1, "corrupt capture counted");
            Check(corrupt.CorruptPayloads == 1, "corrupt payload counted");
            Check(!corrupt.IsHealthy, "corruption is unhealthy");

            Check(ReceiverProgram.Main(new[] { "--health", data, "0", "60" }) == 1, "health CLI returns failure for degraded state");
            Check(ReceiverProgram.Main(new[] { "--health", data, "-1" }) == 1, "health CLI rejects invalid threshold");

            Console.WriteLine("Monitoring tests passed: " + count); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}

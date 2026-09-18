using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Visep.Receiver
{
    internal sealed class ReceiverHealth
    {
        internal long AvailableBytes { get; private set; }
        internal bool LowDiskSpace { get; private set; }
        internal int InboxFiles { get; private set; }
        internal int PendingCaptures { get; private set; }
        internal int StalePendingCaptures { get; private set; }
        internal int DeliveredCaptures { get; private set; }
        internal int CapturedCaptures { get; private set; }
        internal int InvalidCaptures { get; private set; }
        internal int CorruptCaptures { get; private set; }
        internal int CorruptPayloads { get; private set; }
        internal int MissingPayloads { get; private set; }
        internal int UnreferencedPayloads { get; private set; }
        internal int FutureCaptures { get; private set; }
        internal bool IsHealthy { get { return !LowDiskSpace && StalePendingCaptures == 0 && CorruptCaptures == 0 && CorruptPayloads == 0 && MissingPayloads == 0 && UnreferencedPayloads == 0 && FutureCaptures == 0; } }

        internal ReceiverHealth(long availableBytes, long minimumFreeBytes)
        {
            AvailableBytes = availableBytes;
            LowDiskSpace = availableBytes < minimumFreeBytes;
        }

        internal void CountInbox() { InboxFiles++; }
        internal void CountPending(bool stale) { PendingCaptures++; if (stale) StalePendingCaptures++; }
        internal void CountDelivered() { DeliveredCaptures++; }
        internal void CountCaptured() { CapturedCaptures++; }
        internal void CountInvalid() { InvalidCaptures++; }
        internal void CountCorruptCapture() { CorruptCaptures++; }
        internal void CountCorruptPayload() { CorruptPayloads++; }
        internal void CountMissingPayload() { MissingPayloads++; }
        internal void CountUnreferencedPayload() { UnreferencedPayloads++; }
        internal void CountFutureCapture() { FutureCaptures++; }
    }

    internal static class ReceiverMonitor
    {
        internal static ReceiverHealth Inspect(string dataFile, long minimumFreeBytes, TimeSpan staleAfter)
        {
            string fullPath = Path.GetFullPath(dataFile);
            string root = Path.GetPathRoot(fullPath);
            long available = new DriveInfo(root).AvailableFreeSpace;
            return Inspect(fullPath, minimumFreeBytes, staleAfter, DateTime.UtcNow, available);
        }

        internal static ReceiverHealth Inspect(string dataFile, long minimumFreeBytes, TimeSpan staleAfter, DateTime nowUtc, long availableBytes)
        {
            if (minimumFreeBytes < 0 || staleAfter <= TimeSpan.Zero || nowUtc.Kind != DateTimeKind.Utc || availableBytes < 0)
                throw new ArgumentOutOfRangeException();
            var health = new ReceiverHealth(availableBytes, minimumFreeBytes);
            string inbox = ReceiverProgram.Inbox(dataFile);
            if (!Directory.Exists(inbox)) return health;

            foreach (string ignored in Directory.EnumerateFiles(inbox, "*.xml")) health.CountInbox();
            var captureHashes = InspectCaptures(Path.Combine(inbox, "captures"), staleAfter, nowUtc, health);
            var payloadHashes = InspectPayloads(inbox, health);
            foreach (string hash in captureHashes) if (!payloadHashes.Contains(hash)) health.CountMissingPayload();
            foreach (string hash in payloadHashes) if (!captureHashes.Contains(hash)) health.CountUnreferencedPayload();
            return health;
        }

        private static HashSet<string> InspectCaptures(string directory, TimeSpan staleAfter, DateTime nowUtc, ReceiverHealth health)
        {
            var hashes = new HashSet<string>(StringComparer.Ordinal);
            if (File.Exists(directory)) { health.CountCorruptCapture(); return hashes; }
            if (!Directory.Exists(directory)) return hashes;
            foreach (string path in Directory.EnumerateFiles(directory, "*.xml"))
            {
                try
                {
                    XElement capture = CaptureJournal.Load(path);
                    hashes.Add((string)capture.Attribute("payloadSha256"));
                    DateTime captured = DateTime.ParseExact((string)capture.Attribute("capturedUtc"), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                    if (captured > nowUtc) health.CountFutureCapture();
                    string state = (string)capture.Attribute("state");
                    if (state == "delivered") health.CountDelivered();
                    else if (state == "captured") health.CountCaptured();
                    else if (state == "invalid") health.CountInvalid();
                    else health.CountPending(captured <= nowUtc && nowUtc - captured >= staleAfter);
                }
                catch (Exception) { health.CountCorruptCapture(); }
            }
            return hashes;
        }

        private static HashSet<string> InspectPayloads(string inbox, ReceiverHealth health)
        {
            var hashes = new HashSet<string>(StringComparer.Ordinal);
            string directory = Path.Combine(inbox, "journal");
            if (File.Exists(directory)) { health.CountCorruptPayload(); return hashes; }
            if (!Directory.Exists(directory)) return hashes;
            var journal = new RawJournal(inbox);
            foreach (string path in journal.Entries())
            {
                try { journal.Read(path); hashes.Add(Path.GetFileNameWithoutExtension(path)); }
                catch (Exception) { health.CountCorruptPayload(); }
            }
            return hashes;
        }
    }
}

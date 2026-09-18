using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Security.Cryptography;
using Visep.Receiver;
class CaptureTests
{
    static int count;
    static void Check(bool ok, string message) { count++; if (!ok) throw new Exception(message); }
    static XElement[] Captures(string inbox) { return Directory.GetFiles(Path.Combine(inbox, "captures"), "*.xml").Select(XElement.Load).ToArray(); }
    static int Main()
    {
        string dir = Path.Combine(Path.GetTempPath(), "visep-captures-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string data = Path.Combine(dir, "data.xml"), inbox = Path.Combine(dir, "inbox");
            Directory.CreateDirectory(inbox);
            string input = Path.Combine(inbox, "source.xml");
            byte[] payload = Encoding.UTF8.GetBytes("<simulation id='one' account='1234' code='130' zone='000' partition='01' />");
            File.WriteAllBytes(input, payload); new InboxReceiver(data).ProcessPending();
            Check(Directory.Exists(Path.Combine(inbox, "captures")), "capture envelope required");
            XElement first = Captures(inbox).Single();
            Check((string)first.Attribute("source") == "source.xml", "source filename retained");
            Check(((string)first.Attribute("capturedUtc")).EndsWith("Z"), "UTC capture timestamp");
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(payload)).Replace("-", "").ToLowerInvariant();
            Check((string)first.Attribute("payloadSha256") == hash, "exact byte hash");
            Check((string)first.Attribute("state") == "delivered", "delivered after store");
            File.WriteAllBytes(input, payload); new InboxReceiver(data).ProcessPending();
            Check(Captures(inbox).Select(x => (string)x.Attribute("id")).Distinct().Count() == 2, "retransmission has distinct capture");
            Check(new RawJournal(inbox).Entries().Count() == 1, "raw deduplication preserved");
            File.WriteAllText(input, "invalid xml"); new InboxReceiver(data).ProcessPending();
            Check(Captures(inbox).Count(x => (string)x.Attribute("state") == "invalid") == 1, "parse failure invalid");
            string failed = Path.Combine(dir, "failed"), failedInbox = Path.Combine(failed, "inbox"), failedData = Path.Combine(failed, "data.xml");
            Directory.CreateDirectory(failedInbox);
            string pending = Path.Combine(failedInbox, "pending.xml");
            File.WriteAllBytes(pending, payload); File.WriteAllText(failedData, "invalid database");
            new InboxReceiver(failedData).ProcessPending();
            Check((string)Captures(failedInbox).Single().Attribute("state") == "pending", "store failure stays pending");
            Check(File.Exists(pending), "store failure retains input");
            File.Delete(failedData); File.Delete(pending);
            Check(new InboxReceiver(failedData).Replay() == 0, "replay recovers pending capture");
            Check((string)Captures(failedInbox).Single().Attribute("state") == "delivered", "replay updates capture after persistence");
            string blocked = Path.Combine(dir, "blocked"), blockedInbox = Path.Combine(blocked, "inbox"), blockedData = Path.Combine(blocked, "data.xml");
            Directory.CreateDirectory(blockedInbox); File.WriteAllText(Path.Combine(blockedInbox, "captures"), "blocked");
            File.WriteAllBytes(Path.Combine(blockedInbox, "input.xml"), payload);
            new InboxReceiver(blockedData).ProcessPending();
            Check(!File.Exists(blockedData), "envelope failure prevents store write");
            Check(File.Exists(Path.Combine(blockedInbox, "input.xml")), "envelope failure preserves input");
            string damaged = Path.Combine(dir, "damaged"), damagedInbox = Path.Combine(damaged, "inbox"), damagedData = Path.Combine(damaged, "data.xml");
            Directory.CreateDirectory(Path.Combine(damagedInbox, "captures"));
            string corrupt = Path.Combine(damagedInbox, "captures", "00000000000000000000000000000000.xml");
            File.WriteAllText(corrupt, "broken metadata");
            string validCapture = new CaptureJournal(damagedInbox).Append("recovered.xml", payload);
            new RawJournal(damagedInbox).Append(payload);
            Check(new InboxReceiver(damagedData).Replay() > 0, "corrupt metadata reports replay failure");
            Check(XElement.Load(damagedData).Element("Incidents").Elements().Count() == 1, "corrupt metadata does not block raw persistence");
            Check((string)XElement.Load(validCapture).Attribute("state") == "delivered", "corrupt metadata does not block valid capture delivery");
            File.Delete(corrupt);
            foreach (string field in new[] { "root", "id", "payloadSha256", "state", "capturedUtc", "source" })
            {
                var invalid = new XElement(XElement.Load(validCapture));
                invalid.SetAttributeValue("id", Path.GetFileNameWithoutExtension(corrupt));
                if (field == "root") invalid.Name = "unexpected";
                else invalid.SetAttributeValue(field, field == "source" ? "../outside.xml" : "broken");
                invalid.Save(corrupt);
                Check(new InboxReceiver(damagedData).Replay() > 0, "invalid metadata schema reported: " + field);
            }
            File.Delete(corrupt);
            new CaptureJournal(damagedInbox).SetState(validCapture, "pending");
            File.Delete(damagedData);
            using (var held = new FileStream(validCapture, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Check(new InboxReceiver(damagedData).Replay() > 0, "state replacement failure reported after store");
                Check(XElement.Load(damagedData).Element("Incidents").Elements().Count() == 1, "store persisted before state replacement failure");
                Check((string)XElement.Load(validCapture).Attribute("state") == "pending", "failed state replacement remains pending");
            }
            Check(new InboxReceiver(damagedData).Replay() == 0, "state replacement retry recovers");
            Check((string)XElement.Load(validCapture).Attribute("state") == "delivered", "state replacement retry delivers capture");
            Check(XElement.Load(damagedData).Element("Incidents").Elements().Count() == 1, "state replacement retry does not duplicate incident");
            Console.WriteLine("Capture tests passed: " + count); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}


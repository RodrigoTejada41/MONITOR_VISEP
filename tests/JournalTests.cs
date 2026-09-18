using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Visep.Receiver;
class JournalTests
{
    static int count;
    static void Check(bool ok, string text) { count++; if (!ok) throw new Exception(text); }
    static int Main()
    {
        string dir = Path.Combine(Path.GetTempPath(), "visep-journal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string data = Path.Combine(dir, "data.xml");
            Check(new InboxReceiver(data).Replay() == 1, "missing journal does not report successful recovery");
            string inbox = Path.Combine(dir, "inbox"); Directory.CreateDirectory(inbox);
            byte[] raw = Encoding.UTF8.GetBytes("<simulation id='one' account='1234' code='130' zone='000' partition='01' />\r\n");
            string input = Path.Combine(inbox, "one.xml"); File.WriteAllBytes(input, raw);
            new InboxReceiver(data).ProcessPending();
            var journal = new RawJournal(inbox);
            string entry = journal.Entries().Single();
            Check(journal.Read(entry).SequenceEqual(raw), "exact original bytes");
            Check(new InboxReceiver(data).Replay() == 0, "replay succeeds after restart");
            Check(XElement.Load(data).Element("Incidents").Elements().Count() == 1, "idempotent replay");
            File.WriteAllBytes(input, raw); new InboxReceiver(data).ProcessPending();
            Check(journal.Entries().Count() == 1, "retry no duplicate journal");
            File.WriteAllText(input, "<simulation id=' one ' account='1234' code='131' zone='000' partition='01' />");
            new InboxReceiver(data).ProcessPending();
            Check(journal.Entries().Count() == 2, "same id different payload preserved");
            string conflictData = Path.Combine(dir, "fresh.xml");
            Check(new InboxReceiver(conflictData).Replay() == 2, "conflicting ids refused in empty store");
            Check(!File.Exists(conflictData), "conflicts cannot pick arbitrary winner");
            File.WriteAllText(input, "invalid xml"); new InboxReceiver(data).ProcessPending();
            Check(journal.Entries().Count() == 3, "invalid payload journaled");
            Check(Directory.GetFiles(Path.Combine(inbox, "quarantine"), "*.xml").Length == 1, "invalid quarantined");
            File.WriteAllText(entry, "tampered");
            Check(new InboxReceiver(data).Replay() > 0, "corruption and invalid replay fail");
            string failedDir = Path.Combine(dir, "failed"); Directory.CreateDirectory(failedDir);
            string failedInbox = Path.Combine(failedDir, "inbox"); Directory.CreateDirectory(failedInbox);
            File.WriteAllText(Path.Combine(failedInbox, "journal"), "blocked");
            File.WriteAllBytes(Path.Combine(failedInbox, "pending.xml"), raw);
            new InboxReceiver(Path.Combine(failedDir, "data.xml")).ProcessPending();
            Check(File.Exists(Path.Combine(failedInbox, "pending.xml")), "journal write failure keeps input");
            Check(!File.Exists(Path.Combine(failedDir, "data.xml")), "journal failure prevents store write");
            string recoveredDir = Path.Combine(dir, "recovered"); Directory.CreateDirectory(recoveredDir);
            string recoveredData = Path.Combine(recoveredDir, "data.xml");
            string recoveredInbox = Path.Combine(recoveredDir, "inbox"); Directory.CreateDirectory(recoveredInbox);
            File.WriteAllBytes(Path.Combine(recoveredInbox, "pending.xml"), raw);
            File.WriteAllText(recoveredData, "invalid database");
            new InboxReceiver(recoveredData).ProcessPending();
            Check(File.Exists(Path.Combine(recoveredInbox, "pending.xml")), "store failure keeps input pending");
            Check(new RawJournal(recoveredInbox).Entries().Count() == 1, "store failure retains journal");
            Check(new InboxReceiver(recoveredData).Replay() == 1, "replay store failure reported");
            File.Delete(recoveredData);
            File.Delete(Path.Combine(recoveredInbox, "pending.xml"));
            Check(new InboxReceiver(recoveredData).Replay() == 0, "journal-only recovery after failure");
            Check(XElement.Load(recoveredData).Element("Incidents").Elements().Count() == 1, "journal-only recreated incident");
            using (var held = new FileStream(Path.Combine(recoveredInbox, "receiver.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                bool denied = false;
                try { new InboxReceiver(recoveredData).Replay(); } catch(IOException) { denied = true; }
                Check(denied, "replay excludes concurrent receiver");
            }
            File.WriteAllBytes(Path.Combine(recoveredInbox, "large.xml"), new byte[RawJournal.MaximumBytes + 1]);
            new InboxReceiver(recoveredData).ProcessPending();
            Check(File.Exists(Path.Combine(recoveredInbox, "large.xml")), "oversized input preserved");
            File.WriteAllText(Path.Combine(recoveredInbox, "control.xml"), "<simulation id='bad&#10;id' account='1234' code='130' zone='000' partition='01' />");
            new InboxReceiver(recoveredData).ProcessPending();
            Check(File.Exists(Path.Combine(recoveredInbox, "quarantine", "control.xml")), "control field quarantined before store");
            string missingData = Path.Combine(dir, "missing", "data.xml");
            Check(new InboxReceiver(missingData).Replay() == 1, "replay requires existing journal");
            Check(!Directory.Exists(Path.Combine(dir, "missing", "inbox", "journal")), "replay does not create empty journal");
            Console.WriteLine("Journal tests passed: " + count); return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}

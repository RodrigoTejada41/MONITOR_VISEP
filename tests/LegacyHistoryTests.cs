using System;
using System.IO;
using System.Xml.Linq;
using Visep;

class LegacyHistoryTests
{
    static int assertions;
    static void Assert(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }
    static void Denied(Action action)
    {
        bool denied = false;
        try { action(); } catch (UnauthorizedAccessException) { denied = true; }
        Assert(denied, "History requires this store's authenticated session.");
    }
    static int Main()
    {
        string dir = Path.Combine(Path.GetTempPath(), "visep-history-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string path = Path.Combine(dir, "data.xml");
            Store store = new Store(path);
            string password = Guid.NewGuid().ToString("N");
            store.Bootstrap("admin", password);
            Session admin = store.Login("admin", password);
            Assert(store.LegacyHistory(admin).Count == 0, "Missing optional history is empty.");
            Denied(() => store.LegacyHistory(null));
            Denied(() => new Store(path).LegacyHistory(admin));
            XElement root = XElement.Load(path);
            root.Add(new XElement("LegacyHistory", new XElement("Event",
                new XAttribute("Id", "legacy-1"), new XAttribute("SourceClientId", "42"),
                new XAttribute("Account", "0001"), new XAttribute("ClientName", "Synthetic client"),
                new XAttribute("Code", "130"), new XAttribute("Zone", "02"),
                new XAttribute("OccurredLocal", "2014-10-19 00:15:00"), new XAttribute("Detail", "Imported test"))));
            root.Save(path);
            foreach (string role in new[] { "Admin", "Operator", "Viewer" })
            {
                Session session = admin;
                if (role != "Admin") { store.AddUser(admin, role, password, role); session = store.Login(role, password); }
                string before = File.ReadAllText(path);
                var events = store.LegacyHistory(session);
                Assert(events.Count == 1, role + " can read history.");
                Assert(events[0].Id == "legacy-1" && events[0].SourceClientId == "42", "Source identity preserved.");
                Assert(events[0].Account == "0001" && events[0].ClientName == "Synthetic client", "Client mapped.");
                Assert(events[0].Code == "130" && events[0].Zone == "02" && events[0].Detail == "Imported test", "Event mapped.");
                Assert(events[0].OccurredLocal == "2014-10-19 00:15:00", "Original local timestamp preserved verbatim.");
                Assert(File.ReadAllText(path) == before, "Read must not modify database.");
                Assert(store.Incidents(session).Count == 0, "Historical events never become incidents.");
                events.Clear();
                Assert(store.LegacyHistory(session).Count == 1, "Result collection is detached.");
            }
            Console.WriteLine("PASS: " + assertions + " legacy history assertions");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { Directory.Delete(dir, true); }
    }
}

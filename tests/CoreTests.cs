using System;
using System.IO;
using System.Threading;
using System.Linq;
using System.Xml.Linq;
using Visep;
class CoreTests
{
    static int assertions;
    static void Assert(bool ok,string message)
    {
        assertions++;
        if(!ok)throw new Exception(message);
    }
    static void Denied(Action a)
    {
        bool denied=false;
        try
        {
            a();
        }
        catch(UnauthorizedAccessException)
        {
            denied=true;
        }
        Assert(denied,"permission denied");
    }
    static void SecurityTests(string dir)
    {
        string path=Path.Combine(dir,"security.xml");
        var store=new Store(path);
        store.Bootstrap("admin","StrongPass123!");
        var session=store.Login("admin","StrongPass123!");
        Denied(()=>store.Login("admin","wrong"));
        Assert(store.Audit(session).Any(a=>a.Action=="LoginSuccess"),"login success audited");
        Assert(store.Audit(session).Any(a=>a.Action=="LoginFailure"),"login failure audited");
        for(int n=0;n<4;n++)Denied(()=>store.Login("admin","wrong"));
        var restarted=new Store(path);
        Denied(()=>restarted.Login("admin","StrongPass123!"));
        var xml=XElement.Load(path);
        Assert((string)xml.Attribute("SchemaVersion")=="1","versioned schema");
        xml.Element("Users").Element("User").SetAttributeValue("LockedUntilUtc",DateTime.UtcNow.AddMinutes(-1).ToString("o"));
        xml.Save(path);
        Assert(restarted.Login("admin","StrongPass123!")!=null,"lock expires");
        xml=XElement.Load(path);
        xml.SetAttributeValue("SchemaVersion","99");
        xml.Save(path);
        bool unknown=false;
        try
        {
            var unused=restarted.HasUsers;
        }
        catch(InvalidDataException)
        {
            unknown=true;
        }
        Assert(unknown,"reject unknown schema");
        xml.SetAttributeValue("SchemaVersion","1");
        xml.Add(new XElement("Users"));
        xml.Save(path);
        bool duplicate=false;
        try
        {
            var unused=restarted.HasUsers;
        }
        catch(InvalidDataException)
        {
            duplicate=true;
        }
        Assert(duplicate,"reject duplicate data section");
        File.WriteAllText(path,"<!DOCTYPE Visep [<!ENTITY probe 'expanded'>]><Visep SchemaVersion='1'><Users>&probe;</Users><Clients/><Incidents/><Audit/></Visep>");
        bool dtd=false;
        try
        {
            var unused=restarted.HasUsers;
        }
        catch(System.Xml.XmlException)
        {
            dtd=true;
        }
        Assert(dtd,"reject DTD");
    }
    static void ClaimRaceTests(string dir)
    {
        string path=Path.Combine(dir,"race.xml");
        var adminStore=new Store(path);
        adminStore.Bootstrap("admin","StrongPass123!");
        var admin=adminStore.Login("admin","StrongPass123!");
        adminStore.AddUser(admin,"first","StrongPass123!","Operator");
        adminStore.AddUser(admin,"second","StrongPass123!","Operator");
        var stores=new[]{new Store(path),new Store(path)};
        var sessions=new[]{stores[0].Login("first","StrongPass123!"),stores[1].Login("second","StrongPass123!")};
        var incident=adminStore.ReceiveSimulation("race","TEST","130","1","1",null);
        var successes=new bool[2];
        var errors=new Exception[2];
        using(var ready=new CountdownEvent(2))
        using(var start=new ManualResetEvent(false))
        {
            var threads=new Thread[2];
            for(int n=0;n<2;n++)
            {
                int index=n;
                threads[n]=new Thread(()=>
                {
                    ready.Signal();start.WaitOne();
                    try { stores[index].Claim(sessions[index],incident.Id);successes[index]=true; }
                    catch(Exception e) { errors[index]=e; }
                });
                threads[n].Start();
            }
            ready.Wait();start.Set();
            foreach(var thread in threads)thread.Join();
        }
        Assert(successes.Count(x=>x)==1,"exactly one concurrent claim succeeds");
        Assert(errors.Count(x=>x is InvalidOperationException)==1,"losing claim explicitly rejected");
        int winner=successes[0]?0:1;
        var persisted=adminStore.Incidents(admin).Single();
        Assert(persisted.Owner==sessions[winner].User&&persisted.Status=="InProgress","winning owner persisted");
        Assert(adminStore.Audit(admin).Count(x=>x.Action=="Claim")==1,"only successful claim audited");
    }
    static void PersistenceFailureTests(string dir)
    {
        string path=Path.Combine(dir,"persistence.xml");
        var store=new Store(path);
        store.ReceiveSimulation("before","TEST","130","1","1",null);
        store.ReceiveSimulation("second","TEST","130","1","1",null);
        byte[] before=File.ReadAllBytes(path);
        bool failed=false;
        using(var blocked=new FileStream(path+".bak",FileMode.Open,FileAccess.Read,FileShare.None))
        {
            try { store.ReceiveSimulation("blocked","TEST","130","1","1",null); }
            catch(IOException) { failed=true; }
            Assert(failed,"persistence failure does not report success");
            Assert(before.SequenceEqual(File.ReadAllBytes(path)),"failed write preserves previous state exactly");
        }
        Assert(Directory.GetFiles(dir,"persistence.xml.*.tmp").Length==0,"failed write cleans temporary file");
        store.ReceiveSimulation("blocked","TEST","130","1","1",null);
        var xml=XElement.Load(path);
        Assert(xml.Element("Incidents").Elements().Count()==3,"retry persists once after storage recovers");
    }
    static void CsvTests(string dir)
    {
        string path=Path.Combine(dir,"csv.xml");
        var store=new Store(path);
        store.Bootstrap("admin","StrongPass123!");
        var admin=store.Login("admin","StrongPass123!");
        foreach(string prefix in new[]{"=","+","-","@"})
        {
            string account="account"+(int)prefix[0];
            store.AddClient(admin,prefix+"SUM(1,2)",account,"","","","");
            store.ReceiveSimulation(account,account,"130","1","1",null);
        }
        store.AddClient(admin,"Quoted \"client\", test","quoted","","","","");
        var incident=store.ReceiveSimulation("quoted","quoted","130","1","1",null);
        store.Claim(admin,incident.Id);
        store.AddAction(admin,incident.Id,"Line \"one\", checked\r\nLine two");
        string destination=Path.Combine(dir,"escaping.csv");
        store.ExportCsv(admin,destination);
        string csv=File.ReadAllText(destination);
        foreach(string prefix in new[]{"=","+","-","@"})
            Assert(csv.Contains("\"'"+prefix+"SUM(1,2)\""),"CSV formula prefix escaped: "+prefix);
        Assert(csv.Contains("\"Quoted \"\"client\"\", test\""),"CSV quote and comma escaping");
        Assert(csv.Replace("\r\n","\n").Contains("Line \"\"one\"\", checked\nLine two\""),"CSV multiline action and quote escaping");
    }
    static int Main()
    {
        string dir=Path.Combine(Path.GetTempPath(),"visep-test-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string path=Path.Combine(dir,"state.xml");
            var s=new Store(path);
            Assert(!s.HasUsers,"fresh");
            s.Bootstrap("admin","StrongPass123!");
            var admin=s.Login("admin","StrongPass123!");
            Denied(()=>s.Login("admin","wrong"));
            s.AddUser(admin,"operator","StrongPass123!","Operator");
            s.AddUser(admin,"viewer","StrongPass123!","Viewer");
            var op=s.Login("operator","StrongPass123!");
            var view=s.Login("viewer","StrongPass123!");
            Denied(()=>s.AddClient(view,"A","0001","","","",""));
            s.AddClient(admin,"A","0001","Street","555","Panel","1");
            var i=s.ReceiveSimulation("m1","0001","130","1","1",null);
            Assert(i.IsSimulation&&i.ClientName=="A","simulation metadata");
            Assert(s.ReceiveSimulation("m1","0001","130","1","1",null).Id==i.Id,"dedup");
            Denied(()=>s.Claim(view,i.Id));
            Denied(()=>s.AddUser(op,"blocked","StrongPass123!","Admin"));
            Denied(()=>s.Audit(view));
            Denied(()=>s.ExportCsv(view,Path.Combine(dir,"blocked.csv")));
            s.Claim(op,i.Id);
            Denied(()=>s.AddAction(view,i.Id,"Forged action"));
            s.AddAction(op,i.Id,"Contacted client");
            s.Close(op,i.Id,"Confirmed test");
            Assert(s.Incidents(view)[0].Status=="Closed","closed");
            bool invalid=false;
            try
            {
                s.Close(op,i.Id,"Again");
            }
            catch(InvalidOperationException)
            {
                invalid=true;
            }
            Assert(invalid,"closed immutable");
            var restart=new Store(path);
            Assert(restart.HasUsers,"restart");
            Denied(()=>restart.Clients(admin));
            var radmin=restart.Login("admin","StrongPass123!");
            Assert(restart.Incidents(radmin).Count==1,"persisted");
            Exception failure=null;
            Thread[] threads=new Thread[4];
            for(int n=0;n<4;n++)
            {
                int index=n;
                threads[n]=new Thread(()=>
                {
                    try
                    {
                        var local=new Store(path);local.ReceiveSimulation("parallel"+index,"0001","130","1","1",null);
                    }
                    catch(Exception e)
                    {
                        failure=e;
                    }
                }
                );
                threads[n].Start();
            }
            foreach(var t in threads)t.Join();
            Assert(failure==null,"parallel errors");
            Assert(restart.Incidents(radmin).Count==5,"parallel persistence");
            restart.ExportCsv(radmin,Path.Combine(dir,"export.csv"));
            Assert(File.Exists(Path.Combine(dir,"export.csv")),"export");
            Assert(restart.Audit(radmin).Count>=10,"audit");
            SecurityTests(dir);
            ClaimRaceTests(dir);
            PersistenceFailureTests(dir);
            CsvTests(dir);
            Console.WriteLine("PASS "+assertions+" assertions");
            return 0;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine(e);
            return 1;
        }
        finally
        {
            Directory.Delete(dir,true);
        }
    }
}

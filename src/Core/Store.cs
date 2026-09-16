using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Linq;
namespace Visep
{
    public sealed class Store
    {
        readonly string path;
        readonly XmlRepository repository;
        readonly HashSet<Session> sessions=new HashSet<Session>();
        public Store(string path)
        {
            this.path=Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(this.path));
            repository=new XmlRepository(this.path);
        }
        T Access<T>(bool write,Func<XElement,T> action)
        {
            return repository.Access(write,action);
        }
        static string A(XElement x,string key)
        {
            return (string)x.Attribute(key)??"";
        }
        static string Required(string value,string name,int max)
        {
            value=(value??"").Trim();
            if(value.Length==0||value.Length>max||value.Any(char.IsControl))throw new ArgumentException("Invalid "+name);
            return value;
        }
        static string OperationText(string value,string name)
        {
            value=(value??"").Trim();
            if(value.Length==0||value.Length>4000||value.Any(c=>char.IsControl(c)&&c!='\r'&&c!='\n'&&c!='\t'))throw new ArgumentException("Invalid "+name);
            return value;
        }
        static string Optional(string value,int max)
        {
            value=value??"";
            if(value.Length>max)throw new ArgumentException("Text too long.");
            return value;
        }
        void Auth(Session session,params string[] roles)
        {
            lock(sessions)
            {
                if(session==null||!sessions.Contains(session)|| (roles.Length>0&&!roles.Contains(session.Role)))throw new UnauthorizedAccessException("Access denied.");
            }
        }
        static void Log(XElement root,string user,string action,string target)
        {
            root.Element("Audit").Add(new XElement("Entry",new XAttribute("AtUtc",DateTime.UtcNow.ToString("o")),new XAttribute("User",user),new XAttribute("Action",action),new XAttribute("Target",target)));
        }
        static XElement UserElement(string user,string password,string role)
        {
            user=Required(user,"user",64);
            if(!new[]
            {
                "Admin","Operator","Viewer"
            }
            .Contains(role))throw new ArgumentException("Invalid role.");
            return PasswordSecurity.CreateUser(user,password,role);
        }
        public bool HasUsers
        {
            get
            {
                return Access(false,r=>r.Element("Users").Elements().Any());
            }
        }
        public void Bootstrap(string user,string password)
        {
            Access(true,r=>
            {
                if(r.Element("Users").Elements().Any())throw new InvalidOperationException("Already initialized.");var u=UserElement(user,password,"Admin");r.Element("Users").Add(u);Log(r,A(u,"Name"),"Bootstrap",A(u,"Name"));return 0;
            }
            );
        }
        public Session Login(string user,string password)
        {
            user=Required(user,"user",64);
            if(password==null||password.Length>256) throw new UnauthorizedAccessException("Invalid credentials.");
            var session=Access(true,r=>Authenticate(r,user,password));
            if(session==null) throw new UnauthorizedAccessException("Invalid credentials or account temporarily locked.");
            lock(sessions) sessions.Add(session);
            return session;
        }
        Session Authenticate(XElement root,string user,string password)
        {
            var entry=root.Element("Users").Elements().FirstOrDefault(x=>string.Equals(A(x,"Name"),user,StringComparison.OrdinalIgnoreCase));
            DateTime lockedUntil;
            bool locked=entry!=null&&DateTime.TryParse(A(entry,"LockedUntilUtc"),null,DateTimeStyles.RoundtripKind,out lockedUntil)&&lockedUntil>DateTime.UtcNow;
            bool valid=PasswordSecurity.Verify(entry,password);
            if(locked||!valid)
            {
                if(entry!=null&&!locked) RecordFailure(entry);
                Log(root,user,locked?"LoginBlocked":"LoginFailure",user);
                return null;
            }
            entry.SetAttributeValue("FailedAttempts",0);
            entry.SetAttributeValue("LockedUntilUtc","");
            Log(root,A(entry,"Name"),"LoginSuccess",A(entry,"Name"));
            return new Session(A(entry,"Name"),A(entry,"Role"));
        }
        static void RecordFailure(XElement user)
        {
            int attempts;
            int.TryParse(A(user,"FailedAttempts"),out attempts);
            attempts++;
            user.SetAttributeValue("FailedAttempts",attempts>=5?0:attempts);
            if(attempts>=5) user.SetAttributeValue("LockedUntilUtc",DateTime.UtcNow.AddMinutes(5).ToString("o"));
        }
        public void AddUser(Session session,string user,string password,string role)
        {
            Auth(session,"Admin");
            Access(true,r=>
            {
                var u=UserElement(user,password,role);if(r.Element("Users").Elements().Any(x=>string.Equals(A(x,"Name"),A(u,"Name"),StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("User exists.");r.Element("Users").Add(u);Log(r,session.User,"AddUser",A(u,"Name"));return 0;
            }
            );
        }
        static Client ReadClient(XElement c)
        {
            return new Client
            {
                Id=A(c,"Id"),Name=A(c,"Name"),Account=A(c,"Account"),Address=A(c,"Address"),Contacts=A(c,"Contacts"),Equipment=A(c,"Equipment"),Zones=A(c,"Zones")
            }
            ;
        }
        public List<Client> Clients(Session session)
        {
            Auth(session);
            return Access(false,r=>r.Element("Clients").Elements().Select(ReadClient).ToList());
        }
        public Client AddClient(Session session,string name,string account,string address,string contacts,string equipment,string zones)
        {
            Auth(session,"Admin");
            name=Required(name,"name",200);
            account=Required(account,"account",64);
            return Access(true,r=>
            {
                if(r.Element("Clients").Elements().Any(x=>A(x,"Account")==account))throw new ArgumentException("Account exists.");var c=new XElement("Client",new XAttribute("Id",Guid.NewGuid().ToString("N")),new XAttribute("Name",name),new XAttribute("Account",account),new XAttribute("Address",Optional(address,1000)),new XAttribute("Contacts",Optional(contacts,4000)),new XAttribute("Equipment",Optional(equipment,4000)),new XAttribute("Zones",Optional(zones,4000)));r.Element("Clients").Add(c);Log(r,session.User,"AddClient",A(c,"Id"));return ReadClient(c);
            }
            );
        }
        static Incident ReadIncident(XElement i)
        {
            return new Incident
            {
                Id=A(i,"Id"),Account=A(i,"Account"),ClientName=A(i,"ClientName"),Code=A(i,"Code"),Zone=A(i,"Zone"),Partition=A(i,"Partition"),ReceivedUtc=DateTime.Parse(A(i,"ReceivedUtc"),null,DateTimeStyles.RoundtripKind),OriginUtc=A(i,"OriginUtc")==""?(DateTime?)null:DateTime.Parse(A(i,"OriginUtc"),null,DateTimeStyles.RoundtripKind),Status=A(i,"Status"),Owner=A(i,"Owner"),Actions=string.Join(Environment.NewLine,i.Elements("Action").Select(a=>a.Value)),Raw=A(i,"Raw"),IsSimulation=true
            }
            ;
        }
        public List<Incident> Incidents(Session session)
        {
            Auth(session);
            return Access(false,r=>r.Element("Incidents").Elements().Select(ReadIncident).OrderByDescending(i=>i.ReceivedUtc).ToList());
        }
        public List<LegacyEvent> LegacyHistory(Session session)
        {
            Auth(session);
            return Access(false,root=>
            {
                var history=root.Element("LegacyHistory");
                if(history==null)return new List<LegacyEvent>();
                return history.Elements("Event").Select(e=>new LegacyEvent
                {
                    Id=A(e,"Id"),SourceClientId=A(e,"SourceClientId"),Account=A(e,"Account"),
                    ClientName=A(e,"ClientName"),Code=A(e,"Code"),Zone=A(e,"Zone"),
                    OccurredLocal=A(e,"OccurredLocal"),Detail=A(e,"Detail")
                }).ToList();
            });
        }
        public Incident ReceiveSimulation(string messageId,string account,string code,string zone,string partition,DateTime? origin)
        {
            messageId=Required(messageId,"message ID",128);
            account=Required(account,"account",64);
            code=Required(code,"code",32);
            zone=Required(zone,"zone",32);
            partition=Required(partition,"partition",32);
            return Access(true,r=>
            {
                var existing=r.Element("Incidents").Elements().FirstOrDefault(x=>A(x,"MessageId")==messageId);if(existing!=null)return ReadIncident(existing);var client=r.Element("Clients").Elements().FirstOrDefault(x=>A(x,"Account")==account);var i=new XElement("Incident",new XAttribute("Id",Guid.NewGuid().ToString("N")),new XAttribute("MessageId",messageId),new XAttribute("Account",account),new XAttribute("ClientName",client==null?"Unknown account":A(client,"Name")),new XAttribute("Code",code),new XAttribute("Zone",zone),new XAttribute("Partition",partition),new XAttribute("ReceivedUtc",DateTime.UtcNow.ToString("o")),new XAttribute("OriginUtc",origin.HasValue?origin.Value.ToUniversalTime().ToString("o"):""),new XAttribute("Status","New"),new XAttribute("Owner",""),new XAttribute("Raw","SIMULATION|"+account+"|"+code+"|"+zone+"|"+partition));r.Element("Incidents").Add(i);Log(r,"Simulator","ReceiveSimulation",A(i,"Id"));return ReadIncident(i);
            }
            );
        }
        void Change(Session s,string id,string action,Action<XElement> change)
        {
            Auth(s,"Admin","Operator");
            Access(true,r=>
            {
                var i=r.Element("Incidents").Elements().FirstOrDefault(x=>A(x,"Id")==id);if(i==null)throw new ArgumentException("Incident not found.");if(A(i,"Status")=="Closed")throw new InvalidOperationException("Incident closed.");change(i);Log(r,s.User,action,id);return 0;
            }
            );
        }
        public void Claim(Session s,string id)
        {
            Change(s,id,"Claim",i=>
            {
                if(A(i,"Owner")!="")throw new InvalidOperationException("Incident already claimed.");i.SetAttributeValue("Owner",s.User);i.SetAttributeValue("Status","InProgress");
            }
            );
        }
        void RequireOwner(Session s,XElement i)
        {
            if(A(i,"Owner")!=s.User&&s.Role!="Admin")throw new UnauthorizedAccessException("Only assigned operator can modify incident.");
            if(A(i,"Status")!="InProgress")throw new InvalidOperationException("Claim incident first.");
        }
        public void AddAction(Session s,string id,string text)
        {
            text=OperationText(text,"action");
            Change(s,id,"AddAction",i=>
            {
                RequireOwner(s,i);i.Add(new XElement("Action",DateTime.UtcNow.ToString("o")+" "+s.User+": "+text));
            }
            );
        }
        public void Close(Session s,string id,string reason)
        {
            reason=OperationText(reason,"reason");
            Change(s,id,"Close",i=>
            {
                RequireOwner(s,i);i.Add(new XElement("Action",DateTime.UtcNow.ToString("o")+" "+s.User+" CLOSED: "+reason));i.SetAttributeValue("Status","Closed");
            }
            );
        }
        public List<AuditEntry> Audit(Session s)
        {
            Auth(s,"Admin");
            return Access(false,r=>r.Element("Audit").Elements().Select(a=>new AuditEntry
            {
                AtUtc=DateTime.Parse(A(a,"AtUtc"),null,DateTimeStyles.RoundtripKind),User=A(a,"User"),Action=A(a,"Action"),Target=A(a,"Target")
            }
            ).ToList());
        }
        static string Csv(string value)
        {
            value=value??"";
            if(value.Length>0&&"=+-@\t\r".IndexOf(value[0])>=0)value="'"+value;
            return "\""+value.Replace("\"","\"\"")+"\"";
        }
        public void ExportCsv(Session s,string destination)
        {
            Auth(s,"Admin");
            string target=Path.GetFullPath(destination);
            if(string.Equals(target,path,StringComparison.OrdinalIgnoreCase)||string.Equals(target,path+".bak",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Export cannot overwrite data.");
            var lines=new List<string>
            {
                "Id,Account,Client,Code,Zone,Partition,ReceivedUtc,Status,Owner,Simulation,Actions"
            }
            ;
            foreach(var i in Incidents(s))lines.Add(string.Join(",",new[]
            {
                i.Id,i.Account,i.ClientName,i.Code,i.Zone,i.Partition,i.ReceivedUtc.ToString("o"),i.Status,i.Owner,"true",i.Actions
            }
            .Select(Csv)));
            File.WriteAllLines(target,lines,new UTF8Encoding(true));
            Access(true,r=>
            {
                Log(r,s.User,"ExportCsv",Path.GetFileName(target));return 0;
            }
            );
        }
    }
}

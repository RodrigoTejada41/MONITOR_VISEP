using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;

namespace Visep.Receiver
{
    public static class ReceiverProgram
    {
        public static int Main(string[] args)
        {
            try
            {
                if (args.Length > 0 && args[0] == "--simulate")
                {
                    if (args.Length != 4) throw new ArgumentException("Uso: --simulate datafile account code");
                    string inbox = Inbox(args[1]); Directory.CreateDirectory(inbox);
                    string id = Guid.NewGuid().ToString("N");
                    string temporary = Path.Combine(inbox, id + ".tmp");
                    new XDocument(new XElement("simulation", new XAttribute("id", id),
                        new XAttribute("account", args[2]), new XAttribute("code", args[3]),
                        new XAttribute("zone", "000"), new XAttribute("partition", "01"))).Save(temporary);
                    File.Move(temporary, Path.Combine(inbox, id + ".xml"));
                    Console.WriteLine("Mensagem simulada criada: " + id); return 0;
                }
                bool console = args.Length > 0 && args[0] == "--console";
                string path = args.Length > (console ? 1 : 0) ? args[console ? 1 : 0] :
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "visep.xml");
                if (console)
                {
                    using (var stop = new ManualResetEvent(false))
                    {
                        Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e) { e.Cancel = true; stop.Set(); };
                        Console.WriteLine("Receptor de simulacao iniciado. Ctrl+C encerra.");
                        new InboxReceiver(path).Run(stop);
                    }
                }
                else ServiceBase.Run(new ReceiverService(path));
                return 0;
            }
            catch (Exception) { Console.Error.WriteLine("Falha ao iniciar receptor. Verifique parametros, acesso e configuracao."); return 1; }
        }
        internal static string Inbox(string path) { return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)), "inbox"); }
    }

    internal sealed class ReceiverService : ServiceBase
    {
        private readonly string dataFile;
        private readonly ManualResetEvent stop = new ManualResetEvent(false);
        private Thread worker;
        internal ReceiverService(string path) { ServiceName = "VisepReceiver"; dataFile = path; CanStop = true; }
        protected override void OnStart(string[] args)
        {
            stop.Reset();
            var receiver = new InboxReceiver(args.Length > 0 ? args[0] : dataFile);
            worker = new Thread(delegate() { receiver.Run(stop); }); worker.IsBackground = true; worker.Start();
        }
        protected override void OnStop()
        {
            stop.Set();
            if (worker != null && !worker.Join(15000)) { RequestAdditionalTime(30000); worker.Join(30000); }
        }
        protected override void Dispose(bool disposing) { if (disposing && (worker == null || !worker.IsAlive)) stop.Dispose(); base.Dispose(disposing); }
    }

    internal sealed class InboxReceiver
    {
        private readonly string dataFile;
        private readonly string inbox;
        internal InboxReceiver(string path) { dataFile = Path.GetFullPath(path); inbox = ReceiverProgram.Inbox(path); }
        internal void Run(WaitHandle stop)
        {
            while (!stop.WaitOne(0))
            {
                try
                {
                    Directory.CreateDirectory(inbox);
                    foreach (string path in Directory.GetFiles(inbox, "*.xml"))
                    {
                        if (stop.WaitOne(0)) break;
                        Process(path);
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                if (stop.WaitOne(1000)) break;
            }
        }
        private void Process(string path)
        {
            bool persisting = false;
            try
            {
                XDocument document;
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 65536 };
                using (var reader = XmlReader.Create(path, settings)) document = XDocument.Load(reader);
                XElement root = document.Root;
                if (root == null || root.Name != "simulation" || root.HasElements) throw new FormatException();
                string id = Required(root, "id"), account = Required(root, "account"), code = Required(root, "code");
                string zone = Optional(root, "zone"), partition = Optional(root, "partition"), originText = Optional(root, "originUtc");
                DateTime? origin = null;
                if (originText.Length > 0)
                {
                    DateTime parsed;
                    if (!originText.EndsWith("Z", StringComparison.Ordinal) || !DateTime.TryParse(originText, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed)) throw new FormatException();
                    origin = parsed;
                }
                persisting = true;
                new Visep.Store(dataFile).ReceiveSimulation(id, account, code, zone, partition, origin);
                Move(path, "processed");
            }
            catch (XmlException) { if (!persisting) Quarantine(path); }
            catch (FormatException) { if (!persisting) Quarantine(path); }
            catch (ArgumentException) { Quarantine(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (Exception) { Console.Error.WriteLine("Mensagem pendente: falha de persistencia; nova tentativa sera realizada."); }
        }
        private static string Required(XElement root, string name)
        {
            string value = Optional(root, name); if (String.IsNullOrWhiteSpace(value)) throw new FormatException(); return value;
        }
        private static string Optional(XElement root, string name)
        {
            string value = (string)root.Attribute(name) ?? ""; if (value.Length > 256) throw new FormatException(); return value;
        }
        private void Quarantine(string path)
        {
            try { string target = Move(path, "quarantine"); File.WriteAllText(target + ".error.txt", "Mensagem de simulacao invalida. Revise o esquema e os campos; detalhes omitidos para proteger dados."); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        private string Move(string path, string folder)
        {
            string directory = Path.Combine(inbox, folder); Directory.CreateDirectory(directory);
            string target = Path.Combine(directory, Path.GetFileName(path));
            if (File.Exists(target)) target = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".xml");
            File.Move(path, target); return target;
        }
    }
}

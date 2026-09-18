using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Visep.Receiver
{
    public static class ReceiverProgram
    {
        public static int Main(string[] args)
        {
            try
            {
                if (args.Length > 0 && (args[0] == "--service-sg3" || args[0] == "--console-sg3"))
                {
                    IPAddress endpoint; int port;
                    if (args.Length != 5 || !IPAddress.TryParse(args[2], out endpoint) ||
                        !Int32.TryParse(args[3], NumberStyles.None, CultureInfo.InvariantCulture, out port) || port < 1 || port > 65535)
                        throw new ArgumentException("Uso: --service-sg3|--console-sg3 datafile ip port plain|b32");
                    var receiver = new Sg3ContinuousReceiver(args[1], endpoint, port, Sg3CaptureClient.ParseFraming(args[4]));
                    if (args[0] == "--service-sg3") ServiceBase.Run(new ReceiverService(receiver));
                    else using (var stop = new ManualResetEvent(false))
                    {
                        ConsoleCancelEventHandler cancel = delegate(object sender, ConsoleCancelEventArgs e) { e.Cancel = true; stop.Set(); };
                        Console.CancelKeyPress += cancel;
                        try { Console.WriteLine("SG3 continuo: captura com ACK; nao gera ocorrencias. Ctrl+C encerra."); receiver.Run(stop); }
                        finally { Console.CancelKeyPress -= cancel; }
                    }
                    return 0;
                }
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
                if (args.Length > 0 && args[0] == "--replay")
                {
                    if (args.Length != 2) throw new ArgumentException("Uso: --replay datafile");
                    return new InboxReceiver(args[1]).Replay() == 0 ? 0 : 1;
                }
                if (args.Length > 0 && args[0] == "--health")
                {
                    if (args.Length < 2 || args.Length > 4) throw new ArgumentException("Uso: --health datafile [minimumFreeMiB] [stalePendingMinutes]");
                    long minimumMiB = args.Length > 2 ? ParseNonNegative(args[2]) : 1024;
                    long staleMinutes = args.Length > 3 ? ParsePositive(args[3]) : 5;
                    if (minimumMiB > Int64.MaxValue / 1048576) throw new ArgumentOutOfRangeException();
                    ReceiverHealth health = ReceiverMonitor.Inspect(args[1], minimumMiB * 1048576, TimeSpan.FromMinutes(staleMinutes));
                    Console.WriteLine("Health: " + (health.IsHealthy ? "healthy" : "unhealthy") +
                        "; availableBytes=" + health.AvailableBytes + "; inbox=" + health.InboxFiles +
                        "; pending=" + health.PendingCaptures + "; stalePending=" + health.StalePendingCaptures +
                        "; captured=" + health.CapturedCaptures + "; delivered=" + health.DeliveredCaptures + "; invalid=" + health.InvalidCaptures +
                        "; corruptCaptures=" + health.CorruptCaptures + "; corruptPayloads=" + health.CorruptPayloads +
                        "; missingPayloads=" + health.MissingPayloads + "; unreferencedPayloads=" + health.UnreferencedPayloads +
                        "; futureCaptures=" + health.FutureCaptures + ".");
                    return health.IsHealthy ? 0 : 1;
                }
                if (args.Length > 0 && args[0] == "--retention")
                {
                    if (args.Length < 4 || args.Length > 5 || (args.Length == 5 && args[4] != "--apply"))
                        throw new ArgumentException("Uso: --retention datafile backupDirectory cutoffUtc [--apply]");
                    DateTime cutoff;
                    if (!args[3].EndsWith("Z", StringComparison.Ordinal) || !DateTime.TryParse(args[3], CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out cutoff)) throw new ArgumentException("cutoffUtc invalido.");
                    bool apply = args.Length == 5;
                    RetentionResult retention;
                    try { retention = JournalRetention.Execute(args[1], args[2], cutoff, apply); }
                    catch (InvalidDataException ex) { Console.Error.WriteLine("Retencao recusada: " + ex.Message); return 1; }
                    catch (IOException) { Console.Error.WriteLine("Retencao recusada: arquivos em uso ou inacessiveis."); return 1; }
                    catch (UnauthorizedAccessException) { Console.Error.WriteLine("Retencao recusada: acesso negado."); return 1; }
                    Console.WriteLine("Retention: candidateCaptures=" + retention.CandidateCaptures +
                        "; candidatePayloads=" + retention.CandidatePayloads + "; deletedCaptures=" + retention.DeletedCaptures +
                        "; deletedPayloads=" + retention.DeletedPayloads + "; mode=" + (apply ? "apply" : "preview") + ".");
                    return 0;
                }
                if (args.Length > 0 && args[0] == "--sg3-capture")
                {
                    if (args.Length != 8 || args[6] != "--confirm-live" || args[7] != "--send-ack")
                        throw new ArgumentException("Uso: --sg3-capture datafile ip port plain|b32 durationSeconds --confirm-live --send-ack");
                    IPAddress address;
                    int port, seconds;
                    if (!IPAddress.TryParse(args[2], out address) ||
                        !Int32.TryParse(args[3], NumberStyles.None, CultureInfo.InvariantCulture, out port) || port < 1 || port > 65535 ||
                        !Int32.TryParse(args[5], NumberStyles.None, CultureInfo.InvariantCulture, out seconds) || seconds < 10 || seconds > 600)
                        throw new ArgumentException("IP, porta ou duracao invalidos.");
                    Sg3Framing framing = Sg3CaptureClient.ParseFraming(args[4]);
                    Console.WriteLine("Captura SG3 iniciada com ACK ativo; payload nao sera exibido nem convertido em ocorrencia.");
                    try
                    {
                        Sg3CaptureResult result = new Sg3CaptureClient(args[1]).Run(address, port, framing, TimeSpan.FromSeconds(seconds));
                        Console.WriteLine("Captura SG3 concluida: frames=" + result.Frames + "; bytes=" + result.Bytes + "; framing=" + args[4].ToLowerInvariant() + ".");
                        return result.Frames > 0 ? 0 : 2;
                    }
                    catch (SocketException ex) { Console.Error.WriteLine("Falha TCP SG3: " + ex.SocketErrorCode + "."); return 1; }
                    catch (System.TimeoutException ex) { Console.Error.WriteLine(ex.Message); return 1; }
                    catch (InvalidDataException ex) { Console.Error.WriteLine("Falha de framing SG3: " + ex.Message); return 1; }
                }
                if (args.Length > 0 && args[0] == "--sg3-analyze")
                {
                    if (args.Length != 2) throw new ArgumentException("Uso: --sg3-analyze datafile");
                    try
                    {
                        Sg3AnalysisResult analysis = Sg3JournalAnalyzer.Analyze(Inbox(args[1]));
                        Console.WriteLine("SG3 analysis: captures=" + analysis.Captures + "; validCaptures=" + analysis.ValidCaptures +
                            "; invalidCaptures=" + analysis.InvalidCaptures + "; uniquePayloads=" + analysis.UniquePayloads +
                            "; validUniquePayloads=" + analysis.ValidUniquePayloads + "; invalidUniquePayloads=" + analysis.InvalidUniquePayloads +
                            "; keepAlive=" + analysis.KeepAlive + "; bracketEvents=" + analysis.BracketEvents +
                            "; numericEvents=" + analysis.NumericEvents + ".");
                        foreach (Sg3Category category in Enum.GetValues(typeof(Sg3Category)))
                            Console.WriteLine("SG3 category: " + category + "=" + analysis.CategoryCount(category) + ".");
                        return analysis.InvalidCaptures == 0 && analysis.InvalidUniquePayloads == 0 && analysis.ValidCaptures > 0 ? 0 : 1;
                    }
                    catch (InvalidDataException ex) { Console.Error.WriteLine("Analise SG3 recusada: " + ex.Message); return 1; }
                    catch (IOException) { Console.Error.WriteLine("Analise SG3 recusada: arquivos inacessiveis."); return 1; }
                    catch (UnauthorizedAccessException) { Console.Error.WriteLine("Analise SG3 recusada: acesso negado."); return 1; }
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
        private static long ParseNonNegative(string value)
        {
            long parsed; if (!Int64.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed) || parsed < 0) throw new ArgumentException(); return parsed;
        }
        private static long ParsePositive(string value)
        {
            long parsed = ParseNonNegative(value); if (parsed == 0) throw new ArgumentException(); return parsed;
        }
    }

    internal sealed class ReceiverService : ServiceBase
    {
        private readonly string dataFile;
        private readonly Sg3ContinuousReceiver sg3;
        private readonly ManualResetEvent stop = new ManualResetEvent(false);
        private Thread worker;
        internal ReceiverService(string path) { ServiceName = "VisepReceiver"; dataFile = path; CanStop = true; }
        internal ReceiverService(Sg3ContinuousReceiver receiver) { ServiceName = "VisepReceiver"; sg3 = receiver; CanStop = true; }
        protected override void OnStart(string[] args)
        {
            stop.Reset();
            worker = new Thread(delegate()
            {
                try
                {
                    if (sg3 != null) sg3.Run(stop);
                    else new InboxReceiver(args.Length > 0 ? args[0] : dataFile).Run(stop);
                }
                catch (Exception)
                {
                    ExitCode = 1;
                    Console.Error.WriteLine("Receptor encerrado por falha; verifique acesso e outra instancia ativa.");
                    ThreadPool.QueueUserWorkItem(delegate { Stop(); });
                }
            }); worker.IsBackground = true; worker.Start();
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
                    ProcessPending(stop);

                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                if (stop.WaitOne(1000)) break;
            }
        }
        internal void ProcessPending() { ProcessPending(null); }
        private FileStream Lock()
        {
            Directory.CreateDirectory(inbox);
            return new FileStream(Path.Combine(inbox, "receiver.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        private void ProcessPending(WaitHandle stop)
        {
            using (Lock())
            {
                foreach (string path in Directory.EnumerateFiles(inbox, "*.xml"))
                {
                    if (stop != null && stop.WaitOne(0)) break;
                    Process(path);
                }
            }
        }
        internal int Replay()
        {
            int failures = 0, replayed = 0, captureFailures = 0;
            using (Lock())
            {
                var journal = new RawJournal(inbox);
                if (!Directory.Exists(Path.Combine(inbox, "journal")))
                {
                    Console.Error.WriteLine("Journal nao encontrado; confira o diretorio de dados.");
                    return 1;
                }
                var captures = new CaptureJournal(inbox);
                Action<string> captureFailure = delegate(string path)
                {
                    captureFailures++;
                    Console.Error.WriteLine("Falha de metadata de captura: " + Path.GetFileName(path));
                };
                var pendingCaptures = captures.PendingIndex(captureFailure);
                var seen = new Dictionary<string, string>(StringComparer.Ordinal);
                var conflicts = new HashSet<string>(StringComparer.Ordinal);
                foreach (string entry in journal.Entries())
                {
                    try
                    {
                        string id; Parse(journal.Read(entry), out id);
                        string previous;
                        if (seen.TryGetValue(id, out previous) && previous != entry) conflicts.Add(id);
                        else seen[id] = entry;
                    }
                    catch (Exception) { /* The processing pass reports invalid entries. */ }
                }
                foreach (string path in journal.Entries())
                {
                    try
                    {
                        string id; Action persist = Parse(journal.Read(path), out id);
                        if (conflicts.Contains(id)) throw new InvalidDataException("MessageId com payloads conflitantes.");
                        persist();
                        captures.DeliverPending(Path.GetFileNameWithoutExtension(path), pendingCaptures, captureFailure);
                        replayed++;
                    }
                    catch (Exception) { failures++; Console.Error.WriteLine("Registro nao reprocessado: " + Path.GetFileName(path)); }
                }
            }
            Console.WriteLine("Replay: " + replayed + " registros; " + failures + " falhas de payload; " + captureFailures + " falhas de metadata.");
            return failures + captureFailures;
        }
        private void Process(string path)
        {
            try
            {
                byte[] bytes = RawJournal.ReadBounded(path);
                new RawJournal(inbox).Append(bytes);
                var captures = new CaptureJournal(inbox);
                string capture = captures.Append(path, bytes);
                Action persist;
                try { persist = Parse(bytes); }
                catch (XmlException) { captures.SetState(capture, "invalid"); Quarantine(path); return; }
                catch (FormatException) { captures.SetState(capture, "invalid"); Quarantine(path); return; }
                catch (ArgumentException) { captures.SetState(capture, "invalid"); Quarantine(path); return; }
                persist();
                captures.SetState(capture, "delivered");
                Move(path, "processed");
            }
            catch (Exception) { Console.Error.WriteLine("Mensagem pendente: falha de journal, captura ou persistencia; nova tentativa sera realizada."); }
        }
        private Action Parse(byte[] bytes) { string id; return Parse(bytes, out id); }
        private Action Parse(byte[] bytes, out string messageId) {
                XDocument document;
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 65536 };
                using (var reader = XmlReader.Create(new MemoryStream(bytes, false), settings)) document = XDocument.Load(reader);
                XElement root = document.Root;
                if (root == null || root.Name != "simulation" || root.HasElements) throw new FormatException();
                string id = Required(root, "id"), account = Required(root, "account"), code = Required(root, "code");
                string zone = Required(root, "zone"), partition = Required(root, "partition"), originText = Optional(root, "originUtc");
                if (id.Length > 128 || account.Length > 64 || code.Length > 32 || zone.Length > 32 || partition.Length > 32) throw new FormatException();
                DateTime? origin = null;
                if (originText.Length > 0)
                {
                    DateTime parsed;
                    if (!originText.EndsWith("Z", StringComparison.Ordinal) || !DateTime.TryParse(originText, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed)) throw new FormatException();
                    origin = parsed;
                }

                messageId = id;
                return delegate { new Visep.Store(dataFile).ReceiveSimulation(id, account, code, zone, partition, origin); };
        }
        private static string Required(XElement root, string name)
        {
            string value = Optional(root, name).Trim();
            if (value.Length == 0) throw new FormatException();
            foreach (char character in value) if (Char.IsControl(character)) throw new FormatException();
            return value;
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

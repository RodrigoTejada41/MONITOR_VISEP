using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Visep.Receiver
{
    internal enum Sg3MessageKind { KeepAlive, BracketEvent, NumericEvent }

    internal sealed class Sg3NumericFields
    {
        internal string Account { get; private set; }
        internal string EventCode { get; private set; }
        internal string Field2 { get; private set; }
        internal string Field3 { get; private set; }

        private Sg3NumericFields() { }

        internal static Sg3NumericFields DecodeObserved(string header, string receiver, string signal)
        {
            if (header != "501001" || receiver == null || signal == null ||
                receiver.Length != 6 || signal.Length != 8 || !receiver.StartsWith("18", StringComparison.Ordinal)) return null;
            foreach (char value in receiver + signal)
                if (value < '0' || value > '9') return null;
            return new Sg3NumericFields {
                Account = receiver.Substring(2, 4), EventCode = signal.Substring(0, 3),
                Field2 = signal.Substring(3, 2), Field3 = signal.Substring(5, 3)
            };
        }
    }

    internal sealed class Sg3Message
    {
        internal Sg3MessageKind Kind { get; private set; }
        internal string Sequence { get; private set; }
        internal string Account { get; private set; }
        internal string Code { get; private set; }
        internal string Detail { get; private set; }
        internal string Receiver { get; private set; }
        internal char Qualifier { get; private set; }
        internal string Signal { get; private set; }
        internal Sg3NumericFields NumericFields { get; private set; }

        private Sg3Message() { }

        internal static Sg3Message KeepAlive(string sequence)
        {
            return new Sg3Message { Kind = Sg3MessageKind.KeepAlive, Sequence = sequence };
        }

        internal static Sg3Message Bracket(string sequence, string account, string code, string detail)
        {
            return new Sg3Message { Kind = Sg3MessageKind.BracketEvent, Sequence = sequence, Account = account, Code = code, Detail = detail };
        }

        internal static Sg3Message Numeric(string sequence, string receiver, char qualifier, string signal)
        {
            return new Sg3Message { Kind = Sg3MessageKind.NumericEvent, Sequence = sequence, Receiver = receiver, Qualifier = qualifier, Signal = signal,
                NumericFields = Sg3NumericFields.DecodeObserved(sequence, receiver, signal) };
        }
    }

    internal static class Sg3MessageParser
    {
        private static readonly Regex KeepAlive = new Regex("^(?<sequence>[0-9]{6}) {11}@ {4}$", RegexOptions.CultureInvariant);
        private static readonly Regex Bracket = new Regex("^(?<sequence>[0-9]{6})\\[#(?<account>[0-9]{4}|[0-9]{10})\\|(?<code>N[A-Z]{2})(?<detail>[A-Z0-9]{4}|\\*[0-9]{1,3}(?:\\.[0-9]{1,3}){3}\\*)\\]$", RegexOptions.CultureInvariant);
        private static readonly Regex Numeric = new Regex("^(?<sequence>[0-9]{6}) (?<receiver>[0-9]{6})(?<qualifier>[ER])(?<signal>[0-9]{8})$", RegexOptions.CultureInvariant);

        internal static bool TryParse(byte[] frame, out Sg3Message message)
        {
            message = null;
            if (frame == null || frame.Length < 2 || frame.Length > 128 || frame[frame.Length - 1] != 0x14) return false;
            for (int index = 0; index < frame.Length - 1; index++)
                if (frame[index] < 0x20 || frame[index] > 0x7e) return false;

            string text = Encoding.ASCII.GetString(frame, 0, frame.Length - 1);
            Match match = KeepAlive.Match(text);
            if (match.Success)
            {
                message = Sg3Message.KeepAlive(match.Groups["sequence"].Value);
                return true;
            }

            match = Bracket.Match(text);
            if (match.Success)
            {
                string detail = match.Groups["detail"].Value;
                if (detail[0] == '*' && !ValidAddress(detail.Substring(1, detail.Length - 2))) return false;
                message = Sg3Message.Bracket(match.Groups["sequence"].Value, match.Groups["account"].Value,
                    match.Groups["code"].Value, detail);
                return true;
            }

            match = Numeric.Match(text);
            if (!match.Success) return false;
            message = Sg3Message.Numeric(match.Groups["sequence"].Value, match.Groups["receiver"].Value,
                match.Groups["qualifier"].Value[0], match.Groups["signal"].Value);
            return true;
        }

        private static bool ValidAddress(string value)
        {
            string[] parts = value.Split('.');
            if (parts.Length != 4) return false;
            foreach (string part in parts)
            {
                int octet;
                if (part.Length == 0 || part.Length > 3 || !Int32.TryParse(part, out octet) || octet > 255) return false;
            }
            return true;
        }
    }

    internal sealed class Sg3AnalysisResult
    {
        internal int UniquePayloads { get; set; }
        internal int ValidUniquePayloads { get; set; }
        internal int InvalidUniquePayloads { get; set; }
        internal int Captures { get; set; }
        internal int ValidCaptures { get; set; }
        internal int InvalidCaptures { get; set; }
        internal int KeepAlive { get; set; }
        internal int BracketEvents { get; set; }
        internal int NumericEvents { get; set; }
        private readonly Dictionary<Sg3Category, int> categories = new Dictionary<Sg3Category, int>();

        internal int CategoryCount(Sg3Category category)
        {
            int count;
            return categories.TryGetValue(category, out count) ? count : 0;
        }

        internal void CountCategory(Sg3Category category)
        {
            categories[category] = CategoryCount(category) + 1;
        }
    }

    internal static class Sg3JournalAnalyzer
    {
        internal static Sg3AnalysisResult Analyze(string inbox)
        {
            var result = new Sg3AnalysisResult();
            var journal = new RawJournal(inbox);
            string journalDirectory = Path.Combine(inbox, "journal");
            string captureDirectory = Path.Combine(inbox, "captures");
            if (!Directory.Exists(journalDirectory)) throw new InvalidDataException("Journal SG3 nao encontrado.");
            if (!Directory.Exists(captureDirectory)) throw new InvalidDataException("Envelopes SG3 nao encontrados.");
            var captureHashes = new List<string>();
            foreach (string path in Directory.EnumerateFiles(captureDirectory, "*.xml"))
            {
                try
                {
                    XElement capture = CaptureJournal.Load(path);
                    if ((string)capture.Attribute("source") != "sg3-tcp") continue;
                    result.Captures++;
                    captureHashes.Add((string)capture.Attribute("payloadSha256"));
                }
                catch (IOException) { result.Captures++; result.InvalidCaptures++; }
                catch (UnauthorizedAccessException) { result.Captures++; result.InvalidCaptures++; }
                catch (InvalidDataException) { result.Captures++; result.InvalidCaptures++; }
                catch (XmlException) { result.Captures++; result.InvalidCaptures++; }
            }
            var expected = new HashSet<string>(captureHashes, StringComparer.Ordinal);
            var parsed = new Dictionary<string, Sg3Message>(StringComparer.Ordinal);
            foreach (string entry in journal.Entries())
            {
                string hash = Path.GetFileNameWithoutExtension(entry);
                if (!expected.Contains(hash)) continue;
                result.UniquePayloads++;
                Sg3Message message;
                try
                {
                    if (!Sg3MessageParser.TryParse(journal.Read(entry), out message))
                    {
                        result.InvalidUniquePayloads++;
                        continue;
                    }
                }
                catch (IOException)
                {
                    result.InvalidUniquePayloads++;
                    continue;
                }
                catch (InvalidDataException)
                {
                    result.InvalidUniquePayloads++;
                    continue;
                }
                result.ValidUniquePayloads++;
                parsed.Add(hash, message);
            }
            foreach (string hash in captureHashes)
            {
                Sg3Message message;
                if (!parsed.TryGetValue(hash, out message))
                {
                    result.InvalidCaptures++;
                    continue;
                }
                result.ValidCaptures++;
                result.CountCategory(Sg3Classifier.Classify(message));
                if (message.Kind == Sg3MessageKind.KeepAlive) result.KeepAlive++;
                else if (message.Kind == Sg3MessageKind.BracketEvent) result.BracketEvents++;
                else result.NumericEvents++;
            }
            return result;
        }
    }
}

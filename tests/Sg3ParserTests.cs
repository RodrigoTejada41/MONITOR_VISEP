using System;
using System.IO;
using System.Text;
using Visep.Receiver;

class Sg3ParserTests
{
    static int assertions;

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        assertions++;
    }

    static byte[] Frame(string value)
    {
        return Encoding.ASCII.GetBytes(value + "\x14");
    }

    static void ClassificationTests()
    {
        string[] frames = {
            "501001 180007E60201000", "501001 180007R60201000",
            "501001 180007E35100000", "501001 180007R35100000",
            "501001 180007R35400000", "501001 180007E35401000",
            "501001 180007E60800000", "501001 180007R60800000",
            "501001 180007E40201003", "501001 180007R40201001",
            "501001 180007E99900000", "501002 180007E60201000",
            "001000[#0000|NSC0003]", "001000[#0000|NSC0000]",
            "001000[#0000|NSC0001]", "001000[#0000|NYY0000]",
            "001001[#0000000007|NYC*192.0.2.1*]",
            "001001[#0000000007|NYK*192.0.2.1*]",
            "001001[#0000000007|NYC0000]", "101000           @    "
        };
        Sg3Category[] expected = {
            Sg3Category.PeriodicTest, Sg3Category.Unknown,
            Sg3Category.TroubleTopic, Sg3Category.TroubleTopic,
            Sg3Category.TroubleTopic, Sg3Category.TroubleTopic,
            Sg3Category.TroubleTopic, Sg3Category.Unknown,
            Sg3Category.Operational, Sg3Category.Operational,
            Sg3Category.Unknown, Sg3Category.Unknown,
            Sg3Category.ReceiverControl, Sg3Category.ReceiverControl,
            Sg3Category.Unknown, Sg3Category.ReceiverControl,
            Sg3Category.TransmitterFailure, Sg3Category.TransmitterRestoral,
            Sg3Category.Unknown, Sg3Category.KeepAlive
        };
        for (int i = 0; i < frames.Length; i++)
        {
            Sg3Message message;
            Check(Sg3MessageParser.TryParse(Frame(frames[i]), out message), "classification fixture parses " + i);
            Check(Sg3Classifier.Classify(message) == expected[i], "conservative classification " + i);
        }
        Check(Sg3Classifier.Classify(null) == Sg3Category.Unknown, "missing message unclassified");
    }

    static void Main()
    {
        Sg3Message message;

        Check(Sg3MessageParser.TryParse(Frame("000001           @    "), out message), "keepalive parsed");
        Check(message.Kind == Sg3MessageKind.KeepAlive && message.Sequence == "000001", "keepalive fields");

        Check(Sg3MessageParser.TryParse(Frame("000002[#1234|NSC0003]"), out message), "bracket event parsed");
        Check(message.Kind == Sg3MessageKind.BracketEvent, "bracket kind");
        Check(message.Sequence == "000002" && message.Account == "1234", "bracket identity");
        Check(message.Code == "NSC" && message.Detail == "0003", "bracket payload");

        Check(Sg3MessageParser.TryParse(Frame("000003[#1234567890|NYC*192.168.1.1*]"), out message), "network detail parsed");
        Check(message.Account == "1234567890" && message.Code == "NYC" && message.Detail == "*192.168.1.1*", "network fields");
        Check(Sg3MessageParser.TryParse(Frame("000003[#1234|NYD999A]"), out message) && message.Detail == "999A", "alphanumeric detail parsed");

        Check(Sg3MessageParser.TryParse(Frame("000004 123456E12345678"), out message), "numeric event parsed");
        Check(message.Kind == Sg3MessageKind.NumericEvent, "numeric kind");
        Check(message.Sequence == "000004" && message.Receiver == "123456", "numeric identity");
        Check(message.Qualifier == 'E' && message.Signal == "12345678", "numeric payload");

        Check(!Sg3MessageParser.TryParse(Encoding.ASCII.GetBytes("000001           @    "), out message), "terminator required");
        Check(!Sg3MessageParser.TryParse(Frame("00001[#1234|NSC0003]"), out message), "sequence length required");
        Check(!Sg3MessageParser.TryParse(Frame("000002[#12|NSC0003]"), out message), "account length required");
        Check(!Sg3MessageParser.TryParse(Frame("000002[#1234|nsC0003]"), out message), "uppercase code required");
        Check(!Sg3MessageParser.TryParse(Frame("000003[#1234|NYC*999.168.1.1*]"), out message), "invalid address rejected");
        Check(!Sg3MessageParser.TryParse(Frame("000004 123456X12345678"), out message), "qualifier restricted");
        Check(!Sg3MessageParser.TryParse(new byte[] { 0x80, 0x14 }, out message), "ASCII required");

        Check(Sg3MessageParser.TryParse(Frame("501001 180007E60201000"), out message), "observed numeric layout accepted");
        Check(message.NumericFields != null, "observed layout decoded");
        Check(message.NumericFields.Account == "0007" && message.NumericFields.EventCode == "602", "account zeros and event preserved");
        Check(message.NumericFields.Field2 == "01" && message.NumericFields.Field3 == "000", "suffix fields preserved without semantics");
        Check(message.Receiver == "180007" && message.Signal == "60201000" && message.Qualifier == 'E', "raw fields preserved");
        Check(Sg3MessageParser.TryParse(Frame("501001 180007R35100009"), out message) && message.NumericFields.EventCode == "351" && message.Qualifier == 'R', "qualifier preserved without classification");
        Check(Sg3MessageParser.TryParse(Frame("501001 990007E60201000"), out message) && message.NumericFields == null, "unknown format stays structural");
        Check(Sg3MessageParser.TryParse(Frame("501002 180007E60201000"), out message) && message.NumericFields == null, "unknown header stays structural");
        Check(Sg3MessageParser.TryParse(Frame("000001           @    "), out message) && message.NumericFields == null, "keepalive has no numeric fields");
        Check(!Sg3MessageParser.TryParse(Frame("501001 180007E6020100"), out message) && message == null, "short numeric payload rejected");

        ClassificationTests();

        string root = Path.Combine(Path.GetTempPath(), "visep-sg3-parser-" + Guid.NewGuid().ToString("N"));
        try
        {
            string inbox = Path.Combine(root, "inbox");
            var journal = new RawJournal(inbox);
            var captures = new CaptureJournal(inbox);
            byte[][] frames = {
                Frame("000001           @    "), Frame("000002[#1234|NSC0003]"),
                Frame("000004 123456R12345678"), Encoding.ASCII.GetBytes("invalid\x14")
            };
            foreach (byte[] frame in frames)
            {
                journal.Append(frame);
                string capture = captures.Append("sg3-tcp", frame);
                captures.SetState(capture, "captured");
            }
            string duplicate = captures.Append("sg3-tcp", frames[2]);
            captures.SetState(duplicate, "captured");
            byte[] simulation = Encoding.UTF8.GetBytes("<simulation />");
            journal.Append(simulation);
            captures.Append("simulation.xml", simulation);
            Sg3AnalysisResult result = Sg3JournalAnalyzer.Analyze(inbox);
            Check(result.UniquePayloads == 4 && result.ValidUniquePayloads == 3 && result.InvalidUniquePayloads == 1, "unique analysis totals");
            Check(result.Captures == 5 && result.ValidCaptures == 4 && result.InvalidCaptures == 1, "capture analysis totals");
            Check(result.KeepAlive == 1 && result.BracketEvents == 1 && result.NumericEvents == 2, "capture families");
            Check(result.Captures == 5 && result.UniquePayloads == 4, "non-SG3 captures ignored");
            Check(result.CategoryCount(Sg3Category.KeepAlive) == 1 && result.CategoryCount(Sg3Category.Unknown) == 3,
                "categories count valid captures including duplicate, not invalid payloads");

            bool missingRejected = false;
            try { Sg3JournalAnalyzer.Analyze(Path.Combine(root, "missing")); }
            catch (InvalidDataException) { missingRejected = true; }
            Check(missingRejected, "missing journal rejected clearly");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        Console.WriteLine("SG3 parser: " + assertions + " assertions passed.");
    }
}

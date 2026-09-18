using System;

namespace Visep.Receiver
{
    internal enum Sg3Category
    {
        Unknown, KeepAlive, PeriodicTest, TroubleTopic, Operational,
        ReceiverControl, TransmitterFailure, TransmitterRestoral
    }

    internal static class Sg3Classifier
    {
        internal static Sg3Category Classify(Sg3Message message)
        {
            if (message == null) return Sg3Category.Unknown;
            if (message.Kind == Sg3MessageKind.KeepAlive) return Sg3Category.KeepAlive;
            if (message.Kind == Sg3MessageKind.BracketEvent) return ClassifyBracket(message);
            if (message.NumericFields == null) return Sg3Category.Unknown;
            string key = message.Qualifier + message.NumericFields.EventCode;
            switch (key)
            {
                case "E602": return Sg3Category.PeriodicTest;
                case "E351": case "R351": case "E354": case "R354": case "E608":
                    return Sg3Category.TroubleTopic;
                case "E402": case "R402": return Sg3Category.Operational;
                default: return Sg3Category.Unknown;
            }
        }

        private static Sg3Category ClassifyBracket(Sg3Message message)
        {
            if (message.Sequence == "001000" && message.Account == "0000")
            {
                if (message.Code == "NSC" && (message.Detail == "0000" || message.Detail == "0003"))
                    return Sg3Category.ReceiverControl;
                if (message.Code == "NYY" && message.Detail == "0000")
                    return Sg3Category.ReceiverControl;
            }
            if (message.Sequence == "001001" && message.Detail != null &&
                message.Detail.StartsWith("*", StringComparison.Ordinal))
            {
                if (message.Code == "NYC") return Sg3Category.TransmitterFailure;
                if (message.Code == "NYK") return Sg3Category.TransmitterRestoral;
            }
            return Sg3Category.Unknown;
        }
    }
}

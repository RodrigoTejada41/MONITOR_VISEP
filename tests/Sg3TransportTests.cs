using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Visep.Receiver;

class Sg3TransportTests
{
    private static int assertions;
    private static void Check(bool condition, string name)
    {
        assertions++;
        if (!condition) throw new Exception("FAIL: " + name);
    }

    private static void PlainFramesFragmentedAndCombined()
    {
        var decoder = new Sg3FrameDecoder(Sg3Framing.Plain);
        Check(decoder.Push(new byte[] { 0x41 }).Count == 0, "plain waits for terminator");
        var frames = decoder.Push(new byte[] { 0x42, 0x14, 0x43, 0x14 });
        Check(frames.Count == 2, "plain returns combined frames");
        Check(frames[0].SequenceEqual(new byte[] { 0x41, 0x42, 0x14 }), "plain preserves first payload");
        Check(frames[1].SequenceEqual(new byte[] { 0x43, 0x14 }), "plain preserves second payload");
    }

    private static void B32FramesFragmentedAndCombined()
    {
        var decoder = new Sg3FrameDecoder(Sg3Framing.B32);
        Check(decoder.Push(new byte[] { 0x30, 0x30 }).Count == 0, "b32 waits for header");
        var frames = decoder.Push(new byte[] { 0x30, 0x36, 0x41, 0x14, 0x30, 0x30, 0x30, 0x36, 0x42, 0x14 });
        Check(frames.Count == 2, "b32 returns combined frames");
        Check(frames[0].SequenceEqual(new byte[] { 0x41, 0x14 }), "b32 strips first header");
        Check(frames[1].SequenceEqual(new byte[] { 0x42, 0x14 }), "b32 strips second header");
    }

    private static void RejectsInvalidAndOversizedFrames()
    {
        var invalid = new Sg3FrameDecoder(Sg3Framing.B32);
        try { invalid.Push(new byte[] { 0x30, 0x30, 0x58, 0x36 }); Check(false, "invalid header rejected"); }
        catch (InvalidDataException) { Check(true, "invalid header rejected"); }

        var oversized = new Sg3FrameDecoder(Sg3Framing.Plain, 3);
        try { oversized.Push(new byte[] { 1, 2, 3, 4 }); Check(false, "oversized plain rejected"); }
        catch (InvalidDataException) { Check(true, "oversized plain rejected"); }
    }

    private static void AckMatchesFraming()
    {
        Check(Sg3Protocol.Ack(Sg3Framing.Plain).SequenceEqual(new byte[] { 0x06 }), "plain ACK");
        Check(Sg3Protocol.Ack(Sg3Framing.B32).SequenceEqual(new byte[] { 0x30, 0x30, 0x30, 0x35, 0x06 }), "b32 ACK");
    }

    private static void ReportsPartialFrame()
    {
        var decoder = new Sg3FrameDecoder(Sg3Framing.Plain);
        decoder.Push(new byte[] { 0x41 });
        Check(decoder.PendingBytes == 1, "partial frame is observable");
    }

    private static void ClientPersistsBeforeB32Ack()
    {
        string root = Path.Combine(Path.GetTempPath(), "visep-sg3-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        byte[] receivedAck = null;
        Exception serverFailure = null;
        var server = new Thread(delegate()
        {
            try
            {
                using (TcpClient accepted = listener.AcceptTcpClient())
                using (NetworkStream stream = accepted.GetStream())
                {
                    byte[] frame = new byte[] { 0x30, 0x30, 0x30, 0x36, 0x41, 0x14 };
                    stream.Write(frame, 0, 2);
                    stream.Write(frame, 2, frame.Length - 2);
                    receivedAck = new byte[5];
                    int offset = 0;
                    while (offset < receivedAck.Length)
                    {
                        int read = stream.Read(receivedAck, offset, receivedAck.Length - offset);
                        if (read == 0) throw new EndOfStreamException();
                        offset += read;
                    }
                }
            }
            catch (Exception ex) { serverFailure = ex; }
        });
        server.Start();
        try
        {
            string data = Path.Combine(root, "data.xml");
            Sg3CaptureResult result = new Sg3CaptureClient(data).Run(IPAddress.Loopback, port, Sg3Framing.B32, TimeSpan.FromSeconds(10));
            server.Join(5000);
            if (serverFailure != null) throw serverFailure;
            Check(result.Frames == 1 && result.Bytes == 2, "client counts captured frame");
            Check(receivedAck != null && receivedAck.SequenceEqual(Sg3Protocol.Ack(Sg3Framing.B32)), "client sends b32 ACK");
            Check(Directory.GetFiles(Path.Combine(root, "inbox", "journal"), "*.raw").Length == 1, "client persists raw frame");
            Check(Directory.GetFiles(Path.Combine(root, "inbox", "captures"), "*.xml").Length == 1, "client persists capture envelope");
            Check((string)System.Xml.Linq.XElement.Load(Directory.GetFiles(Path.Combine(root, "inbox", "captures"), "*.xml")[0]).Attribute("state") == "captured", "client marks durable SG3 capture");
        }
        finally
        {
            listener.Stop();
            if (server.IsAlive) server.Join(1000);
            Directory.Delete(root, true);
        }
    }

    private static void PersistenceFailureDoesNotAck()
    {
        string root = Path.Combine(Path.GetTempPath(), "visep-sg3-fail-" + Guid.NewGuid().ToString("N"));
        string inbox = Path.Combine(root, "inbox");
        Directory.CreateDirectory(inbox);
        File.WriteAllText(Path.Combine(inbox, "journal"), "blocked");
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        bool ackReceived = false;
        Exception serverFailure = null;
        var server = new Thread(delegate()
        {
            try
            {
                using (TcpClient accepted = listener.AcceptTcpClient())
                using (NetworkStream stream = accepted.GetStream())
                {
                    stream.ReadTimeout = 1500;
                    stream.WriteByte(0x41); stream.WriteByte(0x14);
                    try { ackReceived = stream.ReadByte() >= 0; }
                    catch (IOException) { ackReceived = false; }
                }
            }
            catch (Exception ex) { serverFailure = ex; }
        });
        server.Start();
        try
        {
            try
            {
                new Sg3CaptureClient(Path.Combine(root, "data.xml")).Run(IPAddress.Loopback, port, Sg3Framing.Plain, TimeSpan.FromSeconds(10));
                Check(false, "persistence failure is reported");
            }
            catch (IOException) { Check(true, "persistence failure is reported"); }
            server.Join(5000);
            if (serverFailure != null) throw serverFailure;
            Check(!ackReceived, "persistence failure sends no ACK");
        }
        finally
        {
            listener.Stop();
            if (server.IsAlive) server.Join(1000);
            Directory.Delete(root, true);
        }
    }

    private static void ClientRejectsPartialFrameAtDisconnect()
    {
        string root = Path.Combine(Path.GetTempPath(), "visep-sg3-partial-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = new Thread(delegate()
        {
            using (TcpClient accepted = listener.AcceptTcpClient()) accepted.GetStream().WriteByte(0x41);
        });
        server.Start();
        try
        {
            try
            {
                new Sg3CaptureClient(Path.Combine(root, "data.xml")).Run(IPAddress.Loopback, port, Sg3Framing.Plain, TimeSpan.FromSeconds(10));
                Check(false, "partial disconnect is rejected");
            }
            catch (InvalidDataException) { Check(true, "partial disconnect is rejected"); }
        }
        finally
        {
            listener.Stop();
            server.Join(1000);
            Directory.Delete(root, true);
        }
    }

    static int Main()
    {
        PlainFramesFragmentedAndCombined();
        B32FramesFragmentedAndCombined();
        RejectsInvalidAndOversizedFrames();
        AckMatchesFraming();
        ReportsPartialFrame();
        ClientPersistsBeforeB32Ack();
        PersistenceFailureDoesNotAck();
        ClientRejectsPartialFrameAtDisconnect();
        Console.WriteLine("SG3 transport: " + assertions + " assertions passed.");
        return 0;
    }
}

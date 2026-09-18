using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Linq;
using Visep.Receiver;

class Sg3ContinuousTests
{
    static int checks;
    static void Check(bool value, string name) { checks++; if (!value) throw new Exception(name); }
    static void RejectConcurrentAndFailedPersistence()
    {
        string root = Path.Combine(Path.GetTempPath(), "visep-noack-" + Guid.NewGuid().ToString("N"));
        string inbox = Path.Combine(root, "inbox"); Directory.CreateDirectory(inbox);
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        using (var stop = new ManualResetEvent(false))
        {
            var receiver = new Sg3ContinuousReceiver(Path.Combine(root, "data.xml"), IPAddress.Loopback,
                ((IPEndPoint)listener.LocalEndpoint).Port, Sg3Framing.Plain);
            using (var ownership = new FileStream(Path.Combine(inbox, "receiver.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                try { receiver.Run(stop); Check(false, "exclusive receiver lock"); }
                catch (IOException) { Check(true, "exclusive receiver lock"); }
            }
            File.WriteAllText(Path.Combine(inbox, "captures"), "block capture persistence");
            Exception failure = null;
            var worker = new Thread(delegate() { try { receiver.Run(stop); } catch (Exception ex) { failure = ex; } });
            worker.Start();
            try
            {
                var accept = listener.BeginAcceptTcpClient(null, null);
                Check(accept.AsyncWaitHandle.WaitOne(5000), "failure test connected");
                using (var peer = listener.EndAcceptTcpClient(accept))
                {
                    peer.ReceiveTimeout = 3000;
                    peer.GetStream().Write(new byte[] { 0x41, 0x14 }, 0, 2);
                    Check(peer.GetStream().ReadByte() == -1, "persist failure closes without ACK");
                }
                stop.Set(); Check(worker.Join(3000), "backoff cancellation bounded");
                Check(failure == null, "persistence failure handled for retry");
            }
            finally { stop.Set(); worker.Join(7000); listener.Stop(); Directory.Delete(root, true); }
        }
    }
    public static int Main()
    {
        RejectConcurrentAndFailedPersistence();
        string root = Path.Combine(Path.GetTempPath(), "visep-continuous-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var stop = new ManualResetEvent(false);
        Exception failure = null;
        var receiver = new Sg3ContinuousReceiver(Path.Combine(root, "data.xml"), IPAddress.Loopback,
            ((IPEndPoint)listener.LocalEndpoint).Port, Sg3Framing.Plain);
        var worker = new Thread(delegate() { try { receiver.Run(stop); } catch (Exception ex) { failure = ex; } });
        worker.Start();
        try
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var accept = listener.BeginAcceptTcpClient(null, null);
                Check(accept.AsyncWaitHandle.WaitOne(10000), "initial connection and reconnect bounded");
                using (var peer = listener.EndAcceptTcpClient(accept))
                {
                    peer.ReceiveTimeout = 5000;
                    var stream = peer.GetStream();
                    stream.Write(new byte[] { 0x41, 0x14 }, 0, 2);
                    Check(stream.ReadByte() == 6, "ACK received");
                    string inbox = Path.Combine(root, "inbox");
                    var captures = Directory.GetFiles(Path.Combine(inbox, "captures"), "*.xml");
                    Check(captures.Length == attempt + 1, "each retransmission persisted before ACK");
                    foreach (string capture in captures)
                        Check((string)XElement.Load(capture).Attribute("state") == "captured", "capture durable before ACK");
                    Check(Directory.GetFiles(Path.Combine(inbox, "journal")).Length > 0, "raw durable before ACK");
                    if (attempt == 1)
                    {
                        stop.Set();
                        Check(worker.Join(3000), "idle read cancellation bounded");
                    }
                }
            }
            Check(failure == null, "worker completed without exception");
            Check(!File.Exists(Path.Combine(root, "data.xml")), "capture does not invent occurrences");
            Console.WriteLine("SG3 continuous: " + checks + " checks passed.");
            return 0;
        }
        finally
        {
            stop.Set(); worker.Join(7000); listener.Stop(); stop.Dispose();
            Directory.Delete(root, true);
        }
    }
}

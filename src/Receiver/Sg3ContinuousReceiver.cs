using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Visep.Receiver
{
    internal sealed class Sg3ContinuousReceiver
    {
        private readonly string inbox;
        private readonly IPAddress address;
        private readonly int port;
        private readonly Sg3Framing framing;

        internal Sg3ContinuousReceiver(string path, IPAddress endpoint, int endpointPort, Sg3Framing mode)
        {
            if (endpoint == null) throw new ArgumentNullException("endpoint");
            if (endpointPort < 1 || endpointPort > 65535) throw new ArgumentOutOfRangeException("endpointPort");
            inbox = ReceiverProgram.Inbox(path); address = endpoint; port = endpointPort; framing = mode;
        }

        internal void Run(WaitHandle stop)
        {
            Directory.CreateDirectory(inbox);
            using (var ownership = new FileStream(Path.Combine(inbox, "receiver.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                int delay = 1000;
                while (!stop.WaitOne(0))
                {
                    try { Capture(stop, delegate { delay = 1000; }); }
                    catch (SocketException) { Console.Error.WriteLine("SG3: falha TCP; reconectando."); }
                    catch (IOException) { Console.Error.WriteLine("SG3: conexao, framing ou persistencia falhou; sem ACK para frame nao persistido."); }
                    catch (UnauthorizedAccessException) { Console.Error.WriteLine("SG3: persistencia sem permissao; sem ACK."); }
                    catch (TimeoutException) { Console.Error.WriteLine("SG3: timeout de conexao; reconectando."); }
                    if (stop.WaitOne(delay)) break;
                    delay = Math.Min(delay * 2, 30000);
                }
            }
        }

        private void Capture(WaitHandle stop, Action captured)
        {
            using (var client = new TcpClient(address.AddressFamily))
            {
                IAsyncResult pending = client.BeginConnect(address, port, null, null);
                using (WaitHandle connected = pending.AsyncWaitHandle)
                {
                    int result = WaitHandle.WaitAny(new WaitHandle[] { stop, connected }, 5000);
                    if (result == 0) return;
                    if (result == WaitHandle.WaitTimeout) throw new TimeoutException();
                    client.EndConnect(pending);
                }
                client.ReceiveTimeout = 500;
                client.SendTimeout = 1000;
                using (NetworkStream stream = client.GetStream())
                {
                    var decoder = new Sg3FrameDecoder(framing);
                    var raw = new RawJournal(inbox);
                    var captures = new CaptureJournal(inbox);
                    byte[] buffer = new byte[4096], ack = Sg3Protocol.Ack(framing);
                    while (!stop.WaitOne(0))
                    {
                        int read;
                        try { read = stream.Read(buffer, 0, buffer.Length); }
                        catch (IOException ex)
                        {
                            var socket = ex.InnerException as SocketException;
                            if (socket != null && socket.SocketErrorCode == SocketError.TimedOut) continue;
                            throw;
                        }
                        if (read == 0) break;
                        byte[] chunk = new byte[read]; Buffer.BlockCopy(buffer, 0, chunk, 0, read);
                        foreach (byte[] frame in decoder.Push(chunk))
                        {
                            if (stop.WaitOne(0)) return;
                            raw.Append(frame);
                            string capture = captures.Append("sg3-tcp", frame);
                            captures.SetState(capture, "captured");
                            stream.Write(ack, 0, ack.Length);
                            captured();
                        }
                    }
                    if (!stop.WaitOne(0) && decoder.PendingBytes != 0)
                        throw new InvalidDataException("SG3: frame parcial descartado sem ACK.");
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Visep.Receiver
{
    internal enum Sg3Framing { Plain, B32 }

    internal static class Sg3Protocol
    {
        internal static byte[] Ack(Sg3Framing framing)
        {
            return framing == Sg3Framing.B32
                ? new byte[] { 0x30, 0x30, 0x30, 0x35, 0x06 }
                : new byte[] { 0x06 };
        }
    }

    internal sealed class Sg3FrameDecoder
    {
        private readonly Sg3Framing framing;
        private readonly int maximumBytes;
        private readonly List<byte> buffer = new List<byte>();

        internal Sg3FrameDecoder(Sg3Framing value) : this(value, 65536) { }
        internal Sg3FrameDecoder(Sg3Framing value, int maximum)
        {
            if (maximum < 1) throw new ArgumentOutOfRangeException("maximum");
            framing = value;
            maximumBytes = maximum;
        }
        internal int PendingBytes { get { return buffer.Count; } }

        internal IList<byte[]> Push(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");
            buffer.AddRange(bytes);
            var frames = new List<byte[]>();
            if (framing == Sg3Framing.Plain) DecodePlain(frames);
            else DecodeB32(frames);
            return frames;
        }

        private void DecodePlain(List<byte[]> frames)
        {
            int terminator;
            while ((terminator = buffer.IndexOf(0x14)) >= 0)
            {
                int length = terminator + 1;
                if (length > maximumBytes) throw new InvalidDataException("Frame SG3 excede o limite configurado.");
                frames.Add(buffer.GetRange(0, length).ToArray());
                buffer.RemoveRange(0, length);
            }
            if (buffer.Count > maximumBytes) throw new InvalidDataException("Frame SG3 excede o limite configurado.");
        }

        private void DecodeB32(List<byte[]> frames)
        {
            while (buffer.Count >= 4)
            {
                int total = 0;
                for (int index = 0; index < 4; index++)
                {
                    byte digit = buffer[index];
                    if (digit < 0x30 || digit > 0x39) throw new InvalidDataException("Cabecalho B32 invalido.");
                    total = (total * 10) + digit - 0x30;
                }
                if (total < 5 || total - 4 > maximumBytes) throw new InvalidDataException("Tamanho B32 invalido.");
                if (buffer.Count < total) return;
                byte[] payload = buffer.GetRange(4, total - 4).ToArray();
                if (payload[payload.Length - 1] != 0x14) throw new InvalidDataException("Frame B32 sem terminador DC4.");
                frames.Add(payload);
                buffer.RemoveRange(0, total);
            }
        }
    }

    internal sealed class Sg3CaptureResult
    {
        internal int Frames { get; private set; }
        internal long Bytes { get; private set; }
        internal Sg3CaptureResult(int frames, long bytes) { Frames = frames; Bytes = bytes; }
    }

    internal sealed class Sg3CaptureClient
    {
        private readonly string inbox;
        internal Sg3CaptureClient(string dataFile) { inbox = ReceiverProgram.Inbox(dataFile); }

        internal Sg3CaptureResult Run(IPAddress address, int port, Sg3Framing framing, TimeSpan duration)
        {
            if (address == null) throw new ArgumentNullException("address");
            if (port < 1 || port > 65535) throw new ArgumentOutOfRangeException("port");
            if (duration < TimeSpan.FromSeconds(10) || duration > TimeSpan.FromMinutes(10)) throw new ArgumentOutOfRangeException("duration");
            Directory.CreateDirectory(inbox);
            using (var lockFile = new FileStream(Path.Combine(inbox, "receiver.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            using (var client = new TcpClient(address.AddressFamily))
            {
                IAsyncResult pending = client.BeginConnect(address, port, null, null);
                if (!pending.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5))) { client.Close(); throw new TimeoutException("Timeout ao conectar no SG3."); }
                client.EndConnect(pending);
                client.ReceiveTimeout = 1000;
                client.SendTimeout = 3000;
                using (NetworkStream stream = client.GetStream())
                {
                    var decoder = new Sg3FrameDecoder(framing);
                    var raw = new RawJournal(inbox);
                    var captures = new CaptureJournal(inbox);
                    byte[] readBuffer = new byte[4096];
                    byte[] ack = Sg3Protocol.Ack(framing);
                    DateTime deadline = DateTime.UtcNow.Add(duration);
                    int frameCount = 0;
                    long byteCount = 0;
                    while (DateTime.UtcNow < deadline)
                    {
                        int read;
                        try { read = stream.Read(readBuffer, 0, readBuffer.Length); }
                        catch (IOException ex)
                        {
                            var socket = ex.InnerException as SocketException;
                            if (socket != null && socket.SocketErrorCode == SocketError.TimedOut) continue;
                            throw;
                        }
                        if (read == 0) break;
                        var chunk = new byte[read];
                        Buffer.BlockCopy(readBuffer, 0, chunk, 0, read);
                        foreach (byte[] frame in decoder.Push(chunk))
                        {
                            raw.Append(frame);
                            string capture = captures.Append("sg3-tcp", frame);
                            captures.SetState(capture, "captured");
                            stream.Write(ack, 0, ack.Length);
                            stream.Flush();
                            frameCount++;
                            byteCount += frame.Length;
                        }
                    }
                    if (decoder.PendingBytes != 0) throw new InvalidDataException("Conexao SG3 terminou com frame parcial.");
                    return new Sg3CaptureResult(frameCount, byteCount);
                }
            }
        }

        internal static Sg3Framing ParseFraming(string value)
        {
            if (String.Equals(value, "plain", StringComparison.OrdinalIgnoreCase)) return Sg3Framing.Plain;
            if (String.Equals(value, "b32", StringComparison.OrdinalIgnoreCase)) return Sg3Framing.B32;
            throw new ArgumentException("Framing deve ser plain ou b32.");
        }
    }
}

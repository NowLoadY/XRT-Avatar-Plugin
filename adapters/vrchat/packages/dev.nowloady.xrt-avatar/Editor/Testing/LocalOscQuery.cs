using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XrtAvatarPlugin.Editor
{
    // Editor-only OSCQuery peer. All avatar access happens in Tick on Unity's main thread.
    internal sealed class LocalOscQuery : IDisposable
    {
        internal readonly struct Value
        {
            internal readonly char Type;
            internal readonly float Number;
            internal Value(char type, float number) { Type = type; Number = number; }
            internal bool IsBool => Type == 'T' || Type == 'F';
            internal string Json => IsBool ? (Number != 0 ? "true" : "false") : Number.ToString("R", CultureInfo.InvariantCulture);
        }

        private sealed class Query
        {
            internal string Path;
            internal readonly TaskCompletionSource<string> Reply = new TaskCompletionSource<string>();
        }

        private readonly Func<Dictionary<string, Value>> snapshot;
        private readonly Action<string, Value> write;
        private readonly ConcurrentQueue<Query> queries = new ConcurrentQueue<Query>();
        private readonly string identity = "unity-" + Guid.NewGuid().ToString("N");
        private readonly string name = "VRChat-XRT-Unity-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        private UdpClient udp;
        private TcpListener http;
        private Thread worker;
        private LocalDiscovery discovery;
        private volatile bool stopped;
        private IPEndPoint receiver;
        private Dictionary<string, Value> previous;
        internal int Writes { get; private set; }
        internal int Snapshots { get; private set; }
        internal float Read(string parameter) => previous != null && previous.TryGetValue(parameter, out var value)
            ? value.Number : 0;
        internal int Port => ((IPEndPoint)udp.Client.LocalEndPoint).Port;
        internal string Endpoint => "127.0.0.1:" + ((IPEndPoint)http.LocalEndpoint).Port;

        internal LocalOscQuery(Func<Dictionary<string, Value>> snapshot, Action<string, Value> write)
        {
            this.snapshot = snapshot;
            this.write = write;
            try
            {
                udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
                http = new TcpListener(IPAddress.Loopback, 0);
                http.Start(4);
                discovery = new LocalDiscovery(name, ((IPEndPoint)http.LocalEndpoint).Port);
                worker = new Thread(Serve) { IsBackground = true, Name = "XRT Unity OSCQuery" };
                worker.Start();
            }
            catch { Dispose(); throw; }
        }

        internal void Tick()
        {
            // Drain writes before answering fresh queries, including the host's reset confirmation.
            for (int count = 0; count < 64 && udp.Available > 0; count++)
            {
                IPEndPoint peer = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes = udp.Receive(ref peer);
                if (!IPAddress.IsLoopback(peer.Address) || bytes.Length > 16384) continue;
                try
                {
                    ReadPacket(bytes, 0, bytes.Length, 0, (path, value) =>
                    {
                        if (!path.StartsWith("/avatar/parameters/XRT/", StringComparison.Ordinal)) return;
                        write(path.Substring("/avatar/parameters/".Length), value);
                        receiver = peer;
                        Writes++;
                    });
                }
                catch (InvalidDataException) { /* Ignore malformed datagrams without losing the test. */ }
            }
            var values = snapshot();
            if (receiver != null)
                foreach (var pair in values)
                    if (previous == null || !previous.TryGetValue(pair.Key, out var old) ||
                        old.Type != pair.Value.Type || old.Number != pair.Value.Number)
                    {
                        byte[] packet = Encode("/avatar/parameters/" + pair.Key, pair.Value);
                        udp.Send(packet, packet.Length, receiver);
                    }
            previous = values;
            for (int count = 0; count < 8 && queries.TryDequeue(out var query); count++)
            {
                string body = query.Path == "/?HOST_INFO"
                    ? "{\"NAME\":" + Quote(name) + ",\"OSC_IP\":\"127.0.0.1\",\"OSC_PORT\":" + Port +
                      ",\"OSC_TRANSPORT\":\"UDP\",\"EXTENSIONS\":{\"ACCESS\":true,\"VALUE\":true}}"
                    : Tree(values, query.Path);
                if (query.Path == "/") Snapshots++;
                query.Reply.TrySetResult(body);
            }
            discovery.Tick();
        }

        private void Serve()
        {
            while (!stopped)
            {
                try
                {
                    using (var client = http.AcceptTcpClient())
                    {
                        client.ReceiveTimeout = client.SendTimeout = 500;
                        using (var stream = client.GetStream())
                        {
                            var request = new StringBuilder();
                            int next;
                            while (request.Length < 4096 && (next = stream.ReadByte()) >= 0)
                            {
                                request.Append((char)next);
                                if (request.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal)) break;
                            }
                            string[] first = request.ToString().Split('\n')[0].Trim().Split(' ');
                            int code = 405;
                            string body = "";
                            if (first.Length >= 2 && first[0] == "GET")
                            {
                                var query = new Query { Path = first[1] };
                                if (queries.Count < 8)
                                {
                                    queries.Enqueue(query);
                                    bool answered = query.Reply.Task.Wait(900);
                                    body = answered ? query.Reply.Task.Result : null;
                                    code = !answered ? 503 : body == null ? 404 : 200;
                                }
                                else code = 503;
                            }
                            byte[] payload = Encoding.UTF8.GetBytes(body ?? "");
                            byte[] header = Encoding.ASCII.GetBytes("HTTP/1.1 " + code + " " +
                                (code == 200 ? "OK" : code == 404 ? "Not Found" : "Unavailable") +
                                "\r\nContent-Type: application/json\r\nContent-Length: " + payload.Length +
                                "\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n");
                            stream.Write(header, 0, header.Length);
                            stream.Write(payload, 0, payload.Length);
                        }
                    }
                }
                catch (IOException) { }
                catch (SocketException) { if (stopped) return; }
                catch (ObjectDisposedException) { return; }
            }
        }

        private sealed class Node
        {
            internal string Path, Data;
            internal readonly SortedDictionary<string, Node> Children = new SortedDictionary<string, Node>();
            internal string Json() => "{\"FULL_PATH\":" + Quote(Path) + (Data != null ? "," + Data :
                ",\"CONTENTS\":{" + string.Join(",", Children.Select(p => Quote(p.Key) + ":" + p.Value.Json())) + "}") + "}";
            internal Node Add(string path)
            {
                Node node = this;
                string full = "";
                foreach (string part in path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    full += "/" + part;
                    if (!node.Children.TryGetValue(part, out var child)) node.Children[part] = child = new Node { Path = full };
                    node = child;
                }
                return node;
            }
        }

        private string Tree(Dictionary<string, Value> values, string path)
        {
            var root = new Node { Path = "/" };
            root.Add("/avatar/change").Data = "\"TYPE\":\"s\",\"ACCESS\":1,\"VALUE\":[" + Quote(identity) + "]";
            foreach (var pair in values)
                root.Add("/avatar/parameters/" + pair.Key).Data = "\"TYPE\":" + Quote(pair.Value.Type.ToString()) +
                    ",\"ACCESS\":3,\"VALUE\":[" + pair.Value.Json + "]";
            Node selected = root;
            foreach (string part in path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
                if (!selected.Children.TryGetValue(part, out selected)) return null;
            return selected.Json();
        }

        private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        private static int Int(byte[] bytes, ref int offset, int end)
        {
            if (offset < 0 || offset + 4 > end) throw new InvalidDataException();
            int value = (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
            offset += 4;
            return value;
        }
        private static string String(byte[] bytes, ref int offset, int end)
        {
            int start = offset;
            while (offset < end && bytes[offset] != 0) offset++;
            if (offset >= end) throw new InvalidDataException();
            string value = Encoding.UTF8.GetString(bytes, start, offset - start);
            offset = (offset + 4) & ~3;
            if (offset > end) throw new InvalidDataException();
            return value;
        }
        private static void ReadPacket(byte[] bytes, int offset, int end, int depth, Action<string, Value> receive)
        {
            if (depth > 8) throw new InvalidDataException();
            string path = String(bytes, ref offset, end);
            if (path == "#bundle")
            {
                offset += 8;
                int count = 0;
                while (offset < end && count++ < 64)
                {
                    int size = Int(bytes, ref offset, end);
                    if (size <= 0 || size > end - offset) throw new InvalidDataException();
                    ReadPacket(bytes, offset, offset + size, depth + 1, receive);
                    offset += size;
                }
                return;
            }
            string types = String(bytes, ref offset, end);
            if (types.Length != 2 || types[0] != ',') return;
            char type = types[1];
            float number;
            if (type == 'T' || type == 'F') number = type == 'T' ? 1 : 0;
            else if (type == 'i') number = Int(bytes, ref offset, end);
            else if (type == 'f') number = BitConverter.ToSingle(BitConverter.GetBytes(Int(bytes, ref offset, end)), 0);
            else return;
            if (float.IsNaN(number) || float.IsInfinity(number)) throw new InvalidDataException();
            receive(path, new Value(type, number));
        }
        private static byte[] Encode(string path, Value value)
        {
            using (var stream = new MemoryStream())
            {
                foreach (string item in new[] { path, "," + value.Type })
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(item);
                    stream.Write(bytes, 0, bytes.Length);
                    do { stream.WriteByte(0); } while (stream.Length % 4 != 0);
                }
                if (!value.IsBool)
                {
                    int bits = value.Type == 'i' ? (int)value.Number : BitConverter.ToInt32(BitConverter.GetBytes(value.Number), 0);
                    for (int shift = 24; shift >= 0; shift -= 8) stream.WriteByte((byte)(bits >> shift));
                }
                return stream.ToArray();
            }
        }

        public void Dispose()
        {
            stopped = true;
            discovery?.Dispose();
            http?.Stop();
            while (queries.TryDequeue(out var query)) query.Reply.TrySetResult(null);
            udp?.Close();
            worker?.Join(100);
        }
    }

    // Minimal DNS-SD announcement for the editor's one loopback OSCQuery endpoint.
    internal sealed class LocalDiscovery : IDisposable
    {
        private const string Service = "_oscjson._tcp.local";
        private static readonly IPEndPoint Group = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);
        private readonly UdpClient socket;
        private readonly string instance, host;
        private readonly int port;
        private readonly List<IPAddress> interfaces = new List<IPAddress>();
        private DateTime next;
        internal LocalDiscovery(string name, int port)
        {
            this.port = port;
            instance = name + "." + Service;
            host = name.ToLowerInvariant() + ".local";
            socket = new UdpClient(AddressFamily.InterNetwork);
            try
            {
                socket.ExclusiveAddressUse = false;
                socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.Client.Bind(new IPEndPoint(IPAddress.Any, 5353));
                socket.MulticastLoopback = true;
                foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
                    if (network.OperationalStatus == OperationalStatus.Up && network.SupportsMulticast)
                        foreach (var address in network.GetIPProperties().UnicastAddresses)
                            if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                                try { socket.JoinMulticastGroup(Group.Address, address.Address); interfaces.Add(address.Address); }
                                catch (SocketException) { }
                if (interfaces.Count == 0) { socket.JoinMulticastGroup(Group.Address); interfaces.Add(IPAddress.Any); }
                Announce(120);
            }
            catch { socket.Close(); throw; }
        }
        internal void Tick()
        {
            bool requested = false;
            for (int count = 0; count < 32 && socket.Available > 0; count++)
            {
                IPEndPoint peer = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes = socket.Receive(ref peer);
                if (bytes.Length > 12 && (bytes[2] & 0x80) == 0)
                {
                    string query = Encoding.ASCII.GetString(bytes);
                    requested |= query.Contains("_oscjson") || query.Contains(host.Split('.')[0]);
                }
            }
            if (DateTime.UtcNow >= next || requested && DateTime.UtcNow >= next.AddSeconds(-9)) Announce(120);
        }
        private void Announce(int ttl)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                Word(writer, 0); Word(writer, 0x8400); Word(writer, 0); Word(writer, 4); Word(writer, 0); Word(writer, 0);
                Record(writer, Service, 12, 1, ttl, data => Name(data, instance));
                Record(writer, instance, 33, 0x8001, ttl, data => { Word(data, 0); Word(data, 0); Word(data, port); Name(data, host); });
                Record(writer, instance, 16, 0x8001, ttl, data => data.Write((byte)0));
                Record(writer, host, 1, 0x8001, ttl, data => data.Write(IPAddress.Loopback.GetAddressBytes()));
                byte[] packet = stream.ToArray();
                foreach (var address in interfaces)
                    try
                    {
                        socket.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, address.GetAddressBytes());
                        socket.Send(packet, packet.Length, Group);
                    }
                    catch (SocketException) { }
            }
            next = DateTime.UtcNow.AddSeconds(10);
        }
        private static void Word(BinaryWriter writer, int value) { writer.Write((byte)(value >> 8)); writer.Write((byte)value); }
        private static void Name(BinaryWriter writer, string name)
        {
            foreach (string part in name.Split('.'))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(part);
                writer.Write((byte)bytes.Length); writer.Write(bytes);
            }
            writer.Write((byte)0);
        }
        private static void Record(BinaryWriter writer, string name, int type, int recordClass, int ttl, Action<BinaryWriter> write)
        {
            Name(writer, name); Word(writer, type); Word(writer, recordClass); Word(writer, ttl >> 16); Word(writer, ttl);
            using (var data = new MemoryStream())
            using (var body = new BinaryWriter(data))
            {
                write(body); Word(writer, (int)data.Length); writer.Write(data.ToArray());
            }
        }
        public void Dispose() { try { Announce(0); } finally { socket.Close(); } }
    }
}

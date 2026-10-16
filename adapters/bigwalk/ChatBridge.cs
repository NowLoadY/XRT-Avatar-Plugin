using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using BepInEx.Logging;

namespace XRT.BigWalk;

internal sealed class ChatBridge : IDisposable
{
    internal const int Port = 38647;
    internal const int MaxUtf16 = 256;
    private readonly object gate = new();
    private readonly ManualLogSource log;
    private readonly TcpListener listener = new(IPAddress.Loopback, Port);
    private readonly CancellationTokenSource lifetime = new();
    private readonly Dictionary<string, string> results = new();
    private readonly Queue<string> resultOrder = new();
    private NativeChat? native;
    private Connection? client;
    private Pending? pending;
    private IntPtr identity;
    private string session = Guid.NewGuid().ToString("N");
    private bool ready;
    private double nextSend;
    internal string? BindingError { get; set; }

    private static double Now => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
    internal ChatBridge(ManualLogSource log) => this.log = log;
    internal void Bind(NativeChat chat) => native = chat;

    internal void Start()
    {
        try
        {
            listener.Start();
            _ = Task.Run(Listen);
            log.LogInfo($"XRTranslate chat bridge listening on 127.0.0.1:{Port}.");
        }
        catch (SocketException exception) { log.LogError($"Cannot start chat bridge: {exception.Message}"); }
    }

    private async Task Listen()
    {
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                var socket = await listener.AcceptTcpClientAsync(lifetime.Token);
                lock (gate)
                {
                    if (client != null) { socket.Dispose(); continue; }
                    client = new Connection(socket);
                    _ = Serve(client);
                }
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or SocketException or ObjectDisposedException) { }
    }

    private async Task Serve(Connection connection)
    {
        var heartbeat = Heartbeat(connection);
        try
        {
            var buffer = new byte[1024];
            using var line = new MemoryStream();
            while (!connection.Token.IsCancellationRequested)
            {
                var count = await connection.Stream.ReadAsync(buffer, connection.Token);
                if (count == 0) break;
                for (var i = 0; i < count; i++)
                {
                    if (buffer[i] == '\n')
                    {
                        using var document = JsonDocument.Parse(line.ToArray(), new JsonDocumentOptions { MaxDepth = 8 });
                        line.SetLength(0);
                        await Handle(connection, document.RootElement);
                    }
                    else
                    {
                        if (line.Length >= 16384) throw new InvalidDataException("Frame too large.");
                        line.WriteByte(buffer[i]);
                    }
                }
            }
        }
        catch (Exception exception) when (exception is IOException or SocketException or OperationCanceledException or JsonException or InvalidOperationException or KeyNotFoundException or TimeoutException or ObjectDisposedException) { }
        finally
        {
            lock (gate)
            {
                connection.Dispose();
                if (client == connection) client = null;
                if (pending?.Connection == connection) Finish("not_ready");
            }
            await heartbeat;
        }
    }

    private async Task Heartbeat(Connection connection)
    {
        try
        {
            while (!connection.Token.IsCancellationRequested)
            {
                object state;
                lock (gate) state = new { type = "state", protocol = 1, ready, session, max_utf16 = MaxUtf16, error = BindingError };
                await connection.Write(state);
                await Task.Delay(1000, connection.Token);
            }
        }
        catch (Exception exception) when (exception is IOException or SocketException or OperationCanceledException or ObjectDisposedException) { connection.Dispose(); }
    }

    private async Task Handle(Connection connection, JsonElement frame)
    {
        if (frame.GetProperty("type").GetString() != "chat") throw new InvalidDataException("Unknown frame.");
        var id = frame.GetProperty("id").GetString();
        var text = frame.GetProperty("text").GetString();
        var requestSession = frame.GetProperty("session").GetString();
        if (string.IsNullOrEmpty(id) || id.Length > 80) throw new InvalidDataException("Invalid id.");
        Task<string> result;
        lock (gate)
        {
            if (results.TryGetValue(id, out var prior)) result = Task.FromResult(prior);
            else if (string.IsNullOrWhiteSpace(text) || text.Length > MaxUtf16)
                result = Task.FromResult(Remember(id, "too_long"));
            else if (!ready || requestSession != session)
                result = Task.FromResult(Remember(id, "not_ready"));
            else if (pending != null || Now < nextSend)
                result = Task.FromResult(Remember(id, "busy"));
            else
            {
                pending = new Pending(connection, id, text, session, Now);
                result = pending.Completion.Task;
            }
        }
        var status = await result.WaitAsync(TimeSpan.FromSeconds(2), connection.Token);
        await connection.Write(new { type = "result", id, status });
    }

    // Mirror invokes this on Unity's main thread, including while joining/leaving.
    internal void Tick()
    {
        lock (gate)
        {
            if (lifetime.IsCancellationRequested || native == null) return;
            try
            {
                var (player, currentIdentity) = native.LocalPlayer();
                BindingError = null;
                if (identity != currentIdentity)
                {
                    identity = currentIdentity;
                    session = Guid.NewGuid().ToString("N");
                }
                ready = player != null;
                if (pending == null) return;
                if (!ready || pending.Session != session || pending.Connection.IsDisconnected || Now - pending.Created > 2)
                    Finish("not_ready");
                else if (Now < nextSend) Finish("busy");
                else
                {
                    // Reserve the interval before invoking: an exception may occur after dispatch.
                    nextSend = Now + 2;
                    native.Send(player!, pending.Text);
                    Finish("sent");
                }
            }
            catch (Exception exception)
            {
                if (ready || identity != IntPtr.Zero)
                {
                    identity = IntPtr.Zero;
                    session = Guid.NewGuid().ToString("N");
                }
                ready = false;
                if (BindingError == null) log.LogWarning($"Native chat unavailable: {exception.GetBaseException().Message}");
                BindingError = "The game's chat interface is unavailable.";
                Finish("failed");
            }
        }
    }

    private void Finish(string status)
    {
        if (pending == null) return;
        pending.Completion.TrySetResult(Remember(pending.Id, status));
        pending = null;
    }

    private string Remember(string id, string status)
    {
        if (results.TryAdd(id, status)) resultOrder.Enqueue(id);
        while (resultOrder.Count > 256) results.Remove(resultOrder.Dequeue());
        return status;
    }

    public void Dispose()
    {
        lifetime.Cancel();
        listener.Stop();
        lock (gate) { ready = false; client?.Dispose(); Finish("not_ready"); }
    }

    private sealed record Pending(Connection Connection, string Id, string Text, string Session, double Created)
    {
        internal TaskCompletionSource<string> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class Connection : IDisposable
    {
        private readonly TcpClient socket;
        private readonly CancellationTokenSource stopped = new();
        private readonly SemaphoreSlim writeGate = new(1);
        internal NetworkStream Stream { get; }
        internal CancellationToken Token => stopped.Token;
        internal bool IsDisconnected
        {
            get
            {
                if (Token.IsCancellationRequested) return true;
                try { return socket.Client.Poll(0, SelectMode.SelectRead) && socket.Available == 0; }
                catch (Exception exception) when (exception is SocketException or ObjectDisposedException) { return true; }
            }
        }
        internal Connection(TcpClient socket) { this.socket = socket; socket.NoDelay = true; Stream = socket.GetStream(); }
        internal async Task Write(object value)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value) + "\n");
            await writeGate.WaitAsync(Token);
            try { await Stream.WriteAsync(bytes, Token); }
            finally { writeGate.Release(); }
        }
        public void Dispose() { stopped.Cancel(); socket.Dispose(); }
    }
}

using System.IO.Pipes;
using Xapper.Protocol;

namespace Xapper.McpServer.Ipc;

public sealed class InspectorClient : IAsyncDisposable
{
    private readonly string _pipeName;
    private NamedPipeClientStream? _pipe;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

    public InspectorClient(int processId)
    {
        _pipeName = IpcPipeNames.ForProcess(processId);
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await _pipe.ConnectAsync((int)ConnectTimeout.TotalMilliseconds, ct);
    }

    public bool IsConnected => _pipe?.IsConnected ?? false;

    public async Task<IpcMessage> SendAsync(IpcMessage request, CancellationToken ct = default)
    {
        if (_pipe == null || !_pipe.IsConnected)
            throw new InvalidOperationException("Not connected to inspector");

        await _sendLock.WaitAsync(ct);
        try
        {
            var bytes = IpcSerializer.Serialize(request);
            await _pipe.WriteAsync(bytes, ct);
            await _pipe.FlushAsync(ct);

            var response = await IpcSerializer.DeserializeAsync(_pipe, ct);
            return response ?? throw new InvalidOperationException("Received null response");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task<IpcMessage> PingAsync(CancellationToken ct = default)
    {
        var request = IpcSerializer.CreateRequest("ping");
        return await SendAsync(request, ct);
    }

    public async Task<IpcMessage> SnapshotAsync(int? rootRef = null, int maxDepth = 5, CancellationToken ct = default)
    {
        var payload = new { rootRef, maxDepth };
        var request = IpcSerializer.CreateRequest("snapshot", payload);
        return await SendAsync(request, ct);
    }

    public async Task<IpcMessage> ClickAsync(int @ref, int timeout = 5000, CancellationToken ct = default)
    {
        var payload = new { @ref, timeout };
        var request = IpcSerializer.CreateRequest("click", payload);
        return await SendAsync(request, ct);
    }

    public async Task<IpcMessage> TypeAsync(int @ref, string text, bool clear = true, int timeout = 5000, CancellationToken ct = default)
    {
        var payload = new { @ref, text, clear, timeout };
        var request = IpcSerializer.CreateRequest("type", payload);
        return await SendAsync(request, ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_pipe != null)
        {
            await _pipe.DisposeAsync();
            _pipe = null;
        }
        _sendLock.Dispose();
    }
}

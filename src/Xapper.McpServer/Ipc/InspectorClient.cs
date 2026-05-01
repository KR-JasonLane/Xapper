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

    public async Task<IpcMessage> SelectAsync(int @ref, string? itemText = null, int? itemIndex = null, int timeout = 5000, CancellationToken ct = default)
    {
        var payload = new { @ref, itemText, itemIndex, timeout };
        var request = IpcSerializer.CreateRequest("select", payload);
        return await SendAsync(request, ct);
    }

    public async Task<IpcMessage> ToggleAsync(int @ref, int timeout = 5000, CancellationToken ct = default)
    {
        var payload = new { @ref, timeout };
        var request = IpcSerializer.CreateRequest("toggle", payload);
        return await SendAsync(request, ct);
    }

    public async Task<IpcMessage> ExpandAsync(int @ref, bool expand = true, int timeout = 5000, CancellationToken ct = default)
    {
        var payload = new { @ref, expand, timeout };
        var request = IpcSerializer.CreateRequest("expand", payload);
        return await SendAsync(request, ct);
    }

    public async Task<IpcMessage> ScrollAsync(int @ref, double horizontalPercent = -1, double verticalPercent = -1, int timeout = 5000, CancellationToken ct = default)
    {
        var payload = new { @ref, horizontalPercent, verticalPercent, timeout };
        var request = IpcSerializer.CreateRequest("scroll", payload);
        return await SendAsync(request, ct);
    }

    public async Task<IpcMessage> GetPropertyAsync(int @ref, string propertyName, CancellationToken ct = default)
    {
        var payload = new { @ref, propertyName };
        var request = IpcSerializer.CreateRequest("getProperty", payload);
        return await SendAsync(request, ct);
    }

    public async Task<IpcMessage> GetBindingsAsync(int @ref, CancellationToken ct = default)
    {
        var payload = new { @ref };
        var request = IpcSerializer.CreateRequest("getBindings", payload);
        return await SendAsync(request, ct);
    }

    public async Task<IpcMessage> ScreenshotAsync(int? @ref = null, CancellationToken ct = default)
    {
        var payload = new { @ref };
        var request = IpcSerializer.CreateRequest("screenshot", payload);
        return await SendAsync(request, ct);
    }

    public async Task<IpcMessage> AssertAsync(int @ref, string property, string expected, CancellationToken ct = default)
    {
        var payload = new { @ref, property, expected };
        var request = IpcSerializer.CreateRequest("assert", payload);
        return await SendAsync(request, ct);
    }

    public async Task<IpcMessage> FindAsync(string? name = null, string? automationId = null, string? type = null, string? text = null, CancellationToken ct = default)
    {
        var payload = new { name, automationId, type, text };
        var request = IpcSerializer.CreateRequest("find", payload);
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

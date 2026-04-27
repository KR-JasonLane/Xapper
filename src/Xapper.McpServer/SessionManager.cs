using Xapper.McpServer.Ipc;

namespace Xapper.McpServer;

public sealed class SessionManager : IAsyncDisposable
{
    private readonly Dictionary<int, InspectorClient> _sessions = new();
    private int? _activeProcessId;

    public int? ActiveProcessId => _activeProcessId;

    public async Task<InspectorClient> AttachAsync(int processId, CancellationToken ct = default)
    {
        if (_sessions.ContainsKey(processId))
            throw new InvalidOperationException($"Already attached to process {processId}");

        var client = new InspectorClient(processId);
        await client.ConnectAsync(ct);

        _sessions[processId] = client;
        _activeProcessId = processId;
        return client;
    }

    public async Task DetachAsync(int? processId = null, CancellationToken ct = default)
    {
        var pid = processId ?? _activeProcessId
            ?? throw new InvalidOperationException("No active session");

        if (_sessions.TryGetValue(pid, out var client))
        {
            await client.DisposeAsync();
            _sessions.Remove(pid);
        }

        if (_activeProcessId == pid)
            _activeProcessId = _sessions.Keys.FirstOrDefault();
    }

    public InspectorClient GetActive()
    {
        if (_activeProcessId == null || !_sessions.TryGetValue(_activeProcessId.Value, out var client))
            throw new InvalidOperationException("No active session. Call xapper_attach first.");

        if (!client.IsConnected)
            throw new InvalidOperationException(
                $"Connection to process {_activeProcessId} lost. Detach and re-attach.");

        return client;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _sessions.Values)
            await client.DisposeAsync();
        _sessions.Clear();
        _activeProcessId = null;
    }
}

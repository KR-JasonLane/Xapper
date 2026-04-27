using System.Text.Json;
using Xapper.Protocol;
using Xapper.Protocol.Messages.Responses;

namespace Xapper.McpServer.Tools;

public sealed class ActionTools
{
    private readonly SessionManager _sessionManager;

    public ActionTools(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public async Task<string> ClickAsync(int @ref, int timeout = 5000, CancellationToken ct = default)
    {
        var client = _sessionManager.GetActive();
        var response = await client.ClickAsync(@ref, timeout, ct);

        if (response.Type == "error")
            return $"Error: {response.Payload}";

        var result = IpcSerializer.DeserializePayload<ActionResponse>(response.Payload!.Value);
        return result.Success ? result.Message ?? "Click succeeded" : $"Failed: {result.Error}";
    }

    public async Task<string> TypeAsync(int @ref, string text, bool clear = true, int timeout = 5000, CancellationToken ct = default)
    {
        var client = _sessionManager.GetActive();
        var response = await client.TypeAsync(@ref, text, clear, timeout, ct);

        if (response.Type == "error")
            return $"Error: {response.Payload}";

        var result = IpcSerializer.DeserializePayload<ActionResponse>(response.Payload!.Value);
        return result.Success ? result.Message ?? "Type succeeded" : $"Failed: {result.Error}";
    }
}

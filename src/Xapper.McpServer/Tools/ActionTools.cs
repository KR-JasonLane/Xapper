using System.ComponentModel;
using ModelContextProtocol.Server;
using Xapper.Protocol;
using Xapper.Protocol.Messages.Responses;

namespace Xapper.McpServer.Tools;

[McpServerToolType]
public sealed class ActionTools
{
    private readonly SessionManager _sessionManager;

    public ActionTools(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    [McpServerTool(Name = "xapper_click"), Description("Click a UI element by ref (uses AutomationPeer or RaiseEvent fallback)")]
    public async Task<string> Click(
        [Description("Element ref from last snapshot")] int @ref,
        [Description("Timeout in ms to wait for element readiness (default 5000)")] int timeout = 5000,
        CancellationToken ct = default)
    {
        var client = _sessionManager.GetActive();
        var response = await client.ClickAsync(@ref, timeout, ct);

        if (response.Type == "error")
            return $"Error: {response.Payload}";

        var result = IpcSerializer.DeserializePayload<ActionResponse>(response.Payload!.Value);
        return result.Success ? result.Message ?? "Click succeeded" : $"Failed: {result.Error}";
    }

    [McpServerTool(Name = "xapper_type"), Description("Type text into a TextBox or editable element by ref")]
    public async Task<string> Type(
        [Description("Element ref from last snapshot")] int @ref,
        [Description("Text to type into the element")] string text,
        [Description("If true, clears existing text first (default true)")] bool clear = true,
        [Description("Timeout in ms (default 5000)")] int timeout = 5000,
        CancellationToken ct = default)
    {
        var client = _sessionManager.GetActive();
        var response = await client.TypeAsync(@ref, text, clear, timeout, ct);

        if (response.Type == "error")
            return $"Error: {response.Payload}";

        var result = IpcSerializer.DeserializePayload<ActionResponse>(response.Payload!.Value);
        return result.Success ? result.Message ?? "Type succeeded" : $"Failed: {result.Error}";
    }
}

using System.ComponentModel;
using ModelContextProtocol.Server;
using Xapper.Protocol;
using Xapper.Protocol.Messages.Responses;

namespace Xapper.McpServer.Tools;

/// <summary>
/// 클릭, 텍스트 입력 등 기본 UI 액션 MCP 도구를 제공하는 클래스.
/// </summary>
[McpServerToolType]
public sealed class ActionTools
{
    private readonly SessionManager _sessionManager;

    public ActionTools(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    [McpServerTool(Name = "xapper_click"), Description("Click a UI element by ref. Optionally specify relative coordinates (0.0~1.0) for position-based click within the element.")]
    public async Task<string> Click(
        [Description("Element ref from last snapshot")] int @ref,
        [Description("Relative X position within element (0.0=left, 1.0=right). Omit for default click.")] double? x = null,
        [Description("Relative Y position within element (0.0=top, 1.0=bottom). Omit for default click.")] double? y = null,
        [Description("Timeout in ms to wait for element readiness (default 5000)")] int timeout = 5000,
        CancellationToken ct = default)
    {
        var client = _sessionManager.GetActive();
        var response = await client.ClickAsync(@ref, timeout, x, y, ct);

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

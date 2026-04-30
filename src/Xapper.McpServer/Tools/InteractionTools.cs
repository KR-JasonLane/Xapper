using System.ComponentModel;
using ModelContextProtocol.Server;
using Xapper.Protocol;
using Xapper.Protocol.Messages.Responses;

namespace Xapper.McpServer.Tools;

[McpServerToolType]
public sealed class InteractionTools
{
    private readonly SessionManager _sessionManager;

    public InteractionTools(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    [McpServerTool(Name = "xapper_select"), Description("Select an item in a ComboBox, ListBox, or TabControl")]
    public async Task<string> Select(
        [Description("Element ref from last snapshot")] int @ref,
        [Description("Item text to select (case-insensitive match)")] string? itemText = null,
        [Description("Item index to select (0-based)")] int? itemIndex = null,
        [Description("Timeout in ms (default 5000)")] int timeout = 5000,
        CancellationToken ct = default)
    {
        var client = _sessionManager.GetActive();
        var response = await client.SelectAsync(@ref, itemText, itemIndex, timeout, ct);
        return FormatResponse(response);
    }

    [McpServerTool(Name = "xapper_toggle"), Description("Toggle a CheckBox or ToggleButton")]
    public async Task<string> Toggle(
        [Description("Element ref from last snapshot")] int @ref,
        [Description("Timeout in ms (default 5000)")] int timeout = 5000,
        CancellationToken ct = default)
    {
        var client = _sessionManager.GetActive();
        var response = await client.ToggleAsync(@ref, timeout, ct);
        return FormatResponse(response);
    }

    [McpServerTool(Name = "xapper_expand"), Description("Expand or collapse a TreeViewItem or Expander")]
    public async Task<string> Expand(
        [Description("Element ref from last snapshot")] int @ref,
        [Description("True to expand, false to collapse (default true)")] bool expand = true,
        [Description("Timeout in ms (default 5000)")] int timeout = 5000,
        CancellationToken ct = default)
    {
        var client = _sessionManager.GetActive();
        var response = await client.ExpandAsync(@ref, expand, timeout, ct);
        return FormatResponse(response);
    }

    [McpServerTool(Name = "xapper_scroll"), Description("Scroll within a ScrollViewer")]
    public async Task<string> Scroll(
        [Description("Element ref from last snapshot")] int @ref,
        [Description("Horizontal scroll percent (0-100, -1 to leave unchanged)")] double horizontalPercent = -1,
        [Description("Vertical scroll percent (0-100, -1 to leave unchanged)")] double verticalPercent = -1,
        [Description("Timeout in ms (default 5000)")] int timeout = 5000,
        CancellationToken ct = default)
    {
        var client = _sessionManager.GetActive();
        var response = await client.ScrollAsync(@ref, horizontalPercent, verticalPercent, timeout, ct);
        return FormatResponse(response);
    }

    private static string FormatResponse(IpcMessage response)
    {
        if (response.Type == "error")
            return $"Error: {response.Payload}";
        var result = IpcSerializer.DeserializePayload<ActionResponse>(response.Payload!.Value);
        return result.Success ? result.Message ?? "Success" : $"Failed: {result.Error}";
    }
}

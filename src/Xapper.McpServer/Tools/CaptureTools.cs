using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Xapper.Protocol;
using Xapper.Protocol.Messages.Responses;

namespace Xapper.McpServer.Tools;

[McpServerToolType]
public sealed class CaptureTools
{
    private readonly SessionManager _sessionManager;

    public CaptureTools(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    [McpServerTool(Name = "xapper_screenshot"), Description("Capture a screenshot of the window or a specific element (returns base64 PNG)")]
    public async Task<string> Screenshot(
        [Description("Element ref to capture (omit for full window)")] int? @ref = null,
        CancellationToken ct = default)
    {
        var client = _sessionManager.GetActive();
        var response = await client.ScreenshotAsync(@ref, ct);

        if (response.Type == "error")
            return $"Error: {response.Payload}";

        var result = IpcSerializer.DeserializePayload<ScreenshotResponse>(response.Payload!.Value);
        return $"Screenshot captured: {result.Width}x{result.Height} pixels\n" +
               $"Base64 PNG ({result.Base64Png.Length} chars): {result.Base64Png[..Math.Min(100, result.Base64Png.Length)]}...";
    }
}

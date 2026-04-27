using System.Text;
using System.Text.Json;
using Xapper.Protocol;
using Xapper.Protocol.Messages.Responses;

namespace Xapper.McpServer.Tools;

public sealed class SnapshotTools
{
    private readonly SessionManager _sessionManager;

    public SnapshotTools(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public async Task<string> SnapshotAsync(int? rootRef = null, int maxDepth = 5, string format = "text", CancellationToken ct = default)
    {
        var client = _sessionManager.GetActive();
        var response = await client.SnapshotAsync(rootRef, maxDepth, ct);

        if (response.Type == "error")
            return $"Error: {response.Payload}";

        var snapshotResponse = IpcSerializer.DeserializePayload<SnapshotResponse>(response.Payload!.Value);

        return format == "json"
            ? JsonSerializer.Serialize(snapshotResponse, new JsonSerializerOptions { WriteIndented = true })
            : FormatAsText(snapshotResponse);
    }

    private static string FormatAsText(SnapshotResponse response)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Generation: {response.Generation}");
        sb.AppendLine();
        FormatElement(sb, response.Root, 0);
        return sb.ToString();
    }

    private static void FormatElement(StringBuilder sb, ElementSnapshot element, int indent)
    {
        var prefix = new string(' ', indent * 2);
        var parts = new List<string> { $"[ref={element.Ref}] {element.Type}" };

        if (!string.IsNullOrEmpty(element.Name))
            parts.Add($"name=\"{element.Name}\"");
        if (!string.IsNullOrEmpty(element.AutomationId))
            parts.Add($"id=\"{element.AutomationId}\"");
        if (!string.IsNullOrEmpty(element.Text))
            parts.Add($"text=\"{element.Text}\"");
        if (!element.IsEnabled)
            parts.Add("disabled");
        if (!element.IsVisible)
            parts.Add("hidden");

        sb.AppendLine($"{prefix}{string.Join(" ", parts)}");

        foreach (var child in element.Children)
        {
            FormatElement(sb, child, indent + 1);
        }
    }
}

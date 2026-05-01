using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using Xapper.Protocol;
using Xapper.Protocol.Messages.Responses;

namespace Xapper.McpServer.Tools;

[McpServerToolType]
public sealed class FindTools
{
    private readonly SessionManager _sessionManager;

    public FindTools(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    [McpServerTool(Name = "xapper_find"), Description("Find elements by name, automationId, type, or text content (returns matching refs)")]
    public async Task<string> Find(
        [Description("Element x:Name to search for (partial match)")] string? name = null,
        [Description("AutomationProperties.AutomationId to search for (partial match)")] string? automationId = null,
        [Description("Control type name to filter by (e.g., 'Button', 'TextBox')")] string? type = null,
        [Description("Text content to search for (partial match)")] string? text = null,
        CancellationToken ct = default)
    {
        if (name == null && automationId == null && type == null && text == null)
            return "Error: Provide at least one search criteria (name, automationId, type, or text)";

        var client = _sessionManager.GetActive();
        var response = await client.FindAsync(name, automationId, type, text, ct);

        if (response.Type == "error")
            return $"Error: {response.Payload}";

        var result = IpcSerializer.DeserializePayload<FindElementResponse>(response.Payload!.Value);

        if (result.Matches.Count == 0)
            return "No matching elements found.";

        var sb = new StringBuilder();
        sb.AppendLine($"Found {result.Matches.Count} match(es):");
        foreach (var match in result.Matches)
        {
            var parts = new List<string> { $"[ref={match.Ref}] {match.Type}" };
            if (!string.IsNullOrEmpty(match.Name)) parts.Add($"name=\"{match.Name}\"");
            if (!string.IsNullOrEmpty(match.AutomationId)) parts.Add($"id=\"{match.AutomationId}\"");
            if (!string.IsNullOrEmpty(match.Text)) parts.Add($"text=\"{match.Text}\"");
            sb.AppendLine($"  {string.Join(" ", parts)}");
        }
        return sb.ToString();
    }
}

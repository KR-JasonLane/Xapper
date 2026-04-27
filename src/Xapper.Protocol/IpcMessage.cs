using System.Text.Json;

namespace Xapper.Protocol;

public sealed class IpcMessage
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public required string Method { get; set; }
    public JsonElement? Payload { get; set; }
}

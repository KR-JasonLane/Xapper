namespace Xapper.Protocol.Messages.Responses;

public sealed class PongResponse
{
    public int ProcessId { get; set; }
    public string? ProcessName { get; set; }
}

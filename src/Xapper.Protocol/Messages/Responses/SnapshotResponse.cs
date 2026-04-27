namespace Xapper.Protocol.Messages.Responses;

public sealed class SnapshotResponse
{
    public int Generation { get; set; }
    public required ElementSnapshot Root { get; set; }
}

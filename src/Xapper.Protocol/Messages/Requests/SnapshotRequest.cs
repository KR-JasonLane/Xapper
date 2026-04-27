namespace Xapper.Protocol.Messages.Requests;

public sealed class SnapshotRequest
{
    public int? RootRef { get; set; }
    public int MaxDepth { get; set; } = 5;
}

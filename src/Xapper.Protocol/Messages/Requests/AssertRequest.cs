namespace Xapper.Protocol.Messages.Requests;

public sealed class AssertRequest
{
    public int Ref { get; set; }
    public required string Property { get; set; }
    public required string Expected { get; set; }
}

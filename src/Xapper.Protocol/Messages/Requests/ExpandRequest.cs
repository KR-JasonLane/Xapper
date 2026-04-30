namespace Xapper.Protocol.Messages.Requests;

public sealed class ExpandRequest
{
    public int Ref { get; set; }
    public bool Expand { get; set; } = true;
    public int Timeout { get; set; } = 5000;
}

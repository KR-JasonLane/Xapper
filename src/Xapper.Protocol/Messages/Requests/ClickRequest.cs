namespace Xapper.Protocol.Messages.Requests;

public sealed class ClickRequest
{
    public int Ref { get; set; }
    public int Timeout { get; set; } = 5000;
}

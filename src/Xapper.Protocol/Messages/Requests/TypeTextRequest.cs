namespace Xapper.Protocol.Messages.Requests;

public sealed class TypeTextRequest
{
    public int Ref { get; set; }
    public required string Text { get; set; }
    public bool Clear { get; set; } = true;
    public int Timeout { get; set; } = 5000;
}

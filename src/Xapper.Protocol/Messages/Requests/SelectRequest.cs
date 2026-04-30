namespace Xapper.Protocol.Messages.Requests;

public sealed class SelectRequest
{
    public int Ref { get; set; }
    public string? ItemText { get; set; }
    public int? ItemIndex { get; set; }
    public int Timeout { get; set; } = 5000;
}

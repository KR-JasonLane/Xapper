namespace Xapper.Protocol.Messages.Requests;

public sealed class ScrollRequest
{
    public int Ref { get; set; }
    public double HorizontalPercent { get; set; } = -1;
    public double VerticalPercent { get; set; } = -1;
    public int Timeout { get; set; } = 5000;
}

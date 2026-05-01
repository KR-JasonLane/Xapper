namespace Xapper.Protocol.Messages.Requests;

public sealed class FindElementRequest
{
    public string? Name { get; set; }
    public string? AutomationId { get; set; }
    public string? Type { get; set; }
    public string? Text { get; set; }
}

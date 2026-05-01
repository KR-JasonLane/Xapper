namespace Xapper.Protocol.Messages.Responses;

public sealed class FindElementResponse
{
    public List<ElementMatch> Matches { get; set; } = [];
}

public sealed class ElementMatch
{
    public int Ref { get; set; }
    public required string Type { get; set; }
    public string? Name { get; set; }
    public string? AutomationId { get; set; }
    public string? Text { get; set; }
}

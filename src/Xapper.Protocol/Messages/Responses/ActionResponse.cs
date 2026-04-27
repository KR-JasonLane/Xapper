namespace Xapper.Protocol.Messages.Responses;

public sealed class ActionResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

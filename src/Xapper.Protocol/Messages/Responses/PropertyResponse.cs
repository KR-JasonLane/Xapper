namespace Xapper.Protocol.Messages.Responses;

public sealed class PropertyResponse
{
    public int Ref { get; set; }
    public required string PropertyName { get; set; }
    public string? Value { get; set; }
    public string? ValueType { get; set; }
}

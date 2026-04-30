namespace Xapper.Protocol.Messages.Requests;

public sealed class GetPropertyRequest
{
    public int Ref { get; set; }
    public required string PropertyName { get; set; }
}

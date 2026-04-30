namespace Xapper.Protocol.Messages.Responses;

public sealed class BindingsResponse
{
    public int Ref { get; set; }
    public List<BindingInfo> Bindings { get; set; } = [];
}

public sealed class BindingInfo
{
    public required string PropertyName { get; set; }
    public string? Path { get; set; }
    public string? Source { get; set; }
    public string? Mode { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
}

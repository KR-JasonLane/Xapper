namespace Xapper.Protocol;

public sealed class ElementSnapshot
{
    public int Ref { get; set; }
    public required string Type { get; set; }
    public string? Name { get; set; }
    public string? AutomationId { get; set; }
    public string? Text { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsVisible { get; set; }
    public BoundingBox? Bounds { get; set; }
    public List<ElementSnapshot> Children { get; set; } = [];
}

public sealed class BoundingBox
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

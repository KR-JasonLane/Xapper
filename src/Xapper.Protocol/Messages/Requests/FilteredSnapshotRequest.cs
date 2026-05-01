namespace Xapper.Protocol.Messages.Requests;

public sealed class FilteredSnapshotRequest
{
    public int? RootRef { get; set; }
    public int MaxDepth { get; set; } = 5;
    public string? TypeFilter { get; set; }
    public string? NameFilter { get; set; }
    public bool VisibleOnly { get; set; } = false;
}

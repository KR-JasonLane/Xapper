namespace Xapper.Protocol.Messages.Requests;

/// <summary>
/// WPF 비주얼 트리 스냅샷을 요청하는 메시지.
/// </summary>
public sealed class SnapshotRequest
{
    /// <summary>스냅샷 시작 지점의 요소 참조. null이면 전체 윈도우 루트부터 탐색.</summary>
    public int? RootRef { get; set; }

    /// <summary>트리 탐색 최대 깊이. 기본값 5.</summary>
    public int MaxDepth { get; set; } = 5;
}

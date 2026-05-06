namespace Xapper.Protocol.Messages.Requests;

/// <summary>
/// 필터 조건이 적용된 비주얼 트리 스냅샷을 요청하는 메시지.
/// 현재 MCP 도구에서는 사용되지 않으며 향후 확장을 위해 존재.
/// </summary>
public sealed class FilteredSnapshotRequest
{
    /// <summary>스냅샷 시작 지점의 요소 참조. null이면 전체 윈도우 루트부터 탐색.</summary>
    public int? RootRef { get; set; }

    /// <summary>트리 탐색 최대 깊이. 기본값 5.</summary>
    public int MaxDepth { get; set; } = 5;

    /// <summary>포함할 컨트롤 타입명 필터 (부분 매칭).</summary>
    public string? TypeFilter { get; set; }

    /// <summary>포함할 요소 이름 필터 (부분 매칭).</summary>
    public string? NameFilter { get; set; }

    /// <summary>true이면 Visible 상태인 요소만 포함. 기본값 false.</summary>
    public bool VisibleOnly { get; set; } = false;
}

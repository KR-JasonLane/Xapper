namespace Xapper.Protocol.Messages.Responses;

/// <summary>
/// 비주얼 트리 스냅샷 응답. 트리 구조와 함께 세대 번호를 포함.
/// </summary>
public sealed class SnapshotResponse
{
    /// <summary>스냅샷 세대 번호. RefRegistry가 초기화될 때마다 증가하며, 요소 참조의 유효 범위를 나타냄.</summary>
    public int Generation { get; set; }

    /// <summary>비주얼 트리의 루트 요소 스냅샷.</summary>
    public required ElementSnapshot Root { get; set; }
}

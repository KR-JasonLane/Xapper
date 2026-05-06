namespace Xapper.Protocol.Messages.Requests;

/// <summary>
/// UI 요소의 특정 프로퍼티 값을 조회하는 요청 메시지.
/// DependencyProperty와 CLR 프로퍼티 모두 조회 가능.
/// </summary>
public sealed class GetPropertyRequest
{
    /// <summary>대상 요소의 참조 번호.</summary>
    public int Ref { get; set; }

    /// <summary>조회할 프로퍼티 이름 (예: "IsEnabled", "Text", "Visibility").</summary>
    public required string PropertyName { get; set; }
}

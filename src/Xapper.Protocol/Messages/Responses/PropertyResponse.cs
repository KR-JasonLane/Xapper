namespace Xapper.Protocol.Messages.Responses;

/// <summary>
/// 프로퍼티 조회 결과 응답.
/// </summary>
public sealed class PropertyResponse
{
    /// <summary>조회된 요소의 참조 번호.</summary>
    public int Ref { get; set; }

    /// <summary>조회한 프로퍼티 이름.</summary>
    public required string PropertyName { get; set; }

    /// <summary>프로퍼티 값의 문자열 표현.</summary>
    public string? Value { get; set; }

    /// <summary>프로퍼티 값의 타입명.</summary>
    public string? ValueType { get; set; }
}

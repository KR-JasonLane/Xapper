namespace Xapper.Protocol.Messages.Responses;

/// <summary>
/// UI 요소에 설정된 데이터 바인딩 목록 응답.
/// </summary>
public sealed class BindingsResponse
{
    /// <summary>조회된 요소의 참조 번호.</summary>
    public int Ref { get; set; }

    /// <summary>요소에 설정된 바인딩 정보 목록.</summary>
    public List<BindingInfo> Bindings { get; set; } = [];
}

/// <summary>
/// 단일 데이터 바인딩의 상세 정보.
/// </summary>
public sealed class BindingInfo
{
    /// <summary>바인딩이 설정된 DependencyProperty 이름.</summary>
    public required string PropertyName { get; set; }

    /// <summary>바인딩 경로 (Binding.Path).</summary>
    public string? Path { get; set; }

    /// <summary>바인딩 소스 타입명.</summary>
    public string? Source { get; set; }

    /// <summary>바인딩 모드 (OneWay, TwoWay 등).</summary>
    public string? Mode { get; set; }

    /// <summary>바인딩 유효성 검사 에러 존재 여부.</summary>
    public bool HasError { get; set; }

    /// <summary>에러가 있을 경우 에러 메시지.</summary>
    public string? ErrorMessage { get; set; }
}

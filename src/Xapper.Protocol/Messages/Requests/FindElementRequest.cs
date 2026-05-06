namespace Xapper.Protocol.Messages.Requests;

/// <summary>
/// 비주얼 트리에서 조건에 맞는 UI 요소를 검색하는 요청 메시지.
/// 모든 조건은 부분 매칭이며 대소문자를 무시. 최소 하나의 조건이 필요.
/// </summary>
public sealed class FindElementRequest
{
    /// <summary>FrameworkElement.Name으로 검색 (부분 매칭, 대소문자 무시).</summary>
    public string? Name { get; set; }

    /// <summary>AutomationProperties.AutomationId로 검색 (부분 매칭, 대소문자 무시).</summary>
    public string? AutomationId { get; set; }

    /// <summary>컨트롤 타입명으로 검색 (부분 매칭, 대소문자 무시).</summary>
    public string? Type { get; set; }

    /// <summary>텍스트 콘텐츠로 검색 (부분 매칭, 대소문자 무시).</summary>
    public string? Text { get; set; }
}

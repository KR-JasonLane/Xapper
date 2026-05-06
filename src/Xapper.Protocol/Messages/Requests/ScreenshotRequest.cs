namespace Xapper.Protocol.Messages.Requests;

/// <summary>
/// 윈도우 또는 특정 UI 요소의 스크린샷 캡처를 요청하는 메시지.
/// </summary>
public sealed class ScreenshotRequest
{
    /// <summary>캡처할 요소의 참조 번호. null이면 전체 윈도우를 캡처.</summary>
    public int? Ref { get; set; }
}

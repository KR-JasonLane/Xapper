namespace Xapper.Protocol.Messages.Requests;

/// <summary>
/// CheckBox, ToggleButton 등의 토글 상태를 전환하는 액션을 요청하는 메시지.
/// </summary>
public sealed class ToggleRequest
{
    /// <summary>토글할 대상 요소의 참조 번호.</summary>
    public int Ref { get; set; }

    /// <summary>요소가 준비될 때까지 대기하는 최대 시간 (밀리초). 기본값 5000ms.</summary>
    public int Timeout { get; set; } = 5000;
}

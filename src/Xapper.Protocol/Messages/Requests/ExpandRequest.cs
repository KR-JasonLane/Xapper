namespace Xapper.Protocol.Messages.Requests;

/// <summary>
/// TreeViewItem, Expander 등의 확장/축소 상태를 변경하는 액션을 요청하는 메시지.
/// </summary>
public sealed class ExpandRequest
{
    /// <summary>대상 요소의 참조 번호.</summary>
    public int Ref { get; set; }

    /// <summary>true이면 확장, false이면 축소. 기본값 true.</summary>
    public bool Expand { get; set; } = true;

    /// <summary>요소가 준비될 때까지 대기하는 최대 시간 (밀리초). 기본값 5000ms.</summary>
    public int Timeout { get; set; } = 5000;
}

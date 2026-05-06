namespace Xapper.Protocol.Messages.Responses;

/// <summary>
/// 핑 요청에 대한 응답. Inspector가 주입된 프로세스의 정보를 반환.
/// </summary>
public sealed class PongResponse
{
    /// <summary>Inspector가 주입된 대상 프로세스의 PID.</summary>
    public int ProcessId { get; set; }

    /// <summary>대상 프로세스의 이름.</summary>
    public string? ProcessName { get; set; }
}

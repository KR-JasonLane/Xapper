namespace Xapper.Protocol;

/// <summary>
/// Named Pipe 이름 생성 규칙을 정의하는 유틸리티 클래스.
/// Inspector(서버)와 McpServer(클라이언트) 양쪽에서 동일한 파이프 이름을 사용해야 하므로 공유 계약으로 존재.
/// </summary>
public static class IpcPipeNames
{
    /// <summary>
    /// 프로세스 ID 기반의 Named Pipe 이름을 반환합니다.
    /// </summary>
    /// <param name="processId">대상 WPF 프로세스의 PID.</param>
    /// <returns>"xapper_{processId}" 형식의 파이프 이름.</returns>
    public static string ForProcess(int processId) => $"xapper_{processId}";
}

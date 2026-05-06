using System.Text.Json;

namespace Xapper.Protocol;

/// <summary>
/// IPC 통신의 기본 메시지 엔벨로프.
/// Inspector와 McpServer 간 Named Pipe를 통해 주고받는 모든 메시지의 공통 형식.
/// </summary>
public sealed class IpcMessage
{
    /// <summary>요청-응답 매칭을 위한 고유 식별자 (GUID).</summary>
    public required string Id { get; set; }

    /// <summary>메시지 유형. "request", "response", "error" 중 하나.</summary>
    public required string Type { get; set; }

    /// <summary>요청 메서드명 (예: "ping", "snapshot", "click"). 응답/에러 시 빈 문자열.</summary>
    public required string Method { get; set; }

    /// <summary>메서드별 요청/응답 데이터. null일 수 있음.</summary>
    public JsonElement? Payload { get; set; }
}

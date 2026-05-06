namespace Xapper.Protocol.Messages.Responses;

/// <summary>
/// click, type, select, toggle, expand, scroll 등 UI 액션의 공통 응답.
/// </summary>
public sealed class ActionResponse
{
    /// <summary>액션 성공 여부.</summary>
    public bool Success { get; set; }

    /// <summary>성공 시 부가 메시지.</summary>
    public string? Message { get; set; }

    /// <summary>실패 시 에러 메시지.</summary>
    public string? Error { get; set; }
}

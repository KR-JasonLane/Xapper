namespace Xapper.Protocol.Messages.Requests;

/// <summary>
/// UI 요소에 설정된 데이터 바인딩 정보를 조회하는 요청 메시지.
/// </summary>
public sealed class GetBindingsRequest
{
    /// <summary>바인딩을 조회할 대상 요소의 참조 번호.</summary>
    public int Ref { get; set; }
}

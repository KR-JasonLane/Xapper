#if !NET7_0_OR_GREATER

namespace Xapper.Protocol.Polyfills;

/// <summary>
/// .NET 6에서 Stream.ReadExactlyAsync를 사용하기 위한 폴리필 확장 메서드.
/// </summary>
internal static class StreamPolyfill
{
    /// <summary>
    /// 스트림에서 버퍼가 가득 찰 때까지 정확히 읽습니다.
    /// </summary>
    public static async Task ReadExactlyAsync(this Stream stream, byte[] buffer, CancellationToken ct = default)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, ct);
            if (read == 0)
                throw new EndOfStreamException("Unexpected end of stream.");
            offset += read;
        }
    }
}

#endif

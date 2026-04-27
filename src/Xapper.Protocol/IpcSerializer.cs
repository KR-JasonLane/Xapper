using System.Buffers;
using System.Text.Json;

namespace Xapper.Protocol;

public static class IpcSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static byte[] Serialize(IpcMessage message)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(message, Options);
        var result = new byte[4 + json.Length];
        BitConverter.TryWriteBytes(result.AsSpan(0, 4), json.Length);
        json.CopyTo(result, 4);
        return result;
    }

    public static async Task<IpcMessage?> DeserializeAsync(Stream stream, CancellationToken ct = default)
    {
        var lengthBuffer = new byte[4];
        await stream.ReadExactlyAsync(lengthBuffer, ct);
        var length = BitConverter.ToInt32(lengthBuffer);

        if (length <= 0 || length > 10 * 1024 * 1024)
            throw new InvalidOperationException($"Invalid message length: {length}");

        var jsonBuffer = new byte[length];
        await stream.ReadExactlyAsync(jsonBuffer, ct);

        return JsonSerializer.Deserialize<IpcMessage>(jsonBuffer, Options);
    }

    public static T DeserializePayload<T>(JsonElement payload)
    {
        return payload.Deserialize<T>(Options)
            ?? throw new InvalidOperationException($"Failed to deserialize payload as {typeof(T).Name}");
    }

    public static JsonElement SerializePayload<T>(T payload)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, Options);
        using var doc = JsonDocument.Parse(bytes);
        return doc.RootElement.Clone();
    }

    public static IpcMessage CreateRequest(string method, object? payload = null)
    {
        return new IpcMessage
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = "request",
            Method = method,
            Payload = payload != null ? SerializePayload(payload) : null
        };
    }

    public static IpcMessage CreateResponse(string id, object? payload = null)
    {
        return new IpcMessage
        {
            Id = id,
            Type = "response",
            Method = "",
            Payload = payload != null ? SerializePayload(payload) : null
        };
    }

    public static IpcMessage CreateError(string id, string error)
    {
        return new IpcMessage
        {
            Id = id,
            Type = "error",
            Method = "",
            Payload = SerializePayload(new { error })
        };
    }
}

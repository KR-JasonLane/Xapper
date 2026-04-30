namespace Xapper.Protocol.Messages.Responses;

public sealed class ScreenshotResponse
{
    public required string Base64Png { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

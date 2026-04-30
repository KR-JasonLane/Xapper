using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xapper.Protocol.Messages.Responses;

namespace Xapper.Inspector.Capture;

public static class ScreenshotCapture
{
    public static ScreenshotResponse CaptureElement(UIElement element)
    {
        var bounds = VisualTreeHelper.GetDescendantBounds(element);
        if (bounds.IsEmpty)
            throw new InvalidOperationException("Element has no renderable bounds");

        var dpi = VisualTreeHelper.GetDpi(element);
        var width = (int)Math.Ceiling(bounds.Width);
        var height = (int)Math.Ceiling(bounds.Height);

        var renderBitmap = new RenderTargetBitmap(
            (int)(width * dpi.DpiScaleX),
            (int)(height * dpi.DpiScaleY),
            dpi.PixelsPerInchX,
            dpi.PixelsPerInchY,
            PixelFormats.Pbgra32);

        renderBitmap.Render(element);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

        using var memoryStream = new MemoryStream();
        encoder.Save(memoryStream);

        return new ScreenshotResponse
        {
            Base64Png = Convert.ToBase64String(memoryStream.ToArray()),
            Width = width,
            Height = height
        };
    }

    public static ScreenshotResponse CaptureWindow(Window? window = null)
    {
        window ??= Application.Current.MainWindow;
        if (window == null)
            throw new InvalidOperationException("No window available to capture");

        return CaptureElement(window);
    }
}

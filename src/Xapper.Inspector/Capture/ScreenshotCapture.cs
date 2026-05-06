using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xapper.Protocol.Messages.Responses;

namespace Xapper.Inspector.Capture;

/// <summary>
/// WPF 윈도우 또는 개별 UI 요소를 RenderTargetBitmap으로 캡처하여 Base64 PNG로 변환하는 유틸리티 클래스.
/// </summary>
public static class ScreenshotCapture
{
    /// <summary>
    /// 지정된 UI 요소를 네이티브 DPI로 렌더링하여 Base64 PNG 스크린샷을 반환합니다.
    /// </summary>
    /// <param name="element">캡처할 UI 요소.</param>
    /// <returns>Base64 인코딩된 PNG 이미지와 크기 정보.</returns>
    /// <exception cref="InvalidOperationException">요소에 렌더링 가능한 영역이 없는 경우.</exception>
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

    /// <summary>
    /// 지정된 윈도우(또는 메인 윈도우)를 캡처합니다.
    /// </summary>
    /// <param name="window">캡처할 윈도우. null이면 Application.Current.MainWindow 사용.</param>
    /// <returns>Base64 인코딩된 PNG 이미지와 크기 정보.</returns>
    public static ScreenshotResponse CaptureWindow(Window? window = null)
    {
        window ??= Application.Current.MainWindow;
        if (window == null)
            throw new InvalidOperationException("No window available to capture");

        return CaptureElement(window);
    }
}

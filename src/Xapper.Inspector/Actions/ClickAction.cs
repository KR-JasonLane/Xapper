using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;

namespace Xapper.Inspector.Actions;

/// <summary>
/// UI 요소에 대한 클릭 액션을 수행하는 정적 클래스.
/// 좌표 미지정 시: AutomationPeer(IInvokeProvider/IToggleProvider) → ButtonBase.ClickEvent → 마우스 이벤트 시뮬레이션 순으로 시도.
/// 좌표 지정 시: PointToScreen → SendInput(MOUSEEVENTF_ABSOLUTE | VIRTUALDESK).
/// 좌표가 입력 이벤트에 원자적으로 포함되므로 타이밍 이슈 없음.
/// 듀얼 모니터, Per-Monitor DPI 환경에서 동작.
/// </summary>
public static class ClickAction
{
    #region Win32

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private const uint INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
    private const uint MOUSEEVENTF_MOVE = 0x0001;

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    #endregion

    #region Public Methods

    /// <summary>
    /// 지정된 UI 요소를 클릭합니다.
    /// </summary>
    /// <param name="element">클릭할 대상 요소.</param>
    /// <param name="relativeX">요소 내 상대 X 좌표 (0.0~1.0). null이면 기본 클릭.</param>
    /// <param name="relativeY">요소 내 상대 Y 좌표 (0.0~1.0). null이면 기본 클릭.</param>
    /// <exception cref="InvalidOperationException">요소가 UIElement가 아닌 경우.</exception>
    public static void Execute(DependencyObject element, double? relativeX = null, double? relativeY = null)
    {
        if (element is not UIElement uiElement)
            throw new InvalidOperationException($"Element {element.GetType().Name} is not a UIElement");

        // 좌표가 지정된 경우: SendInput(ABSOLUTE)으로 원자적 마우스 클릭
        if (relativeX.HasValue && relativeY.HasValue)
        {
            ClickAtPosition(uiElement, relativeX.Value, relativeY.Value);
            return;
        }

        // Priority 1: AutomationPeer
        var peer = UIElementAutomationPeer.CreatePeerForElement(uiElement);
        if (peer != null)
        {
            if (peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoker)
            {
                invoker.Invoke();
                return;
            }

            if (peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggler)
            {
                toggler.Toggle();
                return;
            }
        }

        // Priority 2: RaiseEvent fallback
        if (uiElement is ButtonBase button)
        {
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            return;
        }

        // Priority 3: 마우스 이벤트 시뮬레이션 (최후 수단)
        uiElement.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent
        });
        uiElement.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent
        });
        uiElement.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent
        });
        uiElement.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonUpEvent
        });
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 요소 내 상대 좌표를 가상 데스크톱 절대 좌표로 변환하여 SendInput으로 클릭합니다.
    /// MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK로 좌표가 입력 이벤트에 포함되므로
    /// SetCursorPos/SendInput 사이의 타이밍 이슈가 없음.
    /// </summary>
    private static void ClickAtPosition(UIElement element, double relativeX, double relativeY)
    {
        var size = element.RenderSize;
        var localPoint = new Point(size.Width * relativeX, size.Height * relativeY);

        // 1. WPF 요소 로컬 좌표 → 스크린 디바이스 좌표
        var screenPoint = element.PointToScreen(localPoint);

        // 2. 스크린 좌표 → 가상 데스크톱 절대 좌표 (0~65535 범위, 멀티 모니터 대응)
        var vx = GetSystemMetrics(SM_XVIRTUALSCREEN);
        var vy = GetSystemMetrics(SM_YVIRTUALSCREEN);
        var vw = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        var vh = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        var absoluteX = (int)(((screenPoint.X - vx) * 65535.0) / vw);
        var absoluteY = (int)(((screenPoint.Y - vy) * 65535.0) / vh);

        // 3. 대상 윈도우를 포그라운드로
        var window = Window.GetWindow(element);
        if (window is not null)
        {
            var hwndSource = PresentationSource.FromVisual(window) as HwndSource;
            if (hwndSource is not null)
                SetForegroundWindow(hwndSource.Handle);
        }

        // 4. 원자적 이동+클릭: 좌표가 이벤트에 포함됨
        var flags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK;

        var inputs = new INPUT[3];

        // 이동
        inputs[0].type = INPUT_MOUSE;
        inputs[0].mi.dx = absoluteX;
        inputs[0].mi.dy = absoluteY;
        inputs[0].mi.dwFlags = flags | MOUSEEVENTF_MOVE;

        // 클릭 다운
        inputs[1].type = INPUT_MOUSE;
        inputs[1].mi.dx = absoluteX;
        inputs[1].mi.dy = absoluteY;
        inputs[1].mi.dwFlags = flags | MOUSEEVENTF_LEFTDOWN;

        // 클릭 업
        inputs[2].type = INPUT_MOUSE;
        inputs[2].mi.dx = absoluteX;
        inputs[2].mi.dy = absoluteY;
        inputs[2].mi.dwFlags = flags | MOUSEEVENTF_LEFTUP;

        SendInput(3, inputs, Marshal.SizeOf<INPUT>());
    }

    #endregion
}

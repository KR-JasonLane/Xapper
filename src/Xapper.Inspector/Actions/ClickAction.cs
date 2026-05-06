using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Xapper.Inspector.Actions;

/// <summary>
/// UI 요소에 대한 클릭 액션을 수행하는 정적 클래스.
/// AutomationPeer(IInvokeProvider/IToggleProvider) → ButtonBase.ClickEvent → 마우스 이벤트 시뮬레이션 순으로 시도.
/// </summary>
public static class ClickAction
{
    /// <summary>
    /// 지정된 UI 요소를 클릭합니다.
    /// </summary>
    /// <param name="element">클릭할 대상 요소.</param>
    /// <exception cref="InvalidOperationException">요소가 UIElement가 아닌 경우.</exception>
    public static void Execute(DependencyObject element)
    {
        if (element is not UIElement uiElement)
            throw new InvalidOperationException($"Element {element.GetType().Name} is not a UIElement");

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
}

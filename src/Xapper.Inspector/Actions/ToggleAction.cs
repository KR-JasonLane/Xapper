using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;

namespace Xapper.Inspector.Actions;

/// <summary>
/// CheckBox, ToggleButton 등의 토글 상태를 전환하는 정적 클래스.
/// AutomationPeer(IToggleProvider) → ToggleButton 직접 조작 순으로 시도.
/// </summary>
public static class ToggleAction
{
    /// <summary>
    /// 지정된 UI 요소의 토글 상태를 전환합니다.
    /// </summary>
    /// <param name="element">대상 토글 요소.</param>
    /// <exception cref="InvalidOperationException">요소가 토글을 지원하지 않는 경우.</exception>
    public static void Execute(DependencyObject element)
    {
        if (element is not UIElement uiElement)
            throw new InvalidOperationException($"Element {element.GetType().Name} is not a UIElement");

        // Priority 1: AutomationPeer IToggleProvider
        var peer = UIElementAutomationPeer.CreatePeerForElement(uiElement);
        if (peer?.GetPattern(PatternInterface.Toggle) is IToggleProvider toggler)
        {
            toggler.Toggle();
            return;
        }

        // Priority 2: Direct ToggleButton manipulation
        if (uiElement is ToggleButton toggleButton)
        {
            toggleButton.IsChecked = !toggleButton.IsChecked;
            return;
        }

        throw new InvalidOperationException(
            $"Element {element.GetType().Name} does not support toggle");
    }
}

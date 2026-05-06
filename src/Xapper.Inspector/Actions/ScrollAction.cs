using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace Xapper.Inspector.Actions;

/// <summary>
/// ScrollViewer 내에서 스크롤 위치를 변경하는 정적 클래스.
/// AutomationPeer(IScrollProvider) → ScrollViewer 직접 조작 순으로 시도.
/// </summary>
public static class ScrollAction
{
    /// <summary>
    /// 지정된 UI 요소의 스크롤 위치를 변경합니다.
    /// </summary>
    /// <param name="element">대상 ScrollViewer 요소.</param>
    /// <param name="horizontalPercent">수평 스크롤 퍼센트 (0~100). -1이면 변경 안 함.</param>
    /// <param name="verticalPercent">수직 스크롤 퍼센트 (0~100). -1이면 변경 안 함.</param>
    /// <exception cref="InvalidOperationException">요소가 스크롤을 지원하지 않는 경우.</exception>
    public static void Execute(DependencyObject element, double horizontalPercent, double verticalPercent)
    {
        if (element is not UIElement uiElement)
            throw new InvalidOperationException($"Element {element.GetType().Name} is not a UIElement");

        // Priority 1: AutomationPeer IScrollProvider
        var peer = UIElementAutomationPeer.CreatePeerForElement(uiElement);
        if (peer?.GetPattern(PatternInterface.Scroll) is IScrollProvider scroller)
        {
            scroller.SetScrollPercent(horizontalPercent, verticalPercent);
            return;
        }

        // Priority 2: Direct ScrollViewer
        if (uiElement is ScrollViewer scrollViewer)
        {
            if (horizontalPercent >= 0)
                scrollViewer.ScrollToHorizontalOffset(
                    scrollViewer.ScrollableWidth * horizontalPercent / 100.0);
            if (verticalPercent >= 0)
                scrollViewer.ScrollToVerticalOffset(
                    scrollViewer.ScrollableHeight * verticalPercent / 100.0);
            return;
        }

        throw new InvalidOperationException(
            $"Element {element.GetType().Name} does not support scrolling");
    }
}

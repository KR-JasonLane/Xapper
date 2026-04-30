using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace Xapper.Inspector.Actions;

public static class ScrollAction
{
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

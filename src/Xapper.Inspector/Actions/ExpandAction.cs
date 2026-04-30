using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace Xapper.Inspector.Actions;

public static class ExpandAction
{
    public static void Execute(DependencyObject element, bool expand)
    {
        if (element is not UIElement uiElement)
            throw new InvalidOperationException($"Element {element.GetType().Name} is not a UIElement");

        // Priority 1: AutomationPeer IExpandCollapseProvider
        var peer = UIElementAutomationPeer.CreatePeerForElement(uiElement);
        if (peer?.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expander)
        {
            if (expand)
                expander.Expand();
            else
                expander.Collapse();
            return;
        }

        // Priority 2: Direct Expander control
        if (uiElement is Expander expanderControl)
        {
            expanderControl.IsExpanded = expand;
            return;
        }

        // Priority 3: TreeViewItem
        if (uiElement is TreeViewItem treeItem)
        {
            treeItem.IsExpanded = expand;
            return;
        }

        throw new InvalidOperationException(
            $"Element {element.GetType().Name} does not support expand/collapse");
    }
}

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Xapper.Inspector.Actions;

public static class SelectAction
{
    public static void Execute(DependencyObject element, string? itemText, int? itemIndex)
    {
        if (element is not UIElement uiElement)
            throw new InvalidOperationException($"Element {element.GetType().Name} is not a UIElement");

        // Try Selector-based controls (ComboBox, ListBox, TabControl)
        if (uiElement is Selector selector)
        {
            if (itemIndex.HasValue)
            {
                selector.SelectedIndex = itemIndex.Value;
                return;
            }

            if (itemText != null)
            {
                for (int i = 0; i < selector.Items.Count; i++)
                {
                    var item = selector.Items[i];
                    var text = item?.ToString() ?? "";
                    if (text.Equals(itemText, StringComparison.OrdinalIgnoreCase))
                    {
                        selector.SelectedIndex = i;
                        return;
                    }
                }
                throw new InvalidOperationException($"Item \"{itemText}\" not found in {element.GetType().Name}");
            }
        }

        // Try AutomationPeer ISelectionItemProvider
        var peer = UIElementAutomationPeer.CreatePeerForElement(uiElement);
        if (peer?.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selectionItem)
        {
            selectionItem.Select();
            return;
        }

        throw new InvalidOperationException(
            $"Element {element.GetType().Name} does not support selection. Provide a Selector control or element with SelectionItem pattern.");
    }
}

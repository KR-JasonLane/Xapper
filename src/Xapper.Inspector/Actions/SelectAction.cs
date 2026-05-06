using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Xapper.Inspector.Actions;

/// <summary>
/// ComboBox, ListBox, TabControl 등 Selector 계열 컨트롤에서 항목 선택을 수행하는 정적 클래스.
/// Selector 인덱스/텍스트 매칭 → AutomationPeer(ISelectionItemProvider) 순으로 시도.
/// </summary>
public static class SelectAction
{
    /// <summary>
    /// 지정된 Selector 컨트롤에서 항목을 선택합니다.
    /// </summary>
    /// <param name="element">대상 Selector 요소.</param>
    /// <param name="itemText">선택할 항목의 텍스트. 대소문자 무시 매칭.</param>
    /// <param name="itemIndex">선택할 항목의 0-based 인덱스.</param>
    /// <exception cref="InvalidOperationException">항목을 찾을 수 없거나 선택을 지원하지 않는 경우.</exception>
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

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace Xapper.Inspector.Actions;

public static class TypeAction
{
    public static void Execute(DependencyObject element, string text, bool clear)
    {
        if (element is not UIElement uiElement)
            throw new InvalidOperationException($"Element {element.GetType().Name} is not a UIElement");

        // Priority 1: AutomationPeer IValueProvider
        var peer = UIElementAutomationPeer.CreatePeerForElement(uiElement);
        if (peer != null)
        {
            if (peer.GetPattern(PatternInterface.Value) is IValueProvider valueProvider)
            {
                if (clear)
                    valueProvider.SetValue(text);
                else
                    valueProvider.SetValue(valueProvider.Value + text);
                return;
            }
        }

        // Priority 2: Direct TextBox manipulation
        if (uiElement is TextBox textBox)
        {
            textBox.Focus();
            if (clear)
                textBox.Text = text;
            else
                textBox.AppendText(text);
            textBox.CaretIndex = textBox.Text.Length;
            return;
        }

        throw new InvalidOperationException(
            $"Element {element.GetType().Name} does not support text input");
    }
}

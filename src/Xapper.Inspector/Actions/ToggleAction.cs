using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;

namespace Xapper.Inspector.Actions;

public static class ToggleAction
{
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

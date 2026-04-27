using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Xapper.Inspector.Actions;

public static class ClickAction
{
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

        // Generic mouse event simulation
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

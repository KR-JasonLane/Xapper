using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Xapper.Protocol;

namespace Xapper.Inspector.VisualTree;

public sealed class TreeWalker
{
    public ElementSnapshot Walk(DependencyObject root, RefRegistry registry, int maxDepth)
    {
        return WalkElement(root, registry, 0, maxDepth);
    }

    private ElementSnapshot WalkElement(DependencyObject element, RefRegistry registry, int depth, int maxDepth)
    {
        var refId = registry.Register(element);

        var snapshot = new ElementSnapshot
        {
            Ref = refId,
            Type = element.GetType().Name,
            Name = GetName(element),
            AutomationId = GetAutomationId(element),
            Text = GetText(element),
            IsEnabled = GetIsEnabled(element),
            IsVisible = GetIsVisible(element),
            Bounds = GetBounds(element)
        };

        if (depth < maxDepth)
        {
            var childCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                snapshot.Children.Add(WalkElement(child, registry, depth + 1, maxDepth));
            }
        }

        return snapshot;
    }

    private static string? GetName(DependencyObject element)
    {
        return element is FrameworkElement fe ? fe.Name : null;
    }

    private static string? GetAutomationId(DependencyObject element)
    {
        var id = AutomationProperties.GetAutomationId(element);
        return string.IsNullOrEmpty(id) ? null : id;
    }

    private static string? GetText(DependencyObject element)
    {
        return element switch
        {
            TextBlock tb => tb.Text,
            TextBox tb => tb.Text,
            ContentControl cc when cc.Content is string s => s,
            _ => null
        };
    }

    private static bool GetIsEnabled(DependencyObject element)
    {
        return element is UIElement ui && ui.IsEnabled;
    }

    private static bool GetIsVisible(DependencyObject element)
    {
        return element is UIElement ui && ui.Visibility == Visibility.Visible;
    }

    private static BoundingBox? GetBounds(DependencyObject element)
    {
        if (element is not UIElement ui)
            return null;

        var size = ui.RenderSize;
        if (size.Width == 0 && size.Height == 0)
            return null;

        try
        {
            var topLeft = ui.TranslatePoint(new Point(0, 0), Application.Current.MainWindow);
            return new BoundingBox
            {
                X = topLeft.X,
                Y = topLeft.Y,
                Width = size.Width,
                Height = size.Height
            };
        }
        catch
        {
            return null;
        }
    }
}

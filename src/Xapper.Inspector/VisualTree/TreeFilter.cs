using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using Xapper.Protocol;
using Xapper.Protocol.Messages.Requests;

namespace Xapper.Inspector.VisualTree;

public sealed class TreeFilter
{
    public ElementSnapshot WalkFiltered(DependencyObject root, RefRegistry registry, FilteredSnapshotRequest request)
    {
        return WalkElement(root, registry, 0, request);
    }

    private ElementSnapshot WalkElement(DependencyObject element, RefRegistry registry, int depth, FilteredSnapshotRequest request)
    {
        var refId = registry.Register(element);

        var snapshot = new ElementSnapshot
        {
            Ref = refId,
            Type = element.GetType().Name,
            Name = (element as FrameworkElement)?.Name,
            AutomationId = AutomationProperties.GetAutomationId(element),
            Text = GetText(element),
            IsEnabled = element is UIElement ui && ui.IsEnabled,
            IsVisible = element is UIElement uiVis && uiVis.Visibility == Visibility.Visible
        };

        if (depth < request.MaxDepth)
        {
            var childCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);

                if (ShouldInclude(child, request))
                {
                    snapshot.Children.Add(WalkElement(child, registry, depth + 1, request));
                }
            }
        }

        return snapshot;
    }

    private static bool ShouldInclude(DependencyObject element, FilteredSnapshotRequest request)
    {
        if (request.VisibleOnly && element is UIElement ui && ui.Visibility != Visibility.Visible)
            return false;

        if (request.TypeFilter != null)
        {
            if (!element.GetType().Name.Contains(request.TypeFilter, StringComparison.OrdinalIgnoreCase))
            {
                // Still include if any descendant matches
                return HasMatchingDescendant(element, request.TypeFilter);
            }
        }

        return true;
    }

    private static bool HasMatchingDescendant(DependencyObject element, string typeFilter)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(element, i);
            if (child.GetType().Name.Contains(typeFilter, StringComparison.OrdinalIgnoreCase))
                return true;
            if (HasMatchingDescendant(child, typeFilter))
                return true;
        }
        return false;
    }

    private static string? GetText(DependencyObject element)
    {
        return element switch
        {
            System.Windows.Controls.TextBlock tb => tb.Text,
            System.Windows.Controls.TextBox tb => tb.Text,
            System.Windows.Controls.ContentControl cc when cc.Content is string s => s,
            _ => null
        };
    }
}

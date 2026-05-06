using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using Xapper.Protocol;
using Xapper.Protocol.Messages.Requests;

namespace Xapper.Inspector.VisualTree;

/// <summary>
/// 필터 조건이 적용된 비주얼 트리 스냅샷을 생성하는 클래스.
/// 타입/이름 필터 및 가시성 필터를 지원하며, 매칭되는 자손이 있는 서브트리는 포함.
/// 현재 MCP 도구에서는 사용되지 않으며 향후 확장을 위해 존재.
/// </summary>
public sealed class TreeFilter
{
    /// <summary>
    /// 필터 조건에 따라 비주얼 트리를 순회하여 스냅샷을 생성합니다.
    /// </summary>
    /// <param name="root">탐색 시작 요소.</param>
    /// <param name="registry">요소 참조 레지스트리.</param>
    /// <param name="request">필터 조건 (TypeFilter, NameFilter, VisibleOnly, MaxDepth).</param>
    /// <returns>필터링된 트리 스냅샷.</returns>
    public ElementSnapshot WalkFiltered(DependencyObject root, RefRegistry registry, FilteredSnapshotRequest request)
    {
        return WalkElement(root, registry, 0, request);
    }

    #region Private Methods

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
                // 타입이 매칭되지 않더라도 자손 중 매칭되는 것이 있으면 포함
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

    #endregion
}

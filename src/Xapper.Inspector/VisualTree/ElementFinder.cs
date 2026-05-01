using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Xapper.Protocol.Messages.Requests;
using Xapper.Protocol.Messages.Responses;

namespace Xapper.Inspector.VisualTree;

public sealed class ElementFinder
{
    public FindElementResponse Find(DependencyObject root, FindElementRequest request, RefRegistry registry)
    {
        var response = new FindElementResponse();
        SearchTree(root, request, registry, response);
        return response;
    }

    private void SearchTree(DependencyObject element, FindElementRequest request, RefRegistry registry, FindElementResponse response)
    {
        if (Matches(element, request))
        {
            var refId = registry.Register(element);
            response.Matches.Add(new ElementMatch
            {
                Ref = refId,
                Type = element.GetType().Name,
                Name = (element as FrameworkElement)?.Name,
                AutomationId = AutomationProperties.GetAutomationId(element),
                Text = GetText(element)
            });
        }

        var childCount = VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < childCount; i++)
        {
            SearchTree(VisualTreeHelper.GetChild(element, i), request, registry, response);
        }
    }

    private static bool Matches(DependencyObject element, FindElementRequest request)
    {
        if (request.Type != null)
        {
            if (!element.GetType().Name.Equals(request.Type, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (request.Name != null)
        {
            var name = (element as FrameworkElement)?.Name;
            if (name == null || !name.Contains(request.Name, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (request.AutomationId != null)
        {
            var id = AutomationProperties.GetAutomationId(element);
            if (id == null || !id.Contains(request.AutomationId, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (request.Text != null)
        {
            var text = GetText(element);
            if (text == null || !text.Contains(request.Text, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
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
}

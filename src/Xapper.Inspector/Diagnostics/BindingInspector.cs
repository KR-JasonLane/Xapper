using System.Reflection;
using System.Windows;
using System.Windows.Data;
using Xapper.Protocol.Messages.Responses;

namespace Xapper.Inspector.Diagnostics;

public static class BindingInspector
{
    public static BindingsResponse GetBindings(DependencyObject element)
    {
        var response = new BindingsResponse { Ref = 0 };

        var localEnumerator = element.GetLocalValueEnumerator();
        while (localEnumerator.MoveNext())
        {
            var entry = localEnumerator.Current;

            if (entry.Value is BindingExpression bindingExpr)
            {
                response.Bindings.Add(CreateBindingInfo(entry.Property.Name, bindingExpr));
            }
        }

        // Also check common properties that might have bindings set via style/template
        var frameworkElement = element as FrameworkElement;
        if (frameworkElement != null)
        {
            CheckBinding(frameworkElement, FrameworkElement.DataContextProperty, response);
            CheckBinding(frameworkElement, UIElement.VisibilityProperty, response);
            CheckBinding(frameworkElement, UIElement.IsEnabledProperty, response);
        }

        return response;
    }

    private static void CheckBinding(FrameworkElement element, DependencyProperty dp, BindingsResponse response)
    {
        var binding = BindingOperations.GetBindingExpression(element, dp);
        if (binding != null && !response.Bindings.Exists(b => b.PropertyName == dp.Name))
        {
            response.Bindings.Add(CreateBindingInfo(dp.Name, binding));
        }
    }

    private static BindingInfo CreateBindingInfo(string propertyName, BindingExpression expr)
    {
        var binding = expr.ParentBinding;
        return new BindingInfo
        {
            PropertyName = propertyName,
            Path = binding.Path?.Path,
            Source = binding.Source?.GetType().Name ?? binding.ElementName ?? "(DataContext)",
            Mode = binding.Mode.ToString(),
            HasError = expr.HasError,
            ErrorMessage = expr.HasError ? expr.ValidationError?.ErrorContent?.ToString() : null
        };
    }
}

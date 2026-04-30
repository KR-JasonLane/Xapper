using System.ComponentModel;
using System.Reflection;
using System.Windows;
using Xapper.Protocol.Messages.Responses;

namespace Xapper.Inspector.Diagnostics;

public static class PropertyReader
{
    public static PropertyResponse ReadProperty(DependencyObject element, string propertyName)
    {
        // Try DependencyProperty first
        var dpField = element.GetType()
            .GetField($"{propertyName}Property", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        if (dpField?.GetValue(null) is DependencyProperty dp)
        {
            var value = element.GetValue(dp);
            return new PropertyResponse
            {
                Ref = 0,
                PropertyName = propertyName,
                Value = value?.ToString(),
                ValueType = value?.GetType().Name ?? "null"
            };
        }

        // Try CLR property via reflection
        var prop = element.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop != null)
        {
            var value = prop.GetValue(element);
            return new PropertyResponse
            {
                Ref = 0,
                PropertyName = propertyName,
                Value = value?.ToString(),
                ValueType = value?.GetType().Name ?? "null"
            };
        }

        throw new InvalidOperationException(
            $"Property \"{propertyName}\" not found on {element.GetType().Name}");
    }
}

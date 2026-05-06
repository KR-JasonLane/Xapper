using System.Reflection;
using System.Windows;
using System.Windows.Data;
using Xapper.Protocol.Messages.Responses;

namespace Xapper.Inspector.Diagnostics;

/// <summary>
/// UI 요소에 설정된 WPF 데이터 바인딩 정보를 수집하는 진단 유틸리티 클래스.
/// 로컬 값에서 바인딩을 열거하고, DataContext/Visibility/IsEnabled 등 공통 프로퍼티도 추가 확인.
/// </summary>
public static class BindingInspector
{
    /// <summary>
    /// 지정된 UI 요소의 모든 데이터 바인딩 정보를 수집합니다.
    /// </summary>
    /// <param name="element">바인딩을 조사할 대상 요소.</param>
    /// <returns>프로퍼티별 바인딩 경로, 소스, 모드, 에러 정보가 포함된 응답.</returns>
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

        // Style/Template을 통해 설정된 공통 프로퍼티의 바인딩도 확인
        var frameworkElement = element as FrameworkElement;
        if (frameworkElement != null)
        {
            CheckBinding(frameworkElement, FrameworkElement.DataContextProperty, response);
            CheckBinding(frameworkElement, UIElement.VisibilityProperty, response);
            CheckBinding(frameworkElement, UIElement.IsEnabledProperty, response);
        }

        return response;
    }

    /// <summary>
    /// 특정 DependencyProperty에 바인딩이 설정되어 있으면 응답에 추가합니다.
    /// </summary>
    private static void CheckBinding(FrameworkElement element, DependencyProperty dp, BindingsResponse response)
    {
        var binding = BindingOperations.GetBindingExpression(element, dp);
        if (binding != null && !response.Bindings.Exists(b => b.PropertyName == dp.Name))
        {
            response.Bindings.Add(CreateBindingInfo(dp.Name, binding));
        }
    }

    /// <summary>
    /// BindingExpression으로부터 BindingInfo 객체를 생성합니다.
    /// </summary>
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

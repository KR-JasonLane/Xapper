using System.Windows;

namespace Xapper.Inspector.AutoWait;

/// <summary>
/// UI 요소가 상호작용 가능한 상태(Visible, Enabled, Loaded)가 될 때까지 폴링 방식으로 대기하는 클래스.
/// 모든 액션 실행 전에 호출되어 UI 로딩 경합 조건을 방지.
/// </summary>
public sealed class ElementWaiter
{
    #region Fields

    private readonly TimeSpan _timeout;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

    #endregion

    #region Constructor

    /// <summary>
    /// <see cref="ElementWaiter"/>의 새 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="timeout">최대 대기 시간.</param>
    public ElementWaiter(TimeSpan timeout)
    {
        _timeout = timeout;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// UI 요소가 준비 상태가 될 때까지 50ms 간격으로 폴링합니다.
    /// FrameworkElement인 경우 IsVisible, IsEnabled, IsLoaded를 모두 확인.
    /// </summary>
    /// <param name="element">대기할 대상 요소.</param>
    /// <exception cref="TimeoutException">제한 시간 내에 요소가 준비되지 않은 경우.</exception>
    public async Task WaitForReady(DependencyObject element)
    {
        var deadline = DateTime.UtcNow + _timeout;

        while (DateTime.UtcNow < deadline)
        {
            var ready = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (element is FrameworkElement fe)
                    return fe.IsVisible && fe.IsEnabled && fe.IsLoaded;
                if (element is UIElement ui)
                    return ui.IsEnabled && ui.Visibility == Visibility.Visible;
                return true;
            });

            if (ready) return;
            await Task.Delay(PollInterval);
        }

        throw new TimeoutException(
            $"Element {element.GetType().Name} not ready within {_timeout.TotalMilliseconds}ms");
    }

    #endregion
}

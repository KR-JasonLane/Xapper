using System.Windows;

namespace Xapper.Inspector.AutoWait;

public sealed class ElementWaiter
{
    private readonly TimeSpan _timeout;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

    public ElementWaiter(TimeSpan timeout)
    {
        _timeout = timeout;
    }

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
}

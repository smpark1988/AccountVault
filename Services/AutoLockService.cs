using System.Windows;
using System.Windows.Threading;

namespace AccountVault.Services;

public sealed class AutoLockService : IDisposable
{
    private readonly DispatcherTimer _timer;
    private Window? _window;
    private TimeSpan _timeout;
    private DateTime _lastActivityUtc;
    private Action? _onTimeout;

    public AutoLockService()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _timer.Tick += OnTimerTick;
    }

    public void Start(Window window, TimeSpan timeout, Action onTimeout)
    {
        Stop();

        _window = window;
        _timeout = timeout;
        _onTimeout = onTimeout;
        _lastActivityUtc = DateTime.UtcNow;

        window.PreviewMouseMove += OnUserInput;
        window.PreviewMouseDown += OnUserInput;
        window.PreviewKeyDown += OnUserInput;
        window.PreviewMouseWheel += OnUserInput;
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();

        if (_window is not null)
        {
            _window.PreviewMouseMove -= OnUserInput;
            _window.PreviewMouseDown -= OnUserInput;
            _window.PreviewKeyDown -= OnUserInput;
            _window.PreviewMouseWheel -= OnUserInput;
        }

        _window = null;
        _onTimeout = null;
    }

    public void Dispose()
    {
        Stop();
        _timer.Tick -= OnTimerTick;
    }

    private void OnUserInput(object sender, EventArgs e)
    {
        _lastActivityUtc = DateTime.UtcNow;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_onTimeout is null)
        {
            return;
        }

        if (DateTime.UtcNow - _lastActivityUtc >= _timeout)
        {
            var timeoutAction = _onTimeout;
            Stop();
            timeoutAction.Invoke();
        }
    }
}

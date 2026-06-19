using System.Runtime.InteropServices;
using System.Windows;

namespace AccountVault.Services;

public sealed class ClipboardService : IDisposable
{
    private CancellationTokenSource? _clearPasswordCts;

    public void CopyText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        SetClipboardText(text);
    }

    public void CopyPassword(string password, TimeSpan clearAfter)
    {
        if (string.IsNullOrEmpty(password))
        {
            return;
        }

        SetClipboardText(password);
        SchedulePasswordClear(password, clearAfter);
    }

    public void Dispose()
    {
        _clearPasswordCts?.Cancel();
        _clearPasswordCts?.Dispose();
    }

    private static void SetClipboardText(string text)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => Clipboard.SetText(text, TextDataFormat.Text));
        }
        catch (ExternalException ex)
        {
            throw new InvalidOperationException("클립보드에 복사할 수 없습니다.", ex);
        }
    }

    // Clears the clipboard only if it still contains the password copied by this app.
    private void SchedulePasswordClear(string copiedPassword, TimeSpan delay)
    {
        _clearPasswordCts?.Cancel();
        _clearPasswordCts?.Dispose();

        var cts = new CancellationTokenSource();
        _clearPasswordCts = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, cts.Token);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (Clipboard.ContainsText(TextDataFormat.Text) &&
                            Clipboard.GetText(TextDataFormat.Text) == copiedPassword)
                        {
                            Clipboard.Clear();
                        }
                    }
                    catch (ExternalException)
                    {
                        // Clipboard can be temporarily owned by another process.
                    }
                });
            }
            catch (OperationCanceledException)
            {
            }
        }, cts.Token);
    }
}

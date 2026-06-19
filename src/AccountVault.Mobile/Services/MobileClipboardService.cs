namespace AccountVault.Mobile.Services;

public sealed class MobileClipboardService
{
    private CancellationTokenSource? _clearPasswordCts;

    public Task CopyTextAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Task.CompletedTask;
        }

        return Clipboard.SetTextAsync(text);
    }

    public async Task CopyPasswordAsync(string password, TimeSpan clearAfter)
    {
        if (string.IsNullOrEmpty(password))
        {
            return;
        }

        await Clipboard.SetTextAsync(password);
        SchedulePasswordClear(password, clearAfter);
    }

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
                var currentText = await Clipboard.GetTextAsync();
                if (currentText == copiedPassword)
                {
                    await Clipboard.SetTextAsync(string.Empty);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, cts.Token);
    }
}

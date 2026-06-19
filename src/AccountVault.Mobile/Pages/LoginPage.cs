using AccountVault.Services;

namespace AccountVault.Mobile.Pages;

public sealed class LoginPage : ContentPage
{
    private readonly VaultService _vaultService;
    private readonly Entry _passwordEntry;

    public LoginPage(IServiceProvider services)
    {
        _vaultService = services.GetRequiredService<VaultService>();
        Title = "로그인";

        _passwordEntry = new Entry { IsPassword = true, Placeholder = "마스터 비밀번호" };

        var unlockButton = new Button
        {
            Text = "열기",
            BackgroundColor = Color.FromArgb("#2563EB"),
            TextColor = Colors.White
        };
        unlockButton.Clicked += UnlockButton_Clicked;

        Content = MobileLayout.CreateAuthLayout(
            "AccountVault",
            "마스터 비밀번호로 보관함을 엽니다.",
            [_passwordEntry, unlockButton]);
    }

    private async void UnlockButton_Clicked(object? sender, EventArgs e)
    {
        try
        {
            var session = _vaultService.UnlockVault(_passwordEntry.Text ?? string.Empty);
            ((App)Application.Current!).SetSession(session);
        }
        catch (VaultException ex)
        {
            _passwordEntry.Text = string.Empty;
            await DisplayAlert("오류", ex.Message, "확인");
        }
    }
}

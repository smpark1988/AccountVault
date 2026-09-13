using AccountVault.Services;

namespace AccountVault.Mobile.Pages;

public sealed class SetupPage : ContentPage
{
    private readonly VaultService _vaultService;
    private readonly Entry _passwordEntry;
    private readonly Entry _confirmEntry;

    public SetupPage(IServiceProvider services)
    {
        _vaultService = services.GetRequiredService<VaultService>();
        Title = "초기 설정";

        _passwordEntry = new Entry { IsPassword = true, Placeholder = "마스터 비밀번호" };
        _confirmEntry = new Entry { IsPassword = true, Placeholder = "마스터 비밀번호 확인" };

        var createButton = new Button
        {
            Text = "보관함 생성",
            BackgroundColor = Color.FromArgb("#2563EB"),
            TextColor = Colors.White
        };
        createButton.Clicked += CreateButton_Clicked;

        Content = MobileLayout.CreateAuthLayout(
            "새 보관함 만들기",
            "계정 데이터는 이 장치 안에 암호화되어 저장됩니다.",
            [_passwordEntry, _confirmEntry, createButton]);
    }

    private async void CreateButton_Clicked(object? sender, EventArgs e)
    {
        var password = _passwordEntry.Text ?? string.Empty;
        var confirm = _confirmEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(password))
        {
            await DisplayAlert("확인", "마스터 비밀번호는 1자 이상으로 설정하세요.", "확인");
            return;
        }

        if (!string.Equals(password, confirm, StringComparison.Ordinal))
        {
            await DisplayAlert("확인", "마스터 비밀번호와 확인 입력값이 일치하지 않습니다.", "확인");
            return;
        }

        try
        {
            var session = _vaultService.CreateVault(password);
            ((App)Application.Current!).SetSession(session);
        }
        catch (VaultException ex)
        {
            await DisplayAlert("오류", ex.Message, "확인");
        }
    }
}

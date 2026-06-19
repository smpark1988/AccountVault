using AccountVault.Models;
using AccountVault.Services;

namespace AccountVault.Mobile.Pages;

public sealed class AccountEditPage : ContentPage
{
    private readonly VaultService _vaultService;
    private readonly string? _accountId;
    private readonly Entry _siteEntry;
    private readonly Entry _urlEntry;
    private readonly Entry _userIdEntry;
    private readonly Entry _passwordEntry;
    private readonly Editor _memoEditor;

    public AccountEditPage(IServiceProvider services, string? accountId)
    {
        _vaultService = services.GetRequiredService<VaultService>();
        _accountId = accountId;
        Title = accountId is null ? "계정 추가" : "계정 수정";

        var account = FindAccount();
        _siteEntry = new Entry { Placeholder = "사이트명", Text = account?.SiteName ?? string.Empty };
        _urlEntry = new Entry { Placeholder = "URL", Text = account?.Url ?? string.Empty };
        _userIdEntry = new Entry { Placeholder = "아이디", Text = account?.UserId ?? string.Empty };
        _passwordEntry = new Entry { Placeholder = "비밀번호", IsPassword = true, Text = account?.Password ?? string.Empty };
        _memoEditor = new Editor { Placeholder = "메모", Text = account?.Memo ?? string.Empty, AutoSize = EditorAutoSizeOption.TextChanges };

        var saveButton = new Button
        {
            Text = "저장",
            BackgroundColor = Color.FromArgb("#2563EB"),
            TextColor = Colors.White
        };
        saveButton.Clicked += SaveButton_Clicked;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 18,
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = Title,
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold
                    },
                    MobileLayout.CreateField("사이트명 *", _siteEntry),
                    MobileLayout.CreateField("URL", _urlEntry),
                    MobileLayout.CreateField("아이디", _userIdEntry),
                    MobileLayout.CreateField("비밀번호", _passwordEntry),
                    MobileLayout.CreateField("메모", _memoEditor),
                    saveButton
                }
            }
        };
    }

    private AccountItem? FindAccount()
    {
        return ((App)Application.Current!).Session?.Data?.Accounts.FirstOrDefault(account => account.Id == _accountId);
    }

    private async void SaveButton_Clicked(object? sender, EventArgs e)
    {
        var session = ((App)Application.Current!).Session;
        if (session?.Data is null)
        {
            ((App)Application.Current!).Lock();
            return;
        }

        if (string.IsNullOrWhiteSpace(_siteEntry.Text))
        {
            await DisplayAlert("확인", "사이트명은 필수입니다.", "확인");
            return;
        }

        if (string.IsNullOrWhiteSpace(_userIdEntry.Text) && string.IsNullOrWhiteSpace(_passwordEntry.Text))
        {
            await DisplayAlert("확인", "아이디 또는 비밀번호 중 하나 이상을 입력하세요.", "확인");
            return;
        }

        var now = DateTime.Now;
        var account = FindAccount();
        if (account is null)
        {
            account = new AccountItem
            {
                Id = Guid.NewGuid().ToString(),
                CreatedAt = now
            };
            session.Data.Accounts.Add(account);
        }

        account.SiteName = (_siteEntry.Text ?? string.Empty).Trim();
        account.Url = (_urlEntry.Text ?? string.Empty).Trim();
        account.UserId = (_userIdEntry.Text ?? string.Empty).Trim();
        account.Password = _passwordEntry.Text ?? string.Empty;
        account.Memo = (_memoEditor.Text ?? string.Empty).Trim();
        account.UpdatedAt = now;

        try
        {
            _vaultService.SaveVault(session.Data, session.Key);
            await Navigation.PopAsync();
        }
        catch (VaultException ex)
        {
            await DisplayAlert("오류", ex.Message, "확인");
        }
    }
}

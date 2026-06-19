using AccountVault.Mobile.Services;
using AccountVault.Models;
using AccountVault.Services;

namespace AccountVault.Mobile.Pages;

public sealed class AccountDetailPage : ContentPage
{
    private readonly IServiceProvider _services;
    private readonly VaultService _vaultService;
    private readonly MobileClipboardService _clipboardService;
    private readonly string _accountId;
    private readonly Label _siteLabel;
    private readonly Label _urlLabel;
    private readonly Entry _userIdEntry;
    private readonly Entry _passwordEntry;
    private readonly Editor _memoEditor;
    private bool _isPasswordVisible;

    public AccountDetailPage(IServiceProvider services, string accountId)
    {
        _services = services;
        _vaultService = services.GetRequiredService<VaultService>();
        _clipboardService = services.GetRequiredService<MobileClipboardService>();
        _accountId = accountId;
        Title = "상세 정보";

        _siteLabel = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold };
        _urlLabel = new Label { LineBreakMode = LineBreakMode.WordWrap };
        _userIdEntry = new Entry { IsReadOnly = true };
        _passwordEntry = new Entry { IsReadOnly = true };
        _memoEditor = new Editor { IsReadOnly = true, AutoSize = EditorAutoSizeOption.TextChanges };

        var copyUserButton = new Button { Text = "아이디 복사" };
        copyUserButton.Clicked += CopyUserButton_Clicked;

        var togglePasswordButton = new Button { Text = "보기/숨기기" };
        togglePasswordButton.Clicked += (_, _) =>
        {
            _isPasswordVisible = !_isPasswordVisible;
            RefreshAccount();
        };

        var copyPasswordButton = new Button { Text = "비밀번호 복사" };
        copyPasswordButton.Clicked += CopyPasswordButton_Clicked;

        var editButton = new Button { Text = "수정" };
        editButton.Clicked += async (_, _) =>
        {
            var account = FindAccount();
            if (account is not null)
            {
                await Navigation.PushAsync(new AccountEditPage(_services, account.Id));
            }
        };

        var deleteButton = new Button
        {
            Text = "삭제",
            BackgroundColor = Color.FromArgb("#FFF1F0"),
            TextColor = Color.FromArgb("#B42318")
        };
        deleteButton.Clicked += DeleteButton_Clicked;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 18,
                Spacing = 12,
                Children =
                {
                    _siteLabel,
                    MobileLayout.CreateField("URL", _urlLabel),
                    MobileLayout.CreateField("아이디", _userIdEntry),
                    copyUserButton,
                    MobileLayout.CreateField("비밀번호", _passwordEntry),
                    new HorizontalStackLayout { Spacing = 8, Children = { togglePasswordButton, copyPasswordButton } },
                    MobileLayout.CreateField("메모", _memoEditor),
                    new HorizontalStackLayout { Spacing = 8, Children = { editButton, deleteButton } }
                }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshAccount();
    }

    private AccountItem? FindAccount()
    {
        return ((App)Application.Current!).Session?.Data?.Accounts.FirstOrDefault(account => account.Id == _accountId);
    }

    private void RefreshAccount()
    {
        var account = FindAccount();
        if (account is null)
        {
            return;
        }

        _siteLabel.Text = account.SiteName;
        _urlLabel.Text = account.Url;
        _userIdEntry.Text = account.UserId;
        _passwordEntry.Text = _isPasswordVisible ? account.Password : new string('●', Math.Clamp(account.Password.Length, 4, 12));
        _memoEditor.Text = account.Memo;
    }

    private async void CopyUserButton_Clicked(object? sender, EventArgs e)
    {
        var account = FindAccount();
        await _clipboardService.CopyTextAsync(account?.UserId ?? string.Empty);
        await DisplayAlert("복사", "아이디를 클립보드에 복사했습니다.", "확인");
    }

    private async void CopyPasswordButton_Clicked(object? sender, EventArgs e)
    {
        var account = FindAccount();
        await _clipboardService.CopyPasswordAsync(account?.Password ?? string.Empty, TimeSpan.FromSeconds(30));
        await DisplayAlert("복사", "비밀번호를 복사했습니다. 30초 뒤 자동 삭제됩니다.", "확인");
    }

    private async void DeleteButton_Clicked(object? sender, EventArgs e)
    {
        var session = ((App)Application.Current!).Session;
        var account = FindAccount();
        if (session?.Data is null || account is null)
        {
            return;
        }

        if (!await DisplayAlert("삭제", $"'{account.SiteName}' 계정을 삭제할까요?", "삭제", "취소"))
        {
            return;
        }

        session.Data.Accounts.Remove(account);
        _vaultService.SaveVault(session.Data, session.Key);
        await Navigation.PopToRootAsync();
    }
}

using AccountVault.Models;

namespace AccountVault.ViewModels;

public sealed class AccountEditViewModel : BaseViewModel
{
    private string _siteName = string.Empty;
    private string _url = string.Empty;
    private string _userId = string.Empty;
    private string _password = string.Empty;
    private string _memo = string.Empty;
    private string _category = string.Empty;
    private string _errorMessage = string.Empty;

    public AccountEditViewModel(AccountItem? account)
    {
        IsEditMode = account is not null;

        if (account is not null)
        {
            Id = account.Id;
            CreatedAt = account.CreatedAt;
            SiteName = account.SiteName;
            Url = account.Url;
            UserId = account.UserId;
            Password = account.Password;
            Memo = account.Memo;
            Category = account.Category;
        }
    }

    public bool IsEditMode { get; }
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; private set; } = DateTime.Now;

    public string WindowTitle => IsEditMode ? "계정 수정" : "계정 추가";

    public string SiteName
    {
        get => _siteName;
        set => SetProperty(ref _siteName, value);
    }

    public string Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    public string UserId
    {
        get => _userId;
        set => SetProperty(ref _userId, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string Memo
    {
        get => _memo;
        set => SetProperty(ref _memo, value);
    }

    public string Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool Validate()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(SiteName))
        {
            ErrorMessage = "사이트명은 필수입니다.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(UserId) && string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "아이디 또는 비밀번호 중 하나 이상을 입력하세요.";
            return false;
        }

        return true;
    }

    public AccountItem ToAccount()
    {
        var now = DateTime.Now;

        return new AccountItem
        {
            Id = Id,
            SiteName = SiteName.Trim(),
            Url = Url.Trim(),
            UserId = UserId.Trim(),
            Password = Password,
            Memo = Memo.Trim(),
            Category = Category.Trim(),
            CreatedAt = IsEditMode ? CreatedAt : now,
            UpdatedAt = now
        };
    }
}

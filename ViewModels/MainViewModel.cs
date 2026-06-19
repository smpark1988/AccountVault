using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using AccountVault.Models;
using AccountVault.Services;

namespace AccountVault.ViewModels;

public sealed class MainViewModel : BaseViewModel
{
    private readonly VaultService _vaultService;
    private readonly ClipboardService _clipboardService;
    private readonly VaultSession _session;
    private readonly Func<AccountItem?, AccountItem?> _showAccountEditor;
    private readonly Func<string, string, bool> _confirm;
    private readonly Action<string, string> _showMessage;
    private readonly Action _requestLock;
    private VaultData? _vaultData;
    private AccountItem? _selectedAccount;
    private string _searchText = string.Empty;
    private string _statusMessage = "준비됨";
    private bool _isPasswordVisible;

    public MainViewModel(
        VaultService vaultService,
        ClipboardService clipboardService,
        VaultSession session,
        Func<AccountItem?, AccountItem?> showAccountEditor,
        Func<string, string, bool> confirm,
        Action<string, string> showMessage,
        Action requestLock)
    {
        _vaultService = vaultService;
        _clipboardService = clipboardService;
        _session = session;
        _vaultData = session.Data ?? throw new InvalidOperationException("VaultData가 없습니다.");
        _showAccountEditor = showAccountEditor;
        _confirm = confirm;
        _showMessage = showMessage;
        _requestLock = requestLock;

        Accounts = new ObservableCollection<AccountItem>(_vaultData.Accounts);
        AccountsView = CollectionViewSource.GetDefaultView(Accounts);
        AccountsView.Filter = FilterAccount;

        AddCommand = new RelayCommand(_ => AddAccount());
        EditCommand = new RelayCommand(_ => EditAccount(), _ => SelectedAccount is not null);
        DeleteCommand = new RelayCommand(_ => DeleteAccount(), _ => SelectedAccount is not null);
        CopyUserIdCommand = new RelayCommand(_ => CopyUserId(), _ => !string.IsNullOrEmpty(SelectedAccount?.UserId));
        CopyPasswordCommand = new RelayCommand(_ => CopyPassword(), _ => !string.IsNullOrEmpty(SelectedAccount?.Password));
        TogglePasswordCommand = new RelayCommand(_ => TogglePassword(), _ => !string.IsNullOrEmpty(SelectedAccount?.Password));
        LockCommand = new RelayCommand(_ => _requestLock());
    }

    public ObservableCollection<AccountItem> Accounts { get; }
    public ICollectionView AccountsView { get; }

    public RelayCommand AddCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand CopyUserIdCommand { get; }
    public RelayCommand CopyPasswordCommand { get; }
    public RelayCommand TogglePasswordCommand { get; }
    public RelayCommand LockCommand { get; }

    public AccountItem? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            if (SetProperty(ref _selectedAccount, value))
            {
                IsPasswordVisible = false;
                OnPropertyChanged(nameof(DisplayedPassword));
                RaiseSelectedCommands();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                AccountsView.Refresh();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsPasswordVisible
    {
        get => _isPasswordVisible;
        private set
        {
            if (SetProperty(ref _isPasswordVisible, value))
            {
                OnPropertyChanged(nameof(DisplayedPassword));
                OnPropertyChanged(nameof(PasswordToggleText));
            }
        }
    }

    public string DisplayedPassword
    {
        get
        {
            if (SelectedAccount is null || string.IsNullOrEmpty(SelectedAccount.Password))
            {
                return string.Empty;
            }

            if (IsPasswordVisible)
            {
                return SelectedAccount.Password;
            }

            var length = Math.Clamp(SelectedAccount.Password.Length, 4, 12);
            return new string('●', length);
        }
    }

    public string PasswordToggleText => IsPasswordVisible ? "숨기기" : "보기";

    public void ExportBackup(string targetPath)
    {
        try
        {
            _vaultService.ExportBackup(targetPath);
            StatusMessage = "백업을 내보냈습니다.";
        }
        catch (VaultException ex)
        {
            _showMessage("백업 내보내기", ex.Message);
        }
    }

    public bool ImportBackup(string sourcePath, string masterPassword)
    {
        try
        {
            _vaultService.ImportBackup(sourcePath, masterPassword);
            ClearSensitiveData();
            return true;
        }
        catch (VaultException ex)
        {
            _showMessage("백업 가져오기", ex.Message);
            return false;
        }
    }

    public void ClearSensitiveData()
    {
        _session.Clear();
        _vaultData = null;
        SelectedAccount = null;
        Accounts.Clear();
    }

    private void AddAccount()
    {
        var account = _showAccountEditor(null);
        if (account is null)
        {
            return;
        }

        Accounts.Add(account);
        SelectedAccount = account;
        if (SaveVault())
        {
            StatusMessage = "계정을 추가했습니다.";
        }
    }

    private void EditAccount()
    {
        if (SelectedAccount is null)
        {
            return;
        }

        var edited = _showAccountEditor(SelectedAccount.Clone());
        if (edited is null)
        {
            return;
        }

        SelectedAccount.SiteName = edited.SiteName;
        SelectedAccount.Url = edited.Url;
        SelectedAccount.UserId = edited.UserId;
        SelectedAccount.Password = edited.Password;
        SelectedAccount.Memo = edited.Memo;
        SelectedAccount.Category = edited.Category;
        SelectedAccount.UpdatedAt = edited.UpdatedAt;

        AccountsView.Refresh();
        OnPropertyChanged(nameof(SelectedAccount));
        OnPropertyChanged(nameof(DisplayedPassword));
        if (SaveVault())
        {
            StatusMessage = "계정을 수정했습니다.";
        }
    }

    private void DeleteAccount()
    {
        if (SelectedAccount is null)
        {
            return;
        }

        var message = $"'{SelectedAccount.SiteName}' 계정을 삭제할까요?";
        if (!_confirm("계정 삭제", message))
        {
            return;
        }

        var accountToRemove = SelectedAccount;
        Accounts.Remove(accountToRemove);
        SelectedAccount = null;
        if (SaveVault())
        {
            StatusMessage = "계정을 삭제했습니다.";
        }
    }

    private void CopyUserId()
    {
        try
        {
            _clipboardService.CopyText(SelectedAccount?.UserId ?? string.Empty);
            StatusMessage = "아이디를 클립보드에 복사했습니다.";
        }
        catch (InvalidOperationException ex)
        {
            _showMessage("클립보드", ex.Message);
        }
    }

    private void CopyPassword()
    {
        try
        {
            _clipboardService.CopyPassword(SelectedAccount?.Password ?? string.Empty, TimeSpan.FromSeconds(30));
            StatusMessage = "비밀번호를 복사했습니다. 30초 뒤 자동 삭제됩니다.";
        }
        catch (InvalidOperationException ex)
        {
            _showMessage("클립보드", ex.Message);
        }
    }

    private void TogglePassword()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    private bool SaveVault()
    {
        if (_vaultData is null)
        {
            return false;
        }

        try
        {
            _vaultData.Accounts = Accounts.ToList();
            _vaultService.SaveVault(_vaultData, _session.Key);
            return true;
        }
        catch (VaultException ex)
        {
            StatusMessage = "저장 실패";
            _showMessage("저장", ex.Message);
            return false;
        }
    }

    private bool FilterAccount(object item)
    {
        if (item is not AccountItem account)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return Contains(account.SiteName, SearchText) ||
               Contains(account.Url, SearchText) ||
               Contains(account.UserId, SearchText);
    }

    private static bool Contains(string value, string searchText)
    {
        return value.Contains(searchText, StringComparison.CurrentCultureIgnoreCase);
    }

    private void RaiseSelectedCommands()
    {
        EditCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
        CopyUserIdCommand.RaiseCanExecuteChanged();
        CopyPasswordCommand.RaiseCanExecuteChanged();
        TogglePasswordCommand.RaiseCanExecuteChanged();
    }
}

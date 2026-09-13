using AccountVault.Services;

namespace AccountVault.ViewModels;

public sealed class LoginViewModel : BaseViewModel
{
    private readonly VaultService _vaultService;
    private string _errorMessage = string.Empty;

    public LoginViewModel(VaultService vaultService)
    {
        _vaultService = vaultService;
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public VaultSession? Session { get; private set; }

    public bool Unlock(string masterPassword)
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrEmpty(masterPassword))
        {
            ErrorMessage = "마스터 비밀번호를 입력하세요.";
            return false;
        }

        try
        {
            Session = _vaultService.UnlockVault(masterPassword);
            return true;
        }
        catch (VaultException ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }
}

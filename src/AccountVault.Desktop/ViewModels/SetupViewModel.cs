using AccountVault.Services;

namespace AccountVault.ViewModels;

public sealed class SetupViewModel : BaseViewModel
{
    private readonly VaultService _vaultService;
    private string _errorMessage = string.Empty;

    public SetupViewModel(VaultService vaultService)
    {
        _vaultService = vaultService;
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public VaultSession? Session { get; private set; }

    public bool Create(string masterPassword, string confirmPassword)
    {
        ErrorMessage = string.Empty;

        if (masterPassword.Length < 8)
        {
            ErrorMessage = "마스터 비밀번호는 8자 이상으로 설정하세요.";
            return false;
        }

        if (!string.Equals(masterPassword, confirmPassword, StringComparison.Ordinal))
        {
            ErrorMessage = "마스터 비밀번호와 확인 입력값이 일치하지 않습니다.";
            return false;
        }

        try
        {
            Session = _vaultService.CreateVault(masterPassword);
            return true;
        }
        catch (VaultException ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }
}

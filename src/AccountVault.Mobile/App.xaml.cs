using AccountVault.Mobile.Pages;
using AccountVault.Services;

namespace AccountVault.Mobile;

public partial class App : Application
{
    private readonly VaultService _vaultService;
    private readonly IServiceProvider _services;
    private VaultSession? _session;

    public App(VaultService vaultService, IServiceProvider services)
    {
        InitializeComponent();
        _vaultService = vaultService;
        _services = services;
        ShowEntryPage();
    }

    public VaultSession? Session => _session;

    public void SetSession(VaultSession session)
    {
        _session = session;
        MainPage = new NavigationPage(new AccountListPage(_services));
    }

    public void Lock()
    {
        _session?.Clear();
        _session = null;
        ShowEntryPage();
    }

    protected override void OnSleep()
    {
        Lock();
        base.OnSleep();
    }

    private void ShowEntryPage()
    {
        Page page = _vaultService.VaultExists()
            ? new LoginPage(_services)
            : new SetupPage(_services);

        MainPage = new NavigationPage(page);
    }
}

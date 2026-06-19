using AccountVault.Mobile.Services;
using AccountVault.Services;

namespace AccountVault.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>();

        builder.Services.AddSingleton<CryptoService>();
        builder.Services.AddSingleton(provider =>
        {
            var crypto = provider.GetRequiredService<CryptoService>();
            return new VaultService(crypto, MobileVaultPaths.GetDataDirectory);
        });
        builder.Services.AddSingleton<MobileClipboardService>();

        return builder.Build();
    }
}

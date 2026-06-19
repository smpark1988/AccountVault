namespace AccountVault.Mobile.Services;

public static class MobileVaultPaths
{
    public static string GetDataDirectory()
    {
        return Path.Combine(FileSystem.AppDataDirectory, "data");
    }
}

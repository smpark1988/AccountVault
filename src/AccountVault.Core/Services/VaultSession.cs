using AccountVault.Models;

namespace AccountVault.Services;

public sealed class VaultSession : IDisposable
{
    public VaultSession(VaultData data, byte[] key)
    {
        Data = data;
        Key = key;
    }

    public VaultData? Data { get; private set; }
    public byte[] Key { get; }
    public bool IsCleared { get; private set; }

    // Clears sensitive session state when the app locks or exits.
    public void Clear()
    {
        if (IsCleared)
        {
            return;
        }

        Array.Clear(Key);
        Data = null;
        IsCleared = true;
    }

    public void Dispose() => Clear();
}

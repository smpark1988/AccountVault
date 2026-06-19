namespace AccountVault.Models;

public sealed class VaultFile
{
    public int Version { get; set; } = 1;
    public string Kdf { get; set; } = "PBKDF2-SHA256";
    public int Iterations { get; set; } = 300_000;
    public string Salt { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string CipherText { get; set; } = string.Empty;
}

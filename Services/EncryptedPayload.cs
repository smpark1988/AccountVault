namespace AccountVault.Services;

public sealed class EncryptedPayload
{
    public byte[] Nonce { get; init; } = Array.Empty<byte>();
    public byte[] Tag { get; init; } = Array.Empty<byte>();
    public byte[] CipherText { get; init; } = Array.Empty<byte>();
}

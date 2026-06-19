using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AccountVault.Models;

namespace AccountVault.Services;

public sealed class VaultService
{
    private const int CurrentVersion = 1;
    private const string CurrentKdf = "PBKDF2-SHA256";
    private const string VaultFileName = "vault.dat";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly CryptoService _cryptoService;
    private readonly Func<string> _dataDirectoryFactory;

    public VaultService(CryptoService cryptoService, Func<string>? dataDirectoryFactory = null)
    {
        _cryptoService = cryptoService;
        _dataDirectoryFactory = dataDirectoryFactory ?? GetDefaultDataDirectory;
    }

    public string GetDataDirectory()
    {
        return _dataDirectoryFactory();
    }

    public string GetVaultFilePath()
    {
        return Path.Combine(GetDataDirectory(), VaultFileName);
    }

    public bool VaultExists()
    {
        return File.Exists(GetVaultFilePath());
    }

    public VaultSession CreateVault(string masterPassword)
    {
        if (VaultExists())
        {
            throw new VaultException("이미 vault.dat 파일이 있습니다.");
        }

        var data = new VaultData();
        var salt = _cryptoService.GenerateSalt();
        var key = _cryptoService.DeriveKey(masterPassword, salt, CryptoService.DefaultIterations);

        try
        {
            SaveVault(data, key, salt, CryptoService.DefaultIterations);
            return new VaultSession(data, key);
        }
        catch
        {
            Array.Clear(key);
            throw;
        }
    }

    public VaultSession UnlockVault(string masterPassword)
    {
        var path = GetVaultFilePath();

        if (!File.Exists(path))
        {
            throw new VaultException("vault.dat 파일이 없습니다.");
        }

        byte[]? key = null;

        try
        {
            var vaultFile = ReadVaultFile(path);
            var salt = Convert.FromBase64String(vaultFile.Salt);
            key = _cryptoService.DeriveKey(masterPassword, salt, vaultFile.Iterations);
            var json = _cryptoService.Decrypt(vaultFile, key);
            var data = JsonSerializer.Deserialize<VaultData>(json, JsonOptions)
                       ?? throw new JsonException("VaultData가 비어 있습니다.");

            data.Accounts ??= new List<AccountItem>();
            return new VaultSession(data, key);
        }
        catch (VaultException)
        {
            ClearKey(key);
            throw;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException)
        {
            ClearKey(key);
            throw new VaultException("마스터 비밀번호가 올바르지 않거나 데이터 파일이 손상되었습니다.", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ClearKey(key);
            throw new VaultException("데이터 파일에 접근할 수 없습니다.", ex);
        }
    }

    public void SaveVault(VaultData data, byte[] key)
    {
        try
        {
            var path = GetVaultFilePath();
            var existingFile = ReadVaultFile(path);
            var salt = Convert.FromBase64String(existingFile.Salt);
            SaveVault(data, key, salt, existingFile.Iterations);
        }
        catch (VaultException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            throw new VaultException("vault.dat 파일을 저장할 수 없습니다.", ex);
        }
    }

    public void ExportBackup(string targetPath)
    {
        var sourcePath = GetVaultFilePath();

        if (!File.Exists(sourcePath))
        {
            throw new VaultException("내보낼 vault.dat 파일이 없습니다.");
        }

        try
        {
            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.Copy(sourcePath, targetPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new VaultException("백업 파일을 내보낼 수 없습니다.", ex);
        }
    }

    public void ImportBackup(string sourcePath, string masterPassword)
    {
        if (!File.Exists(sourcePath))
        {
            throw new VaultException("가져올 백업 파일을 찾을 수 없습니다.");
        }

        var targetPath = GetVaultFilePath();
        var fullSourcePath = Path.GetFullPath(sourcePath);
        var fullTargetPath = Path.GetFullPath(targetPath);

        if (string.Equals(fullSourcePath, fullTargetPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new VaultException("현재 사용 중인 vault.dat 파일과 같은 파일입니다.");
        }

        byte[]? testKey = null;

        try
        {
            var candidate = ReadVaultFile(sourcePath);
            var salt = Convert.FromBase64String(candidate.Salt);
            testKey = _cryptoService.DeriveKey(masterPassword, salt, candidate.Iterations);
            var json = _cryptoService.Decrypt(candidate, testKey);
            _ = JsonSerializer.Deserialize<VaultData>(json, JsonOptions)
                ?? throw new JsonException("VaultData가 비어 있습니다.");

            Directory.CreateDirectory(GetDataDirectory());
            var tempPath = targetPath + ".import.tmp";
            File.Copy(sourcePath, tempPath, overwrite: true);
            ReplaceFile(tempPath, targetPath);
        }
        catch (VaultException ex)
        {
            throw new VaultException("백업 파일이 손상되었거나 형식이 올바르지 않습니다.", ex);
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException)
        {
            throw new VaultException("백업 파일이 손상되었거나 마스터 비밀번호가 올바르지 않습니다.", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new VaultException("백업 파일을 가져올 수 없습니다.", ex);
        }
        finally
        {
            ClearKey(testKey);
        }
    }

    private void SaveVault(VaultData data, byte[] key, byte[] salt, int iterations)
    {
        try
        {
            Directory.CreateDirectory(GetDataDirectory());

            var dataJson = JsonSerializer.Serialize(data, JsonOptions);
            var encrypted = _cryptoService.Encrypt(dataJson, key);
            var vaultFile = new VaultFile
            {
                Version = CurrentVersion,
                Kdf = CurrentKdf,
                Iterations = iterations,
                Salt = Convert.ToBase64String(salt),
                Nonce = Convert.ToBase64String(encrypted.Nonce),
                Tag = Convert.ToBase64String(encrypted.Tag),
                CipherText = Convert.ToBase64String(encrypted.CipherText)
            };

            var fileJson = JsonSerializer.Serialize(vaultFile, JsonOptions);
            var targetPath = GetVaultFilePath();
            var tempPath = targetPath + ".tmp";
            WriteAllTextDurable(tempPath, fileJson);
            ReplaceFile(tempPath, targetPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
        {
            throw new VaultException("vault.dat 파일을 저장할 수 없습니다.", ex);
        }
    }

    private static VaultFile ReadVaultFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            var vaultFile = JsonSerializer.Deserialize<VaultFile>(json, JsonOptions)
                            ?? throw new JsonException("vault.dat 내용이 비어 있습니다.");

            ValidateVaultFile(vaultFile);
            return vaultFile;
        }
        catch (JsonException ex)
        {
            throw new VaultException("vault.dat JSON 형식이 올바르지 않습니다.", ex);
        }
    }

    private static void ValidateVaultFile(VaultFile vaultFile)
    {
        if (vaultFile.Version != CurrentVersion ||
            !string.Equals(vaultFile.Kdf, CurrentKdf, StringComparison.Ordinal) ||
            vaultFile.Iterations < CryptoService.DefaultIterations ||
            string.IsNullOrWhiteSpace(vaultFile.Salt) ||
            string.IsNullOrWhiteSpace(vaultFile.Nonce) ||
            string.IsNullOrWhiteSpace(vaultFile.Tag) ||
            string.IsNullOrWhiteSpace(vaultFile.CipherText))
        {
            throw new JsonException("vault.dat 메타데이터가 올바르지 않습니다.");
        }
    }

    private static void WriteAllTextDurable(string path, string contents)
    {
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(contents);
        using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.WriteThrough);

        stream.Write(bytes, 0, bytes.Length);
        stream.Flush(flushToDisk: true);
    }

    private static void ReplaceFile(string tempPath, string targetPath)
    {
        if (File.Exists(targetPath))
        {
            File.Replace(tempPath, targetPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            return;
        }

        File.Move(tempPath, targetPath);
    }

    private static void ClearKey(byte[]? key)
    {
        if (key is not null)
        {
            Array.Clear(key);
        }
    }

    private static string GetDefaultDataDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "data");
    }
}

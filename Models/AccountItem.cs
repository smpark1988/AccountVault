namespace AccountVault.Models;

public sealed class AccountItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SiteName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public AccountItem Clone()
    {
        return new AccountItem
        {
            Id = Id,
            SiteName = SiteName,
            Url = Url,
            UserId = UserId,
            Password = Password,
            Memo = Memo,
            Category = Category,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }
}

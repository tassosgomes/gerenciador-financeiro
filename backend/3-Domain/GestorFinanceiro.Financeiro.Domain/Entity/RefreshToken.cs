namespace GestorFinanceiro.Financeiro.Domain.Entity;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    public User User { get; private set; } = null!;

    private RefreshToken()
    {
    }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt, string createdByUserId)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = token,
            TokenHash = ComputeHash(token),
            ExpiresAt = expiresAt,
        };

        refreshToken.SetAuditOnCreate(createdByUserId);
        return refreshToken;
    }

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }

    public static string ComputeHash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes);
    }
}

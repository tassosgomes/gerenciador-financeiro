using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Domain.Entity;

public class User : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool MustChangePassword { get; private set; } = true;

    private User()
    {
    }

    public static User Create(string name, string email, string passwordHash, UserRole role, string createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash, nameof(passwordHash));

        if (name.Length < 3 || name.Length > 150)
            throw new ArgumentException("Name must be between 3 and 150 characters.", nameof(name));

        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
        };

        user.SetAuditOnCreate(createdByUserId);
        return user;
    }

    public void Deactivate(string userId)
    {
        IsActive = false;
        SetAuditOnUpdate(userId);
    }

    public void Activate(string userId)
    {
        IsActive = true;
        SetAuditOnUpdate(userId);
    }

    public void ChangePassword(string newPasswordHash, string userId)
    {
        PasswordHash = newPasswordHash;
        MustChangePassword = false;
        SetAuditOnUpdate(userId);
    }
}

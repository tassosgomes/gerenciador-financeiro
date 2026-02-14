namespace GestorFinanceiro.Financeiro.Domain.Entity;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public string CreatedBy { get; protected set; } = string.Empty;
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    public void SetAuditOnCreate(string userId)
    {
        CreatedBy = userId;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetAuditOnUpdate(string userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTime.UtcNow;
    }
}

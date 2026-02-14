namespace GestorFinanceiro.Financeiro.Domain.Entity;

public class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public string? PreviousData { get; private set; }

    private AuditLog()
    {
    }

    public static AuditLog Create(
        string entityType,
        Guid entityId,
        string action,
        string userId,
        string? previousData = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = userId,
            PreviousData = previousData,
        };
    }
}

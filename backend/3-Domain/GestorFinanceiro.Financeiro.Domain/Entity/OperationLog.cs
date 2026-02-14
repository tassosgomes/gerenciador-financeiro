namespace GestorFinanceiro.Financeiro.Domain.Entity;

public class OperationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OperationId { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public Guid ResultEntityId { get; set; }
    public string ResultPayload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public OperationLog()
    {
        var now = DateTime.UtcNow;
        CreatedAt = now;
        ExpiresAt = now.AddHours(24);
    }
}

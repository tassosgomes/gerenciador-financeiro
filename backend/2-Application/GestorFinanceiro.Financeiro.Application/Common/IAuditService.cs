namespace GestorFinanceiro.Financeiro.Application.Common;

public interface IAuditService
{
    Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        string userId,
        object? previousData,
        CancellationToken cancellationToken);
}

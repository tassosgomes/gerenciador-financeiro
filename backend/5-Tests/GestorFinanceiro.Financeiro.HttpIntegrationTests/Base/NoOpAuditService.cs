using GestorFinanceiro.Financeiro.Application.Common;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Base;

public sealed class NoOpAuditService : IAuditService
{
    public Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        string userId,
        object? previousData,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

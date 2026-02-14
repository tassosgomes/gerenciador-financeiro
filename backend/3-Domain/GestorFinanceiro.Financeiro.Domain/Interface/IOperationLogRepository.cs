using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface IOperationLogRepository
{
    Task<bool> ExistsByOperationIdAsync(string operationId, CancellationToken cancellationToken);
    Task AddAsync(OperationLog log, CancellationToken cancellationToken);
    Task CleanupExpiredAsync(CancellationToken cancellationToken);
}

using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetByInstallmentGroupAsync(Guid groupId, CancellationToken cancellationToken);
    Task<IEnumerable<Transaction>> GetByTransferGroupAsync(Guid groupId, CancellationToken cancellationToken);
    Task<Transaction?> GetByOperationIdAsync(string operationId, CancellationToken cancellationToken);
}

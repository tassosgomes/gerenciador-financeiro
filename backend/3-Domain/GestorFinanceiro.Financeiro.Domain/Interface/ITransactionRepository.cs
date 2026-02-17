using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface ITransactionRepository : IRepository<Transaction>
{
    IQueryable<Transaction> Query();
    Task<IEnumerable<Transaction>> GetByInstallmentGroupAsync(Guid groupId, CancellationToken cancellationToken);
    Task<IEnumerable<Transaction>> GetByTransferGroupAsync(Guid groupId, CancellationToken cancellationToken);
    Task<Transaction?> GetByOperationIdAsync(string operationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Transaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Transaction>> GetByAccountAndPeriodAsync(Guid accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
    Task<IReadOnlyList<Transaction>> GetByRecurrenceTemplateIdAsync(Guid recurrenceTemplateId, CancellationToken cancellationToken);
    void RemoveRange(IEnumerable<Transaction> transactions);
}

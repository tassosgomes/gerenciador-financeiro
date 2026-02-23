using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface IReceiptItemRepository : IRepository<ReceiptItem>
{
    Task AddRangeAsync(IEnumerable<ReceiptItem> items, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReceiptItem>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken);
    void RemoveRange(IEnumerable<ReceiptItem> items);
}

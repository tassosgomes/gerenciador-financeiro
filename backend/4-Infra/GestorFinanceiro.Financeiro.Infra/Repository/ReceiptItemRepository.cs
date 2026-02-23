using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanceiro.Financeiro.Infra.Repository;

public class ReceiptItemRepository : Repository<ReceiptItem>, IReceiptItemRepository
{
    public ReceiptItemRepository(FinanceiroDbContext context)
        : base(context)
    {
    }

    public async Task AddRangeAsync(IEnumerable<ReceiptItem> items, CancellationToken cancellationToken)
    {
        await _context.ReceiptItems.AddRangeAsync(items, cancellationToken);
    }

    public async Task<IReadOnlyList<ReceiptItem>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        return await _context.ReceiptItems
            .AsNoTracking()
            .Where(receiptItem => receiptItem.TransactionId == transactionId)
            .OrderBy(receiptItem => receiptItem.ItemOrder)
            .ToListAsync(cancellationToken);
    }

    public void RemoveRange(IEnumerable<ReceiptItem> items)
    {
        _context.ReceiptItems.RemoveRange(items);
    }
}

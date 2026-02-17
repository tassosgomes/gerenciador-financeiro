using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanceiro.Financeiro.Infra.Repository;

public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(FinanceiroDbContext context)
        : base(context)
    {
    }

    public IQueryable<Transaction> Query()
    {
        return _context.Transactions;
    }

    public async Task<IEnumerable<Transaction>> GetByInstallmentGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return await _context.Transactions
            .Where(transaction => transaction.InstallmentGroupId == groupId)
            .OrderBy(transaction => transaction.InstallmentNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByTransferGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return await _context.Transactions
            .Where(transaction => transaction.TransferGroupId == groupId)
            .OrderBy(transaction => transaction.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Transaction?> GetByOperationIdAsync(string operationId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);

        return await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(transaction => transaction.OperationId == operationId, cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.AccountId == accountId)
            .OrderByDescending(transaction => transaction.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetByAccountAndPeriodAsync(
        Guid accountId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.AccountId == accountId
                && transaction.CompetenceDate > startDate
                && transaction.CompetenceDate <= endDate
                && transaction.Status == TransactionStatus.Paid)
            .OrderBy(transaction => transaction.CompetenceDate)
                .ThenBy(transaction => transaction.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetByRecurrenceTemplateIdAsync(Guid recurrenceTemplateId, CancellationToken cancellationToken)
    {
        return await _context.Transactions
            .Where(transaction => transaction.RecurrenceTemplateId == recurrenceTemplateId)
            .OrderBy(transaction => transaction.CompetenceDate)
            .ThenBy(transaction => transaction.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void RemoveRange(IEnumerable<Transaction> transactions)
    {
        _context.Transactions.RemoveRange(transactions);
    }
}

using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanceiro.Financeiro.Infra.Repository;

public class OperationLogRepository : IOperationLogRepository
{
    private readonly FinanceiroDbContext _context;

    public OperationLogRepository(FinanceiroDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> ExistsByOperationIdAsync(string operationId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);

        return await _context.OperationLogs
            .AsNoTracking()
            .AnyAsync(log => log.OperationId == operationId, cancellationToken);
    }

    public async Task AddAsync(OperationLog log, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(log);

        await _context.OperationLogs.AddAsync(log, cancellationToken);
    }

    public async Task CleanupExpiredAsync(CancellationToken cancellationToken)
    {
        await _context.OperationLogs
            .Where(log => log.ExpiresAt < DateTime.UtcNow)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

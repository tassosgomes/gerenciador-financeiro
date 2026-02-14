using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanceiro.Financeiro.Infra.Repository;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly FinanceiroDbContext _context;

    public AuditLogRepository(FinanceiroDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditLog);
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByFiltersAsync(
        string? entityType,
        Guid? entityId,
        string? userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        var queryable = Query();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            queryable = queryable.Where(auditLog => auditLog.EntityType == entityType);
        }

        if (entityId.HasValue)
        {
            queryable = queryable.Where(auditLog => auditLog.EntityId == entityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            queryable = queryable.Where(auditLog => auditLog.UserId == userId);
        }

        if (dateFrom.HasValue)
        {
            queryable = queryable.Where(auditLog => auditLog.Timestamp >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            queryable = queryable.Where(auditLog => auditLog.Timestamp <= dateTo.Value);
        }

        return await queryable
            .OrderByDescending(auditLog => auditLog.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public IQueryable<AuditLog> Query()
    {
        return _context.AuditLogs.AsNoTracking();
    }
}

using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken);

    Task<IEnumerable<AuditLog>> GetByFiltersAsync(
        string? entityType,
        Guid? entityId,
        string? userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken);

    IQueryable<AuditLog> Query();
}

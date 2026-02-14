using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Audit;

public class ListAuditLogsQueryHandler : IQueryHandler<ListAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private const int MaxPageSize = 100;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<ListAuditLogsQueryHandler> _logger;

    public ListAuditLogsQueryHandler(IAuditLogRepository auditLogRepository, ILogger<ListAuditLogsQueryHandler> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<PagedResult<AuditLogDto>> HandleAsync(ListAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var size = query.Size <= 0 ? 20 : Math.Min(query.Size, MaxPageSize);

        var queryable = _auditLogRepository.Query();

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            queryable = queryable.Where(auditLog => auditLog.EntityType == query.EntityType);
        }

        if (query.EntityId.HasValue)
        {
            queryable = queryable.Where(auditLog => auditLog.EntityId == query.EntityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            queryable = queryable.Where(auditLog => auditLog.UserId == query.UserId);
        }

        if (query.DateFrom.HasValue)
        {
            queryable = queryable.Where(auditLog => auditLog.Timestamp >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            queryable = queryable.Where(auditLog => auditLog.Timestamp <= query.DateTo.Value);
        }

        var total = await queryable.CountAsync(cancellationToken);
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / size);

        var items = await queryable
            .OrderByDescending(auditLog => auditLog.Timestamp)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Listing audit logs with filters. Page: {Page}, Size: {Size}, Total: {Total}",
            page,
            size,
            total);

        return new PagedResult<AuditLogDto>(
            items.Adapt<IReadOnlyList<AuditLogDto>>(),
            new PaginationMetadata(page, size, total, totalPages));
    }
}

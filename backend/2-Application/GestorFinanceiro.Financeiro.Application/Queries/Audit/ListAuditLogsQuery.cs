using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Queries.Audit;

public record ListAuditLogsQuery(
    string? EntityType,
    Guid? EntityId,
    string? UserId,
    DateTime? DateFrom,
    DateTime? DateTo,
    int Page = 1,
    int Size = 20) : IQuery<PagedResult<AuditLogDto>>;

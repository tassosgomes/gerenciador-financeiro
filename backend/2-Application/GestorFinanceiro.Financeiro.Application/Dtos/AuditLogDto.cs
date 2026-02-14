namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record AuditLogDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string UserId,
    DateTime Timestamp,
    string? PreviousData
);

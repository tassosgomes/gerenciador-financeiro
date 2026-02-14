using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record RecurrenceTemplateResponse(
    Guid Id,
    Guid AccountId,
    Guid CategoryId,
    TransactionType Type,
    decimal Amount,
    string Description,
    int DayOfMonth,
    bool IsActive,
    DateTime? LastGeneratedDate,
    TransactionStatus DefaultStatus,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
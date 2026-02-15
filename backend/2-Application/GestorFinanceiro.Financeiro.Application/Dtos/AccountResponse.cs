using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record AccountResponse(
    Guid Id,
    string Name,
    AccountType Type,
    decimal Balance,
    bool AllowNegativeBalance,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

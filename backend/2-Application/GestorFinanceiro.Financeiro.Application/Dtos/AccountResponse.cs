namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record AccountResponse(
    Guid Id,
    string Name,
    decimal Balance,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
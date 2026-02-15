using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record CategoryResponse(
    Guid Id,
    string Name,
    CategoryType Type,
    bool IsActive,
    bool IsSystem,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
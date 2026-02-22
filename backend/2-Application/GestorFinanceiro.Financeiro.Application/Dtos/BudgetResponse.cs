namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record BudgetResponse(
    Guid Id,
    string Name,
    decimal Percentage,
    int ReferenceYear,
    int ReferenceMonth,
    bool IsRecurrent,
    decimal MonthlyIncome,
    decimal LimitAmount,
    decimal ConsumedAmount,
    decimal RemainingAmount,
    decimal ConsumedPercentage,
    IReadOnlyList<BudgetCategoryDto> Categories,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

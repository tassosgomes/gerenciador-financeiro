namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record AvailablePercentageResponse(
    decimal UsedPercentage,
    decimal AvailablePercentage,
    IReadOnlyList<Guid> UsedCategoryIds
);

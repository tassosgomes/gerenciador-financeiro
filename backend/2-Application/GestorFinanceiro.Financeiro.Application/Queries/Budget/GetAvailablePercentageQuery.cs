using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Queries.Budget;

public record GetAvailablePercentageQuery(
    int Year,
    int Month,
    Guid? ExcludeBudgetId = null
) : IQuery<AvailablePercentageResponse>;

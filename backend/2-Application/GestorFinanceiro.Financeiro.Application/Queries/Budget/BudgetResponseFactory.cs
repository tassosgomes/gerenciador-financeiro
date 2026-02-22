using GestorFinanceiro.Financeiro.Application.Dtos;
using BudgetEntity = GestorFinanceiro.Financeiro.Domain.Entity.Budget;

namespace GestorFinanceiro.Financeiro.Application.Queries.Budget;

internal static class BudgetResponseFactory
{
    public static BudgetResponse Build(
        BudgetEntity budget,
        decimal monthlyIncome,
        decimal consumedAmount,
        IReadOnlyList<BudgetCategoryDto> categories)
    {
        var limitAmount = budget.CalculateLimit(monthlyIncome);
        var remainingAmount = limitAmount - consumedAmount;
        var consumedPercentage = limitAmount > 0m
            ? (consumedAmount / limitAmount) * 100m
            : 0m;

        return new BudgetResponse(
            budget.Id,
            budget.Name,
            budget.Percentage,
            budget.ReferenceYear,
            budget.ReferenceMonth,
            budget.IsRecurrent,
            monthlyIncome,
            limitAmount,
            consumedAmount,
            remainingAmount,
            consumedPercentage,
            categories,
            budget.CreatedAt,
            budget.UpdatedAt);
    }
}

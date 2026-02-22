using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Queries.Budget;

public record GetBudgetByIdQuery(
    Guid Id
) : IQuery<BudgetResponse>;

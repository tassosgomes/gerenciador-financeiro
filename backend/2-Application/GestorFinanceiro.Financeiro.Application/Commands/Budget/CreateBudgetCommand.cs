using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Commands.Budget;

public record CreateBudgetCommand(
    string Name,
    decimal Percentage,
    int ReferenceYear,
    int ReferenceMonth,
    List<Guid> CategoryIds,
    bool IsRecurrent,
    string UserId
) : ICommand<BudgetResponse>;

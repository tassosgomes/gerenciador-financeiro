using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Commands.Budget;

public record UpdateBudgetCommand(
    Guid Id,
    string Name,
    decimal Percentage,
    List<Guid> CategoryIds,
    bool IsRecurrent,
    string UserId
) : ICommand<BudgetResponse>;

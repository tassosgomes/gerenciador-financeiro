using GestorFinanceiro.Financeiro.Application.Common;

namespace GestorFinanceiro.Financeiro.Application.Commands.Budget;

public record DeleteBudgetCommand(
    Guid Id,
    string UserId
) : ICommand<Unit>;

using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Commands.Account;

public record ActivateAccountCommand(
    Guid AccountId,
    string UserId,
    string? OperationId = null
) : ICommand<Unit>;
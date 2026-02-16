using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Commands.Account;

public record UpdateAccountCommand(
    Guid AccountId,
    string Name,
    bool AllowNegativeBalance,
    string UserId,
    string? OperationId = null,
    decimal? CreditLimit = null,
    int? ClosingDay = null,
    int? DueDay = null,
    Guid? DebitAccountId = null,
    bool? EnforceCreditLimit = null
) : ICommand<AccountResponse>;

using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Commands.Account;

public record CreateAccountCommand(
    string Name,
    AccountType Type,
    decimal InitialBalance,
    bool AllowNegativeBalance,
    string UserId,
    string? OperationId = null,
    decimal? CreditLimit = null,
    int? ClosingDay = null,
    int? DueDay = null,
    Guid? DebitAccountId = null,
    bool? EnforceCreditLimit = null
) : ICommand<AccountResponse>;
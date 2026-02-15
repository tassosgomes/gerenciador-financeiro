using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Queries.Account;

public record ListAccountsQuery(
    bool? IsActive = null,
    AccountType? Type = null
) : IQuery<IReadOnlyList<AccountResponse>>;

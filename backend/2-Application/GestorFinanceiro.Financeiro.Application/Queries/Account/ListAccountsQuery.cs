using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Queries.Account;

public record ListAccountsQuery(
    bool? IsActive = null
) : IQuery<IReadOnlyList<AccountResponse>>;

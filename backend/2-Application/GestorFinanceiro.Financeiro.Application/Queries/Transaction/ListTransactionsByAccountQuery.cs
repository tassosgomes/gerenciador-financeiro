using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Queries.Transaction;

public record ListTransactionsByAccountQuery(
    Guid AccountId
) : IQuery<IReadOnlyList<TransactionResponse>>;
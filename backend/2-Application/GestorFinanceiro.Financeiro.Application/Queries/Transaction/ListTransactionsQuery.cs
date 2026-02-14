using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Queries.Transaction;

public record ListTransactionsQuery(
    Guid? AccountId,
    Guid? CategoryId,
    TransactionType? Type,
    TransactionStatus? Status,
    DateTime? CompetenceDateFrom,
    DateTime? CompetenceDateTo,
    DateTime? DueDateFrom,
    DateTime? DueDateTo,
    int Page = 1,
    int Size = 20
) : IQuery<PagedResult<TransactionResponse>>;

using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Queries.Transaction;

public record GetTransactionByIdQuery(
    Guid TransactionId
) : IQuery<TransactionResponse>;
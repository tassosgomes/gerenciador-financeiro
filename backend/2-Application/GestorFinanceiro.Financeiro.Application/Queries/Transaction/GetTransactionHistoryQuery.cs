using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Queries.Transaction;

public record GetTransactionHistoryQuery(Guid TransactionId) : IQuery<TransactionHistoryResponse>;

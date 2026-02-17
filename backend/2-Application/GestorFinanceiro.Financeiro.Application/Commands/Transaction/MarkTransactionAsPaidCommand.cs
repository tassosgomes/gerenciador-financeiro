using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Commands.Transaction;

public record MarkTransactionAsPaidCommand(
    Guid TransactionId,
    string UserId,
    string? OperationId = null
) : ICommand<TransactionResponse>;

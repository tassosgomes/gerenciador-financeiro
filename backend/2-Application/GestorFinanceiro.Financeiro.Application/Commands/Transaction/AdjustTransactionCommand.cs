using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Commands.Transaction;

public record AdjustTransactionCommand(
    Guid TransactionId,
    decimal CorrectAmount,
    string UserId,
    string? OperationId = null
) : ICommand<TransactionResponse>;
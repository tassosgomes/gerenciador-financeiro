using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Commands.Transaction;

public record CancelTransactionCommand(
    Guid TransactionId,
    string UserId,
    string? Reason = null,
    string? OperationId = null
) : ICommand<Unit>;
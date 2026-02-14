using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Commands.Transaction;

public record CreateTransactionCommand(
    Guid AccountId,
    Guid CategoryId,
    TransactionType Type,
    decimal Amount,
    string Description,
    DateTime CompetenceDate,
    DateTime? DueDate,
    TransactionStatus Status,
    string UserId,
    string? OperationId = null
) : ICommand<TransactionResponse>;
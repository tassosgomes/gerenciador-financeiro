using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Commands.Transfer;

public record CreateTransferCommand(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    Guid CategoryId,
    decimal Amount,
    string Description,
    DateTime CompetenceDate,
    string UserId,
    string? OperationId = null) : ICommand<IReadOnlyList<TransactionResponse>>;
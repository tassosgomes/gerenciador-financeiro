using GestorFinanceiro.Financeiro.Application.Common;

namespace GestorFinanceiro.Financeiro.Application.Commands.Transfer;

public record CancelTransferCommand(
    Guid TransferGroupId,
    string UserId,
    string? Reason = null,
    string? OperationId = null) : ICommand<Unit>;
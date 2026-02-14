using GestorFinanceiro.Financeiro.Application.Common;

namespace GestorFinanceiro.Financeiro.Application.Commands.Installment;

public record CancelInstallmentGroupCommand(
    Guid GroupId,
    string UserId,
    string? Reason = null,
    string? OperationId = null) : ICommand<Unit>;
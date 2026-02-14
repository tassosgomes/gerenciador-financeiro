using GestorFinanceiro.Financeiro.Application.Common;

namespace GestorFinanceiro.Financeiro.Application.Commands.Installment;

public record CancelInstallmentCommand(
    Guid InstallmentId,
    string UserId,
    string? Reason = null,
    string? OperationId = null) : ICommand<Unit>;
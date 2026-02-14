using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Commands.Installment;

public record CancelInstallmentGroupCommand(
    Guid GroupId,
    string UserId,
    string? Reason = null,
    string? OperationId = null) : ICommand<IReadOnlyList<TransactionResponse>>;

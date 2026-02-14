using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Commands.Installment;

public record AdjustInstallmentGroupCommand(
    Guid GroupId,
    decimal NewTotalAmount,
    string UserId,
    string? OperationId = null) : ICommand<IReadOnlyList<TransactionResponse>>;
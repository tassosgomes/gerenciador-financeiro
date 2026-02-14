using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Commands.Installment;

public record CreateInstallmentCommand(
    Guid AccountId,
    Guid CategoryId,
    TransactionType Type,
    decimal TotalAmount,
    int InstallmentCount,
    string Description,
    DateTime FirstCompetenceDate,
    DateTime FirstDueDate,
    string UserId,
    string? OperationId = null) : ICommand<IReadOnlyList<TransactionResponse>>;
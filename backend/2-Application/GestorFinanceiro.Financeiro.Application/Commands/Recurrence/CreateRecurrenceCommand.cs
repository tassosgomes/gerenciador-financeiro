using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Commands.Recurrence;

public record CreateRecurrenceCommand(
    Guid AccountId,
    Guid CategoryId,
    TransactionType Type,
    decimal Amount,
    string Description,
    int DayOfMonth,
    TransactionStatus DefaultStatus,
    string UserId,
    string? OperationId = null) : ICommand<RecurrenceTemplateResponse>;
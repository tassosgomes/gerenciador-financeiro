using GestorFinanceiro.Financeiro.Application.Common;

namespace GestorFinanceiro.Financeiro.Application.Commands.Recurrence;

public record GenerateRecurrenceCommand(
    Guid RecurrenceId,
    DateTime ReferenceDate,
    string UserId,
    string? OperationId = null) : ICommand<Unit>;
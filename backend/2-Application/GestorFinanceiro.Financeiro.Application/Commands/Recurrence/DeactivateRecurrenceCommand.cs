using GestorFinanceiro.Financeiro.Application.Common;

namespace GestorFinanceiro.Financeiro.Application.Commands.Recurrence;

public record DeactivateRecurrenceCommand(
    Guid RecurrenceId,
    string UserId,
    string? OperationId = null) : ICommand<Unit>;
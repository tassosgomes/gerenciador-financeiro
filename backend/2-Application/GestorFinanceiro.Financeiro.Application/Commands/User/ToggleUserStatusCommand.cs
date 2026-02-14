using GestorFinanceiro.Financeiro.Application.Common;

namespace GestorFinanceiro.Financeiro.Application.Commands.User;

public record ToggleUserStatusCommand(
    Guid UserId,
    bool IsActive,
    string UpdatedByUserId) : ICommand<Unit>;

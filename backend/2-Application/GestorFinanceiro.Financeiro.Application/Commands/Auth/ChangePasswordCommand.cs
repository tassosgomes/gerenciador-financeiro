using GestorFinanceiro.Financeiro.Application.Common;

namespace GestorFinanceiro.Financeiro.Application.Commands.Auth;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : ICommand<Unit>;

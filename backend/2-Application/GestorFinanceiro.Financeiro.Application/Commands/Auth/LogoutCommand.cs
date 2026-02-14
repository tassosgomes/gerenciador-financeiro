using GestorFinanceiro.Financeiro.Application.Common;

namespace GestorFinanceiro.Financeiro.Application.Commands.Auth;

public record LogoutCommand(Guid UserId) : ICommand<Unit>;

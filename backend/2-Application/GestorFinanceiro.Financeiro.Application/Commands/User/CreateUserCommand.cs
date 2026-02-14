using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Commands.User;

public record CreateUserCommand(
    string Name,
    string Email,
    string Password,
    string Role,
    string CreatedByUserId) : ICommand<UserResponse>;

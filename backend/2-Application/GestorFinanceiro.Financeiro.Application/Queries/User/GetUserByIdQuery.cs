using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Queries.User;

public record GetUserByIdQuery(Guid UserId) : IQuery<UserResponse>;

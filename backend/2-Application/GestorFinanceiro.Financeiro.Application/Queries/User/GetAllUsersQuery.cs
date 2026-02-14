using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Queries.User;

public record GetAllUsersQuery() : IQuery<IEnumerable<UserResponse>>;

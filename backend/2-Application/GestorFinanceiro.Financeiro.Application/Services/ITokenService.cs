using GestorFinanceiro.Financeiro.Domain.Entity;
using System.Security.Claims;

namespace GestorFinanceiro.Financeiro.Application.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(Guid userId);
    ClaimsPrincipal? ValidateAccessToken(string token);
}

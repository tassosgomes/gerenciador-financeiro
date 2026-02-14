using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Commands.Auth;

// Intentionally returns AuthResponse for refresh to keep the auth contract aligned with login.
public record RefreshTokenCommand(string RefreshToken) : ICommand<AuthResponse>;

namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserResponse User
);

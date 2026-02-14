namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    bool MustChangePassword,
    DateTime CreatedAt
);

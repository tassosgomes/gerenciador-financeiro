namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class InvalidRefreshTokenException : DomainException
{
    public InvalidRefreshTokenException()
        : base("The refresh token is invalid, expired, or has been revoked.")
    {
    }
}

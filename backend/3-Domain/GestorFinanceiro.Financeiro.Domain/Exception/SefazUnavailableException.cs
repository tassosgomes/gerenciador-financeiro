namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class SefazUnavailableException : DomainException
{
    public SefazUnavailableException()
        : base("SEFAZ is currently unavailable. Please try again later.")
    {
    }

    public SefazUnavailableException(System.Exception innerException)
        : base("SEFAZ is currently unavailable. Please try again later.", innerException)
    {
    }
}

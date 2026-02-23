namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class SefazParsingException : DomainException
{
    public SefazParsingException()
        : base("Unable to parse NFC-e data returned by SEFAZ.")
    {
    }

    public SefazParsingException(System.Exception innerException)
        : base("Unable to parse NFC-e data returned by SEFAZ.", innerException)
    {
    }
}

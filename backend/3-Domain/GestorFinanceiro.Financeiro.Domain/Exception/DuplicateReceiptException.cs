namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class DuplicateReceiptException : DomainException
{
    public DuplicateReceiptException(string accessKey)
        : base($"Receipt with access key '{accessKey}' has already been imported.")
    {
    }
}

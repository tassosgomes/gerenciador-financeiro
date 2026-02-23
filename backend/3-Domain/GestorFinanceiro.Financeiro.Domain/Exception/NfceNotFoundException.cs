namespace GestorFinanceiro.Financeiro.Domain.Exception;

public class NfceNotFoundException : DomainException
{
    public NfceNotFoundException(string accessKey)
        : base($"NFC-e with access key '{accessKey}' was not found in SEFAZ.")
    {
    }
}

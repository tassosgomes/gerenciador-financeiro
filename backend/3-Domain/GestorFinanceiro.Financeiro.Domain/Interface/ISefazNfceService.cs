using GestorFinanceiro.Financeiro.Domain.Dto;

namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface ISefazNfceService
{
    Task<NfceData> LookupAsync(string accessKey, CancellationToken cancellationToken);
}

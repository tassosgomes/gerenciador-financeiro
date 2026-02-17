namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface ISystemRepository
{
    Task ResetSystemDataAsync(CancellationToken cancellationToken);
}

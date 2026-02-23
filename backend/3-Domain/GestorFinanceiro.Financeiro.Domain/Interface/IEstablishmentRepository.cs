using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface IEstablishmentRepository : IRepository<Establishment>
{
    Task<Establishment> AddAsync(Establishment entity, CancellationToken cancellationToken);
    Task<Establishment?> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken);
    void Remove(Establishment entity);
    Task<bool> ExistsByAccessKeyAsync(string accessKey, CancellationToken cancellationToken);
}

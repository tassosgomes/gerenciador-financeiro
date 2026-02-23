using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanceiro.Financeiro.Infra.Repository;

public class EstablishmentRepository : Repository<Establishment>, IEstablishmentRepository
{
    public EstablishmentRepository(FinanceiroDbContext context)
        : base(context)
    {
    }

    public override async Task<Establishment> AddAsync(Establishment entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _context.Establishments.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task<Establishment?> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        return await _context.Establishments
            .AsNoTracking()
            .FirstOrDefaultAsync(establishment => establishment.TransactionId == transactionId, cancellationToken);
    }

    public void Remove(Establishment entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _context.Establishments.Remove(entity);
    }

    public async Task<bool> ExistsByAccessKeyAsync(string accessKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessKey);

        return await _context.Establishments
            .AsNoTracking()
            .AnyAsync(establishment => establishment.AccessKey == accessKey, cancellationToken);
    }
}

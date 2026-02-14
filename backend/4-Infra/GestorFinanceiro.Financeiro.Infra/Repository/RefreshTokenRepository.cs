using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanceiro.Financeiro.Infra.Repository;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly FinanceiroDbContext _context;

    public RefreshTokenRepository(FinanceiroDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return await _context.RefreshTokens
            .SingleOrDefaultAsync(refreshToken => refreshToken.Token == token, cancellationToken);
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);

        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public async Task RevokeByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        await _context.RefreshTokens
            .Where(refreshToken => refreshToken.UserId == userId && !refreshToken.IsRevoked)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(refreshToken => refreshToken.IsRevoked, true)
                    .SetProperty(refreshToken => refreshToken.RevokedAt, utcNow),
                cancellationToken);
    }

    public async Task CleanupExpiredAsync(CancellationToken cancellationToken)
    {
        await _context.RefreshTokens
            .Where(refreshToken => refreshToken.ExpiresAt < DateTime.UtcNow)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

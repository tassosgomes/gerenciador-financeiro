using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.Domain.Interface;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
    Task RevokeByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task CleanupExpiredAsync(CancellationToken cancellationToken);
}

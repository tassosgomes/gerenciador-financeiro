using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Infra.StartupTasks;

public sealed class MigrateDatabaseStartupTask : IStartupTask
{
    private readonly FinanceiroDbContext _dbContext;
    private readonly ILogger<MigrateDatabaseStartupTask> _logger;

    public MigrateDatabaseStartupTask(
        FinanceiroDbContext dbContext,
        ILogger<MigrateDatabaseStartupTask> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting database migrations");

        try
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Database migrations completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply database migrations");
            throw;
        }
    }
}

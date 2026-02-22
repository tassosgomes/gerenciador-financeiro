using GestorFinanceiro.Financeiro.Application.Services;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Audit;
using GestorFinanceiro.Financeiro.Infra.Auth;
using GestorFinanceiro.Financeiro.Infra.Context;
using GestorFinanceiro.Financeiro.Infra.Repository;
using GestorFinanceiro.Financeiro.Infra.StartupTasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InfraUnitOfWork = GestorFinanceiro.Financeiro.Infra.UnitOfWork.UnitOfWork;

namespace GestorFinanceiro.Financeiro.Infra.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<FinanceiroDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, InfraUnitOfWork>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IBackupRepository, BackupRepository>();
        services.AddScoped<IRecurrenceTemplateRepository, RecurrenceTemplateRepository>();
        services.AddScoped<IOperationLogRepository, OperationLogRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<ISystemRepository, SystemRepository>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        // Startup tasks - order matters
        services.AddScoped<IStartupTask, MigrateDatabaseStartupTask>();
        services.AddScoped<IStartupTask, SeedAdminUserStartupTask>();
        services.AddScoped<IStartupTask, SeedInvoicePaymentCategoryStartupTask>();
        services.AddHostedService<StartupTasksHostedService>();
        services.AddHostedService<RecurrenceMaintenanceWorker>();

        return services;
    }
}

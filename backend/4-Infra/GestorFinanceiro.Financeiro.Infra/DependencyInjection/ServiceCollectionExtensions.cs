using GestorFinanceiro.Financeiro.Application.Services;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.Audit;
using GestorFinanceiro.Financeiro.Infra.Auth;
using GestorFinanceiro.Financeiro.Infra.Context;
using GestorFinanceiro.Financeiro.Infra.Repository;
using GestorFinanceiro.Financeiro.Infra.Services;
using GestorFinanceiro.Financeiro.Infra.StartupTasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
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
        services.AddScoped<IReceiptItemRepository, ReceiptItemRepository>();
        services.AddScoped<IEstablishmentRepository, EstablishmentRepository>();
        services.Configure<SefazSettings>(configuration.GetSection("Sefaz"));
        services.AddHttpClient("SefazPb")
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var settings = serviceProvider.GetRequiredService<IOptions<SefazSettings>>().Value;
                var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
                    ? "https://www.sefaz.pb.gov.br/nfce/consulta"
                    : settings.BaseUrl;

                client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/");
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds <= 0 ? 15 : settings.TimeoutSeconds);

                if (!string.IsNullOrWhiteSpace(settings.UserAgent))
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
                }
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
                options.Retry.UseJitter = false;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
            });
        services.AddScoped<ISefazNfceService>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<SefazPbNfceService>>();
            return new SefazPbNfceService(httpClientFactory.CreateClient("SefazPb"), logger);
        });
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
        services.AddHostedService<BudgetRecurrenceWorker>();

        return services;
    }
}

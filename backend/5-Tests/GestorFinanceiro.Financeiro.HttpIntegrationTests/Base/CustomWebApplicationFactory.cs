using GestorFinanceiro.Financeiro.HttpIntegrationTests.Seed;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Service;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Base;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgreSqlFixture = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgreSqlFixture.ConnectionString,
                ["AdminSeed:Name"] = "Admin Test",
                ["AdminSeed:Email"] = TestDataSeeder.AdminEmail,
                ["AdminSeed:Password"] = TestDataSeeder.AdminPassword,
                ["CorsSettings:AllowedOrigins:0"] = "http://localhost"
            };

            configurationBuilder.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<FinanceiroDbContext>));
            services.AddDbContext<FinanceiroDbContext>(options =>
                options.UseNpgsql(_postgreSqlFixture.ConnectionString));

            services.AddScoped<TransactionDomainService>();
            services.AddScoped<InstallmentDomainService>();
            services.AddScoped<RecurrenceDomainService>();
            services.AddScoped<TransferDomainService>();
            services.RemoveAll(typeof(IAuditService));
            services.AddScoped<IAuditService, NoOpAuditService>();
        });
    }

    public async Task InitializeAsync()
    {
        if (!DockerAvailabilityChecker.IsDockerAvailable)
        {
            return;
        }

        await _postgreSqlFixture.InitializeAsync();
        await EnsureDatabaseReadyAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Dispose();
        await _postgreSqlFixture.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        if (!DockerAvailabilityChecker.IsDockerAvailable)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(_postgreSqlFixture.ConnectionString);
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE EXTENSION IF NOT EXISTS pgcrypto;";
            await command.ExecuteNonQueryAsync();
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                TRUNCATE TABLE
                    refresh_tokens,
                    transactions,
                    recurrence_templates,
                    operation_logs,
                    audit_logs,
                    accounts,
                    categories,
                    users
                RESTART IDENTITY CASCADE;
                """;

            await command.ExecuteNonQueryAsync();
        }

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        await TestDataSeeder.SeedAsync(dbContext);
    }

    private async Task EnsureDatabaseReadyAsync()
    {
        await using var connection = new NpgsqlConnection(_postgreSqlFixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE EXTENSION IF NOT EXISTS pgcrypto;";
        await command.ExecuteNonQueryAsync();

        var dbContextOptions = new DbContextOptionsBuilder<FinanceiroDbContext>()
            .UseNpgsql(_postgreSqlFixture.ConnectionString)
            .Options;

        await using (var dbContext = new FinanceiroDbContext(dbContextOptions))
        {
            await dbContext.Database.MigrateAsync();
        }

        using var scope = Services.CreateScope();
        var scopedDbContext = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        await TestDataSeeder.SeedAsync(scopedDbContext);
    }
}

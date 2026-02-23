using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Infra.Context;
using GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GestorFinanceiro.Financeiro.IntegrationTests.Base;

public abstract class IntegrationTestBase : IAsyncLifetime, IAsyncDisposable
{
    private readonly DbContextOptions<FinanceiroDbContext> _options;

    protected IntegrationTestBase(PostgreSqlFixture fixture)
    {
        Fixture = fixture;
        _options = new DbContextOptionsBuilder<FinanceiroDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        DbContext = new FinanceiroDbContext(_options);
    }

    protected PostgreSqlFixture Fixture { get; }

    protected FinanceiroDbContext DbContext { get; }

    protected FinanceiroDbContext CreateDbContext()
    {
        return new FinanceiroDbContext(_options);
    }

    public virtual async Task InitializeAsync()
    {
        if (!DockerAvailabilityChecker.IsDockerAvailable)
        {
            return;
        }

        var cancellationToken = CancellationToken.None;
        // Known pre-existing setup dependency: migrations require pgcrypto extension in PostgreSQL.
        await DbContext.Database.MigrateAsync(cancellationToken);
        await CleanDatabaseAsync(cancellationToken);
    }

    public virtual async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
    }

    public async ValueTask DisposeAsyncCore()
    {
        await DbContext.DisposeAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsyncCore();
    }

    protected async Task CleanDatabaseAsync(CancellationToken cancellationToken)
    {
        const string truncateCoreSql = """
            TRUNCATE TABLE
                transactions,
                recurrence_templates,
                operation_logs,
                accounts,
                categories
            RESTART IDENTITY CASCADE;
            """;

        await DbContext.Database.ExecuteSqlRawAsync(truncateCoreSql, cancellationToken);

        const string truncateBudgetSql = """
            TRUNCATE TABLE
                budget_categories,
                budgets
            RESTART IDENTITY CASCADE;
            """;

        try
        {
            await DbContext.Database.ExecuteSqlRawAsync(truncateBudgetSql, cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UndefinedTable)
        {
        }
    }

    protected async Task<Account> CreateAccountAsync(
        string name,
        decimal initialBalance,
        bool allowNegativeBalance,
        CancellationToken cancellationToken)
    {
        var account = Account.Create(name, AccountType.Corrente, initialBalance, allowNegativeBalance, "integration-user");
        await DbContext.Accounts.AddAsync(account, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    protected async Task<Category> CreateCategoryAsync(
        string name,
        CategoryType type,
        CancellationToken cancellationToken)
    {
        var category = Category.Create(name, type, "integration-user");
        await DbContext.Categories.AddAsync(category, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
        return category;
    }
}

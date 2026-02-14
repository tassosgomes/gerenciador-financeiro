using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Infra.Repository;
using GestorFinanceiro.Financeiro.IntegrationTests.Base;
using GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using InfraUnitOfWork = GestorFinanceiro.Financeiro.Infra.UnitOfWork.UnitOfWork;

namespace GestorFinanceiro.Financeiro.IntegrationTests.UnitOfWork;

[Collection(PostgreSqlCollection.Name)]
public sealed class UnitOfWorkTests : IntegrationTestBase
{
    public UnitOfWorkTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [DockerAvailableFact]
    public async Task Migrations_AplicamCorretamente_SchemaCriado()
    {
        var cancellationToken = CancellationToken.None;

        var requiredTables = new[] { "accounts", "categories", "transactions", "recurrence_templates", "operation_logs" };

        foreach (var tableName in requiredTables)
        {
            var exists = await DbContext.Database.SqlQuery<int>($"""
                SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM pg_catalog.pg_tables
                    WHERE schemaname = current_schema()
                      AND tablename = {tableName}
                ) THEN 1 ELSE 0 END AS "Value"
                """).SingleAsync(cancellationToken);

            exists.Should().Be(1);
        }
    }

    [DockerAvailableFact]
    public async Task UnitOfWork_CommitAposOperacao_DadosPersistidos()
    {
        var cancellationToken = CancellationToken.None;
        var accountName = $"Conta-Commit-{Guid.NewGuid()}";

        await using (var writeContext = CreateDbContext())
        {
            var repository = new AccountRepository(writeContext);
            using var unitOfWork = new InfraUnitOfWork(writeContext);

            var account = Domain.Entity.Account.Create(accountName, AccountType.Corrente, 120m, false, "integration-user");

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await repository.AddAsync(account, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        await using var readContext = CreateDbContext();
        var persistedAccount = await readContext.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.Name == accountName, cancellationToken);

        persistedAccount.Should().NotBeNull();
        persistedAccount!.Balance.Should().Be(120m);
    }

    [DockerAvailableFact]
    public async Task UnitOfWork_RollbackAposExcecao_DadosNaoPersistidos()
    {
        var cancellationToken = CancellationToken.None;
        var account = await CreateAccountAsync($"Conta-Rollback-{Guid.NewGuid()}", 100m, false, cancellationToken);

        await using (var writeContext = CreateDbContext())
        {
            var repository = new AccountRepository(writeContext);
            using var unitOfWork = new InfraUnitOfWork(writeContext);

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            var persistedAccount = await repository.GetByIdWithLockAsync(account.Id, cancellationToken);

            persistedAccount.ApplyCredit(50m, "integration-user");
            repository.Update(persistedAccount);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.RollbackAsync(cancellationToken);
        }

        await using var readContext = CreateDbContext();
        var reloadedAccount = await readContext.Accounts
            .AsNoTracking()
            .SingleAsync(value => value.Id == account.Id, cancellationToken);

        reloadedAccount.Balance.Should().Be(100m);
    }
}

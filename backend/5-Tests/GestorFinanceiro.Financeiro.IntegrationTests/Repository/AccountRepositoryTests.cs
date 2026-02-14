using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Infra.Repository;
using GestorFinanceiro.Financeiro.IntegrationTests.Base;
using GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;
using InfraUnitOfWork = GestorFinanceiro.Financeiro.Infra.UnitOfWork.UnitOfWork;

namespace GestorFinanceiro.Financeiro.IntegrationTests.Repository;

[Collection(PostgreSqlCollection.Name)]
public sealed class AccountRepositoryTests : IntegrationTestBase
{
    public AccountRepositoryTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [DockerAvailableFact]
    public async Task AccountRepository_AddAndGetById_PersistERecuperaCorretamente()
    {
        var cancellationToken = CancellationToken.None;
        var repository = new AccountRepository(DbContext);
        var account = Domain.Entity.Account.Create($"Conta-{Guid.NewGuid()}", Domain.Enum.AccountType.Corrente, 150m, false, "integration-user");

        await repository.AddAsync(account, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        var persistedAccount = await repository.GetByIdAsync(account.Id, cancellationToken);

        persistedAccount.Should().NotBeNull();
        persistedAccount!.Id.Should().Be(account.Id);
        persistedAccount.Name.Should().Be(account.Name);
        persistedAccount.Balance.Should().Be(150m);
    }

    [DockerAvailableFact]
    public async Task AccountRepository_GetByIdWithLock_RetornaContaComLock()
    {
        var cancellationToken = CancellationToken.None;
        var account = await CreateAccountAsync($"Conta-Lock-{Guid.NewGuid()}", 200m, false, cancellationToken);
        var repository = new AccountRepository(DbContext);
        using var unitOfWork = new InfraUnitOfWork(DbContext);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var lockedAccount = await repository.GetByIdWithLockAsync(account.Id, cancellationToken);

        lockedAccount.Id.Should().Be(account.Id);
        DbContext.Database.CurrentTransaction.Should().NotBeNull();

        await unitOfWork.CommitAsync(cancellationToken);
    }
}

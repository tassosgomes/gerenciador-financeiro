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

    [DockerAvailableFact]
    public async Task GetActiveByTypeAsync_WithMatchingType_ShouldReturnActiveAccounts()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var correnteAccount1 = Domain.Entity.Account.Create($"Corrente-1-{Guid.NewGuid()}", Domain.Enum.AccountType.Corrente, 100m, false, "integration-user");
        var correnteAccount2 = Domain.Entity.Account.Create($"Corrente-2-{Guid.NewGuid()}", Domain.Enum.AccountType.Corrente, 200m, true, "integration-user");
        var carteiraAccount = Domain.Entity.Account.Create($"Carteira-{Guid.NewGuid()}", Domain.Enum.AccountType.Carteira, 50m, false, "integration-user");

        await DbContext.Accounts.AddRangeAsync(new[] { correnteAccount1, correnteAccount2, carteiraAccount }, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        var repository = new AccountRepository(DbContext);

        // Act
        var result = await repository.GetActiveByTypeAsync(Domain.Enum.AccountType.Corrente, cancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.Type == Domain.Enum.AccountType.Corrente);
        result.Should().OnlyContain(a => a.IsActive);
        result.Should().BeInAscendingOrder(a => a.Name);
    }

    [DockerAvailableFact]
    public async Task GetActiveByTypeAsync_WithInactiveAccounts_ShouldExclude()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var activeAccount = Domain.Entity.Account.Create($"Active-{Guid.NewGuid()}", Domain.Enum.AccountType.Corrente, 100m, false, "integration-user");
        var inactiveAccount = Domain.Entity.Account.Create($"Inactive-{Guid.NewGuid()}", Domain.Enum.AccountType.Corrente, 200m, false, "integration-user");
        inactiveAccount.Deactivate("integration-user");

        await DbContext.Accounts.AddRangeAsync(new[] { activeAccount, inactiveAccount }, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        var repository = new AccountRepository(DbContext);

        // Act
        var result = await repository.GetActiveByTypeAsync(Domain.Enum.AccountType.Corrente, cancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(a => a.Id == activeAccount.Id);
    }

    [DockerAvailableFact]
    public async Task GetActiveByTypeAsync_WithNoMatchingAccounts_ShouldReturnEmpty()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var correnteAccount = Domain.Entity.Account.Create($"Corrente-{Guid.NewGuid()}", Domain.Enum.AccountType.Corrente, 100m, false, "integration-user");

        await DbContext.Accounts.AddAsync(correnteAccount, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        var repository = new AccountRepository(DbContext);

        // Act
        var result = await repository.GetActiveByTypeAsync(Domain.Enum.AccountType.Carteira, cancellationToken);

        // Assert
        result.Should().BeEmpty();
    }
}

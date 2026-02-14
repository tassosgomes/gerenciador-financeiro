using System.Diagnostics;
using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Infra.Repository;
using GestorFinanceiro.Financeiro.IntegrationTests.Base;
using GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;
using InfraUnitOfWork = GestorFinanceiro.Financeiro.Infra.UnitOfWork.UnitOfWork;

namespace GestorFinanceiro.Financeiro.IntegrationTests.Concurrency;

[Collection(PostgreSqlCollection.Name)]
public sealed class SelectForUpdateTests : IntegrationTestBase
{
    public SelectForUpdateTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [DockerAvailableFact]
    public async Task SelectForUpdate_DuasOperacoesParalelas_SegundaEsperaPrimeiraTerminar()
    {
        var cancellationToken = CancellationToken.None;
        var account = await CreateAccountAsync($"Conta-Concurrency-{Guid.NewGuid()}", 500m, false, cancellationToken);
        var lockDuration = TimeSpan.FromMilliseconds(500);
        var expectedMinimumElapsed = TimeSpan.FromMilliseconds(900);

        var stopwatch = Stopwatch.StartNew();

        var task1 = Task.Run(
            () => ExecuteLockedOperationAsync(account.Id, "integration-user-1", lockDuration, cancellationToken),
            cancellationToken);
        var task2 = Task.Run(
            () => ExecuteLockedOperationAsync(account.Id, "integration-user-2", lockDuration, cancellationToken),
            cancellationToken);

        await Task.WhenAll(task1, task2);

        stopwatch.Stop();
        stopwatch.Elapsed.Should().BeGreaterThanOrEqualTo(expectedMinimumElapsed);
    }

    private async Task ExecuteLockedOperationAsync(
        Guid accountId,
        string userId,
        TimeSpan processingDelay,
        CancellationToken cancellationToken)
    {
        await using var context = CreateDbContext();
        var repository = new AccountRepository(context);
        using var unitOfWork = new InfraUnitOfWork(context);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var account = await repository.GetByIdWithLockAsync(accountId, cancellationToken);

        await Task.Delay(processingDelay, cancellationToken);

        account.ApplyCredit(5m, userId);
        repository.Update(account);

        await unitOfWork.CommitAsync(cancellationToken);
    }
}

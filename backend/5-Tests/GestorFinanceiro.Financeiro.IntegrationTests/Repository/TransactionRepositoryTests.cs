using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Infra.Repository;
using GestorFinanceiro.Financeiro.IntegrationTests.Base;
using GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;

namespace GestorFinanceiro.Financeiro.IntegrationTests.Repository;

[Collection(PostgreSqlCollection.Name)]
public sealed class TransactionRepositoryTests : IntegrationTestBase
{
    public TransactionRepositoryTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [DockerAvailableFact]
    public async Task TransactionRepository_AddAndGetByInstallmentGroup_RetornaParcelas()
    {
        var cancellationToken = CancellationToken.None;
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 1000m, false, cancellationToken);
        var category = await CreateCategoryAsync($"Categoria-{Guid.NewGuid()}", CategoryType.Despesa, cancellationToken);
        var installmentGroupId = Guid.NewGuid();
        var repository = new TransactionRepository(DbContext);

        var firstInstallment = Transaction.Create(
            account.Id,
            category.Id,
            TransactionType.Debit,
            100m,
            "Parcela 1",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            TransactionStatus.Pending,
            "integration-user");
        firstInstallment.SetInstallmentInfo(installmentGroupId, 1, 2);

        var secondInstallment = Transaction.Create(
            account.Id,
            category.Id,
            TransactionType.Debit,
            100m,
            "Parcela 2",
            DateTime.UtcNow.Date.AddMonths(1),
            DateTime.UtcNow.Date.AddMonths(1),
            TransactionStatus.Pending,
            "integration-user");
        secondInstallment.SetInstallmentInfo(installmentGroupId, 2, 2);

        await repository.AddAsync(firstInstallment, cancellationToken);
        await repository.AddAsync(secondInstallment, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        var installments = (await repository.GetByInstallmentGroupAsync(installmentGroupId, cancellationToken)).ToList();

        installments.Should().HaveCount(2);
        installments[0].InstallmentNumber.Should().Be(1);
        installments[1].InstallmentNumber.Should().Be(2);
    }

    [DockerAvailableFact]
    public async Task TransactionRepository_GetByOperationId_RetornaTransacaoCorreta()
    {
        var cancellationToken = CancellationToken.None;
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 300m, false, cancellationToken);
        var category = await CreateCategoryAsync($"Categoria-{Guid.NewGuid()}", CategoryType.Receita, cancellationToken);
        var repository = new TransactionRepository(DbContext);
        var operationId = $"op-{Guid.NewGuid()}";

        var transaction = Transaction.Create(
            account.Id,
            category.Id,
            TransactionType.Credit,
            90m,
            "Credito",
            DateTime.UtcNow.Date,
            null,
            TransactionStatus.Paid,
            "integration-user",
            operationId);

        await repository.AddAsync(transaction, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        var persistedTransaction = await repository.GetByOperationIdAsync(operationId, cancellationToken);

        persistedTransaction.Should().NotBeNull();
        persistedTransaction!.Id.Should().Be(transaction.Id);
        persistedTransaction.OperationId.Should().Be(operationId);
    }
}

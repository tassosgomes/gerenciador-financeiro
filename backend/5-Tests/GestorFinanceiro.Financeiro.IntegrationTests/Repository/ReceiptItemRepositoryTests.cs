using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Infra.Repository;
using GestorFinanceiro.Financeiro.IntegrationTests.Base;
using GestorFinanceiro.Financeiro.IntegrationTests.Fixtures;

namespace GestorFinanceiro.Financeiro.IntegrationTests.Repository;

[Collection(PostgreSqlCollection.Name)]
public sealed class ReceiptItemRepositoryTests : IntegrationTestBase
{
    public ReceiptItemRepositoryTests(PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    [DockerAvailableFact]
    public async Task ReceiptItemRepository_AddRangeAndGetByTransactionId_RetornaOrdenadoPorItemOrder()
    {
        var cancellationToken = CancellationToken.None;
        var transaction = await CreatePaidTransactionAsync(cancellationToken);
        var repository = new ReceiptItemRepository(DbContext);

        var items = new[]
        {
            ReceiptItem.Create(transaction.Id, "Item 2", "P2", 1m, "UN", 10m, 10m, 2, "integration-user"),
            ReceiptItem.Create(transaction.Id, "Item 1", "P1", 1m, "UN", 5m, 5m, 1, "integration-user"),
            ReceiptItem.Create(transaction.Id, "Item 3", "P3", 2m, "UN", 7m, 14m, 3, "integration-user"),
        };

        await repository.AddRangeAsync(items, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        var persistedItems = await repository.GetByTransactionIdAsync(transaction.Id, cancellationToken);

        persistedItems.Should().HaveCount(3);
        persistedItems.Select(receiptItem => receiptItem.ItemOrder).Should().Equal(1, 2, 3);
    }

    [DockerAvailableFact]
    public async Task ReceiptItemRepository_RemoveRange_RemoveTodosOsItensDaTransacao()
    {
        var cancellationToken = CancellationToken.None;
        var transaction = await CreatePaidTransactionAsync(cancellationToken);
        var repository = new ReceiptItemRepository(DbContext);

        var items = new[]
        {
            ReceiptItem.Create(transaction.Id, "Item A", "PA", 1m, "UN", 3m, 3m, 1, "integration-user"),
            ReceiptItem.Create(transaction.Id, "Item B", "PB", 1m, "UN", 4m, 4m, 2, "integration-user"),
        };

        await repository.AddRangeAsync(items, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        repository.RemoveRange(items);
        await DbContext.SaveChangesAsync(cancellationToken);

        var remainingItems = await repository.GetByTransactionIdAsync(transaction.Id, cancellationToken);
        remainingItems.Should().BeEmpty();
    }

    private async Task<Transaction> CreatePaidTransactionAsync(CancellationToken cancellationToken)
    {
        var account = await CreateAccountAsync($"Conta-{Guid.NewGuid()}", 1000m, false, cancellationToken);
        var category = await CreateCategoryAsync($"Categoria-{Guid.NewGuid()}", CategoryType.Despesa, cancellationToken);

        var transaction = Transaction.Create(
            account.Id,
            category.Id,
            TransactionType.Debit,
            100m,
            "Compra de teste",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            TransactionStatus.Paid,
            "integration-user");

        await DbContext.Transactions.AddAsync(transaction, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        return transaction;
    }
}

using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Queries.Transaction;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Infra.Context;
using GestorFinanceiro.Financeiro.Infra.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application;

public class GetTransactionHistoryQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_TransactionWithoutAdjustments_ShouldReturnOnlyOriginal()
    {
        await using var context = CreateContext();
        var originalTransaction = CreateTransaction(TransactionStatus.Paid);
        await SeedAsync(context, originalTransaction);

        var handler = CreateHandler(context);
        var query = new GetTransactionHistoryQuery(originalTransaction.Id);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Entries.Should().HaveCount(1);
        result.Entries[0].Transaction.Id.Should().Be(originalTransaction.Id);
        result.Entries[0].ActionType.Should().Be("Original");
    }

    [Fact]
    public async Task HandleAsync_TransactionWithTwoAdjustments_ShouldReturnChronologicalHistory()
    {
        await using var context = CreateContext();

        var originalTransaction = CreateTransaction(TransactionStatus.Paid);
        var adjustmentOne = Transaction.CreateAdjustment(
            originalTransaction.AccountId,
            originalTransaction.CategoryId,
            originalTransaction.Type,
            10m,
            originalTransaction.Id,
            "Adjustment 1",
            originalTransaction.CompetenceDate.AddDays(1),
            "user-1");

        var adjustmentTwo = Transaction.CreateAdjustment(
            originalTransaction.AccountId,
            originalTransaction.CategoryId,
            originalTransaction.Type,
            5m,
            originalTransaction.Id,
            "Adjustment 2",
            originalTransaction.CompetenceDate.AddDays(2),
            "user-1");

        await SeedAsync(context, originalTransaction, adjustmentOne, adjustmentTwo);

        var handler = CreateHandler(context);
        var query = new GetTransactionHistoryQuery(adjustmentTwo.Id);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Entries.Should().HaveCount(3);
        result.Entries[0].Transaction.Id.Should().Be(originalTransaction.Id);
        result.Entries[0].ActionType.Should().Be("Original");
        result.Entries[1].ActionType.Should().Be("Adjustment");
        result.Entries[2].ActionType.Should().Be("Adjustment");
    }

    [Fact]
    public async Task HandleAsync_CancelledTransaction_ShouldMarkEntryAsCancellation()
    {
        await using var context = CreateContext();
        var transaction = CreateTransaction(TransactionStatus.Pending);
        transaction.Cancel("user-1", "Cancelled for test");
        await SeedAsync(context, transaction);

        var handler = CreateHandler(context);
        var query = new GetTransactionHistoryQuery(transaction.Id);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Entries.Should().HaveCount(1);
        result.Entries[0].ActionType.Should().Be("Cancellation");
    }

    private static GetTransactionHistoryQueryHandler CreateHandler(FinanceiroDbContext context)
    {
        var repository = new TransactionRepository(context);
        var logger = new Mock<ILogger<GetTransactionHistoryQueryHandler>>();
        return new GetTransactionHistoryQueryHandler(repository, logger.Object);
    }

    private static FinanceiroDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinanceiroDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FinanceiroDbContext(options);
    }

    private static async Task SeedAsync(FinanceiroDbContext context, params Transaction[] transactions)
    {
        await context.Transactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();
    }

    private static Transaction CreateTransaction(TransactionStatus status)
    {
        return Transaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            100m,
            "Transaction",
            DateTime.UtcNow,
            DateTime.UtcNow,
            status,
            "user-1");
    }
}

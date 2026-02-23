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

public class ListTransactionsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithoutFilters_ShouldReturnPagedResult()
    {
        await using var context = CreateContext();
        var accountId = Guid.NewGuid();
        await SeedAsync(context,
            CreateTransaction(accountId, TransactionStatus.Paid, new DateTime(2026, 1, 1)),
            CreateTransaction(accountId, TransactionStatus.Pending, new DateTime(2026, 1, 2)),
            CreateTransaction(Guid.NewGuid(), TransactionStatus.Pending, new DateTime(2026, 1, 3)));

        var handler = CreateHandler(context);
        var query = new ListTransactionsQuery(null, null, null, null, null, null, null, null, 1, 2);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Data.Should().HaveCount(2);
        result.Pagination.Page.Should().Be(1);
        result.Pagination.Size.Should().Be(2);
        result.Pagination.Total.Should().Be(3);
        result.Pagination.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_WithAccountFilter_ShouldReturnOnlyAccountTransactions()
    {
        await using var context = CreateContext();
        var targetAccountId = Guid.NewGuid();
        await SeedAsync(context,
            CreateTransaction(targetAccountId, TransactionStatus.Paid, new DateTime(2026, 1, 1)),
            CreateTransaction(targetAccountId, TransactionStatus.Pending, new DateTime(2026, 1, 2)),
            CreateTransaction(Guid.NewGuid(), TransactionStatus.Pending, new DateTime(2026, 1, 3)));

        var handler = CreateHandler(context);
        var query = new ListTransactionsQuery(targetAccountId, null, null, null, null, null, null, null, 1, 20);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Data.Should().OnlyContain(transaction => transaction.AccountId == targetAccountId);
        result.Pagination.Total.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_WithCompetenceDateRange_ShouldFilterByPeriod()
    {
        await using var context = CreateContext();
        await SeedAsync(context,
            CreateTransaction(Guid.NewGuid(), TransactionStatus.Paid, new DateTime(2026, 1, 5)),
            CreateTransaction(Guid.NewGuid(), TransactionStatus.Paid, new DateTime(2026, 2, 5)),
            CreateTransaction(Guid.NewGuid(), TransactionStatus.Paid, new DateTime(2026, 3, 5)));

        var handler = CreateHandler(context);
        var query = new ListTransactionsQuery(
            null,
            null,
            null,
            null,
            new DateTime(2026, 2, 1),
            new DateTime(2026, 2, 28),
            null,
            null,
            1,
            20);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.Should().OnlyContain(transaction => transaction.CompetenceDate.Month == 2);
    }

    [Fact]
    public async Task HandleAsync_WithStatusFilter_ShouldReturnOnlyFilteredStatus()
    {
        await using var context = CreateContext();
        await SeedAsync(context,
            CreateTransaction(Guid.NewGuid(), TransactionStatus.Paid, new DateTime(2026, 1, 1)),
            CreateTransaction(Guid.NewGuid(), TransactionStatus.Pending, new DateTime(2026, 1, 2)),
            CreateCancelledTransaction(Guid.NewGuid(), new DateTime(2026, 1, 3)));

        var handler = CreateHandler(context);
        var query = new ListTransactionsQuery(null, null, null, TransactionStatus.Cancelled, null, null, null, null, 1, 20);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.Should().OnlyContain(transaction => transaction.Status == TransactionStatus.Cancelled);
    }

    [Fact]
    public async Task HandleAsync_WithPagination_ShouldReturnExpectedSlice()
    {
        await using var context = CreateContext();
        await SeedAsync(context,
            CreateTransaction(Guid.NewGuid(), TransactionStatus.Pending, new DateTime(2026, 1, 1)),
            CreateTransaction(Guid.NewGuid(), TransactionStatus.Pending, new DateTime(2026, 1, 2)),
            CreateTransaction(Guid.NewGuid(), TransactionStatus.Pending, new DateTime(2026, 1, 3)),
            CreateTransaction(Guid.NewGuid(), TransactionStatus.Pending, new DateTime(2026, 1, 4)));

        var handler = CreateHandler(context);
        var query = new ListTransactionsQuery(null, null, null, null, null, null, null, null, 2, 2);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Data.Should().HaveCount(2);
        result.Pagination.Page.Should().Be(2);
        result.Pagination.Total.Should().Be(4);
        result.Pagination.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_WhenPageIsGreaterThanTotalPages_ShouldReturnEmptyDataWithMetadata()
    {
        await using var context = CreateContext();
        await SeedAsync(context,
            CreateTransaction(Guid.NewGuid(), TransactionStatus.Pending, new DateTime(2026, 1, 1)),
            CreateTransaction(Guid.NewGuid(), TransactionStatus.Pending, new DateTime(2026, 1, 2)));

        var handler = CreateHandler(context);
        var query = new ListTransactionsQuery(null, null, null, null, null, null, null, null, 3, 1);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Data.Should().BeEmpty();
        result.Pagination.Page.Should().Be(3);
        result.Pagination.Size.Should().Be(1);
        result.Pagination.Total.Should().Be(2);
        result.Pagination.TotalPages.Should().Be(2);
    }

    private static ListTransactionsQueryHandler CreateHandler(FinanceiroDbContext context)
    {
        var repository = new TransactionRepository(context);
        var establishmentRepository = new EstablishmentRepository(context);
        var validator = new ListTransactionsQueryValidator();
        var logger = new Mock<ILogger<ListTransactionsQueryHandler>>();

        return new ListTransactionsQueryHandler(repository, establishmentRepository, validator, logger.Object);
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

    private static Transaction CreateTransaction(Guid accountId, TransactionStatus status, DateTime competenceDate)
    {
        return Transaction.Create(
            accountId,
            Guid.NewGuid(),
            TransactionType.Debit,
            100m,
            "Transaction",
            competenceDate,
            competenceDate,
            status,
            "user-1");
    }

    private static Transaction CreateCancelledTransaction(Guid accountId, DateTime competenceDate)
    {
        var transaction = CreateTransaction(accountId, TransactionStatus.Pending, competenceDate);
        transaction.Cancel("user-1", "Cancelled in test");
        return transaction;
    }
}

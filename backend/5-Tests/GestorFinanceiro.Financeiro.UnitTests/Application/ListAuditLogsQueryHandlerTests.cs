using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Queries.Audit;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Infra.Context;
using GestorFinanceiro.Financeiro.Infra.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application;

public class ListAuditLogsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithFilters_ShouldApplyFilterCriteria()
    {
        await using var context = CreateContext();
        var accountEntityId = Guid.NewGuid();

        await SeedAsync(
            context,
            AuditLog.Create("Account", accountEntityId, "Updated", "user-1"),
            AuditLog.Create("Transaction", Guid.NewGuid(), "Created", "user-1"),
            AuditLog.Create("Account", Guid.NewGuid(), "Created", "user-2"));

        var handler = CreateHandler(context);
        var query = new ListAuditLogsQuery("Account", accountEntityId, "user-1", null, null, 1, 20);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.Single().EntityType.Should().Be("Account");
        result.Data.Single().EntityId.Should().Be(accountEntityId);
        result.Data.Single().UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task HandleAsync_WithPagination_ShouldReturnExpectedSlice()
    {
        await using var context = CreateContext();

        await SeedAsync(
            context,
            AuditLog.Create("Transaction", Guid.NewGuid(), "Created", "user-1"),
            AuditLog.Create("Transaction", Guid.NewGuid(), "Created", "user-1"),
            AuditLog.Create("Transaction", Guid.NewGuid(), "Created", "user-1"));

        var handler = CreateHandler(context);
        var query = new ListAuditLogsQuery(null, null, null, null, null, 2, 2);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Pagination.Page.Should().Be(2);
        result.Pagination.Size.Should().Be(2);
        result.Pagination.Total.Should().Be(3);
        result.Pagination.TotalPages.Should().Be(2);
    }

    private static ListAuditLogsQueryHandler CreateHandler(FinanceiroDbContext context)
    {
        var repository = new AuditLogRepository(context);
        var logger = new Mock<ILogger<ListAuditLogsQueryHandler>>();
        return new ListAuditLogsQueryHandler(repository, logger.Object);
    }

    private static FinanceiroDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinanceiroDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FinanceiroDbContext(options);
    }

    private static async Task SeedAsync(FinanceiroDbContext context, params AuditLog[] auditLogs)
    {
        await context.AuditLogs.AddRangeAsync(auditLogs);
        await context.SaveChangesAsync();
    }
}

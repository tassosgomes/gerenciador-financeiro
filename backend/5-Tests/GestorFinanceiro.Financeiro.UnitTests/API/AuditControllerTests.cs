using AwesomeAssertions;
using GestorFinanceiro.Financeiro.API.Controllers;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Audit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.API;

public class AuditControllerTests
{
    private readonly Mock<IDispatcher> _dispatcherMock;
    private readonly AuditController _controller;

    public AuditControllerTests()
    {
        _dispatcherMock = new Mock<IDispatcher>();
        _controller = new AuditController(_dispatcherMock.Object);
    }

    [Fact]
    public async Task ListAsync_WithFilters_ShouldReturnOkWithPagedAuditLogs()
    {
        var entityType = "Transaction";
        var entityId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var dateFrom = DateTime.UtcNow.AddDays(-7);
        var dateTo = DateTime.UtcNow;

        var response = new PagedResult<AuditLogDto>(
            [CreateAuditLogDto()],
            new PaginationMetadata(2, 10, 1, 1));

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchQueryAsync<ListAuditLogsQuery, PagedResult<AuditLogDto>>(
                It.Is<ListAuditLogsQuery>(query =>
                    query.EntityType == entityType &&
                    query.EntityId == entityId &&
                    query.UserId == userId &&
                    query.DateFrom == dateFrom &&
                    query.DateTo == dateTo &&
                    query.Page == 2 &&
                    query.Size == 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.ListAsync(entityType, entityId, userId, dateFrom, dateTo, 2, 10, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task ListAsync_WithoutFilters_ShouldUseDefaultPagination()
    {
        var response = new PagedResult<AuditLogDto>(
            [CreateAuditLogDto()],
            new PaginationMetadata(1, 20, 1, 1));

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchQueryAsync<ListAuditLogsQuery, PagedResult<AuditLogDto>>(
                It.Is<ListAuditLogsQuery>(query =>
                    query.EntityType == null &&
                    query.EntityId == null &&
                    query.UserId == null &&
                    query.DateFrom == null &&
                    query.DateTo == null &&
                    query.Page == 1 &&
                    query.Size == 20),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.ListAsync(null, null, null, null, null, cancellationToken: CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(response);
    }

    private static AuditLogDto CreateAuditLogDto()
    {
        return new AuditLogDto(
            Guid.NewGuid(),
            "Transaction",
            Guid.NewGuid(),
            "Updated",
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            "{\"amount\":100}");
    }
}

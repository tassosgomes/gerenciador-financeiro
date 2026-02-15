using AwesomeAssertions;
using GestorFinanceiro.Financeiro.API.Controllers;
using GestorFinanceiro.Financeiro.API.Controllers.Requests;
using GestorFinanceiro.Financeiro.Application.Commands.Transaction;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Transaction;
using GestorFinanceiro.Financeiro.Domain.Enum;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace GestorFinanceiro.Financeiro.UnitTests.API;

public class TransactionsControllerTests
{
    private readonly Mock<IDispatcher> _dispatcherMock;
    private readonly TransactionsController _controller;

    public TransactionsControllerTests()
    {
        _dispatcherMock = new Mock<IDispatcher>();
        _controller = new TransactionsController(_dispatcherMock.Object);
        ConfigureAuthenticatedUser(Guid.NewGuid());
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedAtAction()
    {
        var request = new CreateTransactionRequest
        {
            AccountId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            Type = TransactionType.Debit,
            Amount = 100,
            Description = "Mercado",
            CompetenceDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date
        };

        var response = CreateTransactionResponse();

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<CreateTransactionCommand, TransactionResponse>(
                It.IsAny<CreateTransactionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.CreateAsync(request, CancellationToken.None);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result.Result!;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnOkWithPaginationMetadata()
    {
        var response = new PagedResult<TransactionResponse>(
            [CreateTransactionResponse()],
            new PaginationMetadata(1, 20, 1, 1));

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchQueryAsync<ListTransactionsQuery, PagedResult<TransactionResponse>>(
                It.IsAny<ListTransactionsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.ListAsync(null, null, null, null, null, null, null, null, 1, 20, null, null, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnOk()
    {
        var response = CreateTransactionResponse();

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchQueryAsync<GetTransactionByIdQuery, TransactionResponse>(
                It.IsAny<GetTransactionByIdQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.GetByIdAsync(response.Id, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result.Result!).StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task AdjustAsync_ShouldReturnCreated()
    {
        var response = CreateTransactionResponse();
        var request = new AdjustTransactionRequest
        {
            NewAmount = 120m,
            Description = "Ajuste",
            OperationId = "op-1"
        };

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<AdjustTransactionCommand, TransactionResponse>(
                It.IsAny<AdjustTransactionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.AdjustAsync(response.Id, request, CancellationToken.None);

        result.Result.Should().BeOfType<CreatedResult>();
        ((CreatedResult)result.Result!).StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task CancelAsync_ShouldReturnOkWithUpdatedTransaction()
    {
        var response = CreateTransactionResponse() with { Status = TransactionStatus.Cancelled };

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<CancelTransactionCommand, TransactionResponse>(
                It.IsAny<CancelTransactionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.CancelAsync(response.Id, new CancelTransactionRequest { Reason = "Erro" }, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(response);
    }

    private void ConfigureAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "Bearer");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    private static TransactionResponse CreateTransactionResponse()
    {
        return new TransactionResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            100m,
            "Transacao",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            TransactionStatus.Pending,
            false,
            null,
            false,
            null,
            null,
            null,
            false,
            null,
            null,
            null,
            null,
            null,
            false,
            DateTime.UtcNow,
            null);
    }
}

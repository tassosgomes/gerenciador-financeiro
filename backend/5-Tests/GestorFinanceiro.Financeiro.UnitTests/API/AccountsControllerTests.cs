using AwesomeAssertions;
using GestorFinanceiro.Financeiro.API.Controllers;
using GestorFinanceiro.Financeiro.API.Controllers.Requests;
using GestorFinanceiro.Financeiro.Application.Commands.Account;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Account;
using GestorFinanceiro.Financeiro.Domain.Enum;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace GestorFinanceiro.Financeiro.UnitTests.API;

public class AccountsControllerTests
{
    private readonly Mock<IDispatcher> _dispatcherMock;
    private readonly AccountsController _controller;

    public AccountsControllerTests()
    {
        _dispatcherMock = new Mock<IDispatcher>();
        _controller = new AccountsController(_dispatcherMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldReturnCreatedAtActionWithLocation()
    {
        ConfigureAuthenticatedUser(Guid.NewGuid());

        var request = new CreateAccountRequest
        {
            Name = "Conta Principal",
            Type = AccountType.Corrente,
            InitialBalance = 100m,
            AllowNegativeBalance = false
        };

        var accountResponse = CreateAccountResponse();

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<CreateAccountCommand, AccountResponse>(
                It.IsAny<CreateAccountCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountResponse);

        var result = await _controller.CreateAsync(request, CancellationToken.None);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result.Result!;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(AccountsController.GetByIdAsync));
        createdResult.RouteValues.Should().ContainKey("id");
        createdResult.RouteValues!["id"].Should().Be(accountResponse.Id);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnOkWithFilteredResponse()
    {
        var response = new List<AccountResponse> { CreateAccountResponse() };

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchQueryAsync<ListAccountsQuery, IReadOnlyList<AccountResponse>>(
                It.Is<ListAccountsQuery>(query => query.IsActive == true),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.ListAsync(true, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingAccount_ShouldReturnOkWithResponse()
    {
        var accountResponse = CreateAccountResponse();

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchQueryAsync<GetAccountByIdQuery, AccountResponse>(
                It.IsAny<GetAccountByIdQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountResponse);

        var result = await _controller.GetByIdAsync(accountResponse.Id, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(accountResponse);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_ShouldReturnOkWithUpdatedAccount()
    {
        ConfigureAuthenticatedUser(Guid.NewGuid());

        var accountId = Guid.NewGuid();
        var request = new UpdateAccountRequest
        {
            Name = "Conta Atualizada",
            AllowNegativeBalance = true
        };
        var response = CreateAccountResponse();

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<UpdateAccountCommand, AccountResponse>(
                It.IsAny<UpdateAccountCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.UpdateAsync(accountId, request, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenActiveFlagIsFalse_ShouldReturnNoContent()
    {
        ConfigureAuthenticatedUser(Guid.NewGuid());

        var accountId = Guid.NewGuid();
        var request = new UpdateAccountStatusRequest { IsActive = false };

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<DeactivateAccountCommand, Unit>(
                It.IsAny<DeactivateAccountCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _controller.UpdateStatusAsync(accountId, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        ((NoContentResult)result).StatusCode.Should().Be(StatusCodes.Status204NoContent);
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

    private static AccountResponse CreateAccountResponse()
    {
        return new AccountResponse(
            Guid.NewGuid(),
            "Conta Principal",
            100m,
            true,
            DateTime.UtcNow,
            DateTime.UtcNow);
    }
}

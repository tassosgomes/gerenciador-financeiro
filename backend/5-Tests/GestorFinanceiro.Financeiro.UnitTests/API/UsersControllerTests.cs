using AwesomeAssertions;
using GestorFinanceiro.Financeiro.API.Controllers;
using GestorFinanceiro.Financeiro.API.Controllers.Requests;
using GestorFinanceiro.Financeiro.Application.Commands.User;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace GestorFinanceiro.Financeiro.UnitTests.API;

public class UsersControllerTests
{
    private readonly Mock<IDispatcher> _dispatcherMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _dispatcherMock = new Mock<IDispatcher>();
        _controller = new UsersController(_dispatcherMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldReturnCreatedAtActionWithLocation()
    {
        var authenticatedUserId = Guid.NewGuid();
        ConfigureAuthenticatedUser(authenticatedUserId);

        var request = new CreateUserRequest
        {
            Name = "Member",
            Email = "member@familia.com",
            Password = "SenhaSegura123!",
            Role = "Member"
        };

        var createdUser = CreateUserResponse();
        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<CreateUserCommand, UserResponse>(
                It.IsAny<CreateUserCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        var result = await _controller.CreateAsync(request, CancellationToken.None);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result.Result!;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(UsersController.GetByIdAsync));
        createdResult.RouteValues.Should().ContainKey("id");
        createdResult.RouteValues!["id"].Should().Be(createdUser.Id);
        createdResult.Value.Should().BeEquivalentTo(createdUser);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOkWithUsers()
    {
        var users = new List<UserResponse>
        {
            CreateUserResponse(),
            CreateUserResponse()
        };

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchQueryAsync<GetAllUsersQuery, IEnumerable<UserResponse>>(
                It.IsAny<GetAllUsersQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _controller.GetAllAsync(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(users);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingUser_ShouldReturnOkWithUser()
    {
        var user = CreateUserResponse();

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchQueryAsync<GetUserByIdQuery, UserResponse>(
                It.IsAny<GetUserByIdQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _controller.GetByIdAsync(user.Id, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithValidRequest_ShouldReturnNoContent()
    {
        ConfigureAuthenticatedUser(Guid.NewGuid());

        var userId = Guid.NewGuid();
        var request = new UpdateUserStatusRequest { IsActive = false };

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<ToggleUserStatusCommand, Unit>(
                It.IsAny<ToggleUserStatusCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _controller.UpdateStatusAsync(userId, request, CancellationToken.None);

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

    private static UserResponse CreateUserResponse()
    {
        return new UserResponse(
            Guid.NewGuid(),
            "User",
            "user@familia.com",
            "Member",
            true,
            false,
            DateTime.UtcNow);
    }
}

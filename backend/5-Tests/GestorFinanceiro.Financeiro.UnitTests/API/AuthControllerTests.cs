using AwesomeAssertions;
using GestorFinanceiro.Financeiro.API.Controllers;
using GestorFinanceiro.Financeiro.API.Controllers.Requests;
using GestorFinanceiro.Financeiro.Application.Commands.Auth;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace GestorFinanceiro.Financeiro.UnitTests.API;

public class AuthControllerTests
{
    private readonly Mock<IDispatcher> _dispatcherMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _dispatcherMock = new Mock<IDispatcher>();
        _controller = new AuthController(_dispatcherMock.Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidRequest_ShouldReturnOkWithAuthResponse()
    {
        var request = new LoginRequest { Email = "admin@familia.com", Password = "SenhaSegura123!" };
        var authResponse = CreateAuthResponse();

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<LoginCommand, AuthResponse>(
                It.IsAny<LoginCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);

        var result = await _controller.LoginAsync(request, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(authResponse);
    }

    [Fact]
    public async Task RefreshAsync_WithValidRequest_ShouldReturnOkWithAuthResponse()
    {
        var request = new RefreshTokenRequest { RefreshToken = "refresh-token" };
        var authResponse = CreateAuthResponse();

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<RefreshTokenCommand, AuthResponse>(
                It.IsAny<RefreshTokenCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);

        var result = await _controller.RefreshAsync(request, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(authResponse);
    }

    [Fact]
    public async Task LogoutAsync_WithAuthenticatedUser_ShouldReturnNoContent()
    {
        var userId = Guid.NewGuid();
        ConfigureAuthenticatedUser(userId);

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<LogoutCommand, Unit>(
                It.IsAny<LogoutCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _controller.LogoutAsync(CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        ((NoContentResult)result).StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithAuthenticatedUser_ShouldReturnNoContent()
    {
        var userId = Guid.NewGuid();
        ConfigureAuthenticatedUser(userId);
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "SenhaAtual123!",
            NewPassword = "NovaSenha123!"
        };

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<ChangePasswordCommand, Unit>(
                It.IsAny<ChangePasswordCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _controller.ChangePasswordAsync(request, CancellationToken.None);

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

    private static AuthResponse CreateAuthResponse()
    {
        var user = new UserResponse(
            Guid.NewGuid(),
            "Admin",
            "admin@familia.com",
            "Admin",
            true,
            false,
            DateTime.UtcNow);

        return new AuthResponse("access-token", "refresh-token", 3600, user);
    }
}

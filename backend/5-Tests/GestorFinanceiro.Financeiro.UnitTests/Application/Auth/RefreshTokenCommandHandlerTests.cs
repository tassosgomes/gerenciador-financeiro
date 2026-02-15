using AwesomeAssertions;
using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Commands.Auth;
using GestorFinanceiro.Financeiro.Application.Services;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using DomainUser = GestorFinanceiro.Financeiro.Domain.Entity.User;
using DomainRefreshToken = GestorFinanceiro.Financeiro.Domain.Entity.RefreshToken;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<RefreshTokenCommandHandler>> _logger = new();

    private readonly RefreshTokenCommandHandler _sut;

    public RefreshTokenCommandHandlerTests()
    {
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _sut = new RefreshTokenCommandHandler(
            _refreshTokenRepository.Object,
            _userRepository.Object,
            _tokenService.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    private static string GenerateTestJwt(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretKeyForTestingPurposesOnly12345678"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Name, "Test User"),
            new(JwtRegisteredClaimNames.Email, "test@test.com"),
            new("role", "Admin")
        };

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(1440),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task HandleAsync_TokenValido_RetornaNovoAuthResponse()
    {
        var userId = Guid.NewGuid();
        var user = DomainUser.Create("Test User", "test@test.com", "hashed-password", UserRole.Admin, "system");
        var currentToken = DomainRefreshToken.Create(userId, "old-refresh-token", DateTime.UtcNow.AddDays(7), userId.ToString());
        var newRefreshToken = DomainRefreshToken.Create(userId, "new-refresh-token", DateTime.UtcNow.AddDays(7), userId.ToString());
        var jwt = GenerateTestJwt(userId);
        var currentTokenHash = DomainRefreshToken.ComputeHash("old-refresh-token");

        _refreshTokenRepository.Setup(mock => mock.GetByTokenHashAsync(currentTokenHash, It.IsAny<CancellationToken>())).ReturnsAsync(currentToken);
        _userRepository.Setup(mock => mock.GetByIdAsync(currentToken.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenService.Setup(mock => mock.GenerateAccessToken(user)).Returns(jwt);
        _tokenService.Setup(mock => mock.GenerateRefreshToken(user.Id)).Returns(newRefreshToken);
        _refreshTokenRepository.Setup(mock => mock.AddAsync(newRefreshToken, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var command = new RefreshTokenCommand("old-refresh-token");
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be(jwt);
        result.RefreshToken.Should().Be("new-refresh-token");
        result.ExpiresIn.Should().BeGreaterThan(0);
        result.User.Email.Should().Be("test@test.com");
        currentToken.IsRevoked.Should().BeTrue();
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TokenInexistente_LancaInvalidRefreshTokenException()
    {
        var tokenHash = DomainRefreshToken.ComputeHash("token-inexistente");
        _refreshTokenRepository.Setup(mock => mock.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>())).ReturnsAsync((DomainRefreshToken?)null);

        var command = new RefreshTokenCommand("token-inexistente");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidRefreshTokenException>();
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_TokenRevogado_LancaInvalidRefreshTokenException()
    {
        var userId = Guid.NewGuid();
        var revokedToken = DomainRefreshToken.Create(userId, "revoked-token", DateTime.UtcNow.AddDays(7), userId.ToString());
        revokedToken.Revoke();
        var tokenHash = DomainRefreshToken.ComputeHash("revoked-token");

        _refreshTokenRepository.Setup(mock => mock.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>())).ReturnsAsync(revokedToken);

        var command = new RefreshTokenCommand("revoked-token");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidRefreshTokenException>();
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UsuarioNaoEncontrado_LancaUserNotFoundException()
    {
        var userId = Guid.NewGuid();
        var currentToken = DomainRefreshToken.Create(userId, "valid-token", DateTime.UtcNow.AddDays(7), userId.ToString());
        var tokenHash = DomainRefreshToken.ComputeHash("valid-token");

        _refreshTokenRepository.Setup(mock => mock.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>())).ReturnsAsync(currentToken);
        _userRepository.Setup(mock => mock.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((DomainUser?)null);

        var command = new RefreshTokenCommand("valid-token");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_UsuarioInativo_LancaInactiveUserException()
    {
        var userId = Guid.NewGuid();
        var currentToken = DomainRefreshToken.Create(userId, "valid-token", DateTime.UtcNow.AddDays(7), userId.ToString());
        var user = DomainUser.Create("Test User", "test@test.com", "hashed-password", UserRole.Admin, "system");
        user.Deactivate("system");
        var tokenHash = DomainRefreshToken.ComputeHash("valid-token");

        _refreshTokenRepository.Setup(mock => mock.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>())).ReturnsAsync(currentToken);
        _userRepository.Setup(mock => mock.GetByIdAsync(currentToken.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var command = new RefreshTokenCommand("valid-token");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InactiveUserException>();
    }

    [Fact]
    public async Task HandleAsync_TokenVazio_LancaValidationException()
    {
        var command = new RefreshTokenCommand("");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_TokenExpirado_LancaInvalidRefreshTokenException()
    {
        var userId = Guid.NewGuid();
        var expiredToken = DomainRefreshToken.Create(userId, "expired-token", DateTime.UtcNow.AddMinutes(-1), userId.ToString());
        var tokenHash = DomainRefreshToken.ComputeHash("expired-token");

        _refreshTokenRepository.Setup(mock => mock.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>())).ReturnsAsync(expiredToken);

        var command = new RefreshTokenCommand("expired-token");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidRefreshTokenException>();
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

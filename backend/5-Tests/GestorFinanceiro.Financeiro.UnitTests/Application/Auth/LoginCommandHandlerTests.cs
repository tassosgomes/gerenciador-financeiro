using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Commands.Auth;
using GestorFinanceiro.Financeiro.Application.Dtos;
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

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<LoginCommandHandler>> _logger = new();

    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _sut = new LoginCommandHandler(
            _userRepository.Object,
            _refreshTokenRepository.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    private static string GenerateTestJwt(Guid userId, int expirationMinutes = 1440)
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
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static DomainUser CreateTestUser(Guid? id = null, bool isActive = true)
    {
        var user = DomainUser.Create("Test User", "test@test.com", "hashed-password", UserRole.Admin, "system");
        if (!isActive)
        {
            user.Deactivate("system");
        }
        return user;
    }

    [Fact]
    public async Task HandleAsync_CredenciaisValidas_RetornaAuthResponse()
    {
        var user = CreateTestUser();
        var jwt = GenerateTestJwt(user.Id);
        var refreshToken = DomainRefreshToken.Create(user.Id, "refresh-token-value", DateTime.UtcNow.AddDays(7), user.Id.ToString());

        _userRepository.Setup(mock => mock.GetByEmailAsync("test@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(mock => mock.Verify("password123", "hashed-password")).Returns(true);
        _tokenService.Setup(mock => mock.GenerateAccessToken(user)).Returns(jwt);
        _tokenService.Setup(mock => mock.GenerateRefreshToken(user.Id)).Returns(refreshToken);
        _refreshTokenRepository.Setup(mock => mock.AddAsync(refreshToken, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var command = new LoginCommand("test@test.com", "password123");
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be(jwt);
        result.RefreshToken.Should().Be("refresh-token-value");
        result.ExpiresIn.Should().BeGreaterThan(0);
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("test@test.com");
        _refreshTokenRepository.Verify(mock => mock.AddAsync(refreshToken, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EmailInexistente_LancaInvalidCredentialsException()
    {
        _userRepository.Setup(mock => mock.GetByEmailAsync("naoexiste@test.com", It.IsAny<CancellationToken>())).ReturnsAsync((DomainUser?)null);

        var command = new LoginCommand("naoexiste@test.com", "password123");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidCredentialsException>();
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SenhaIncorreta_LancaInvalidCredentialsException()
    {
        var user = CreateTestUser();
        _userRepository.Setup(mock => mock.GetByEmailAsync("test@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(mock => mock.Verify("senha-errada", "hashed-password")).Returns(false);

        var command = new LoginCommand("test@test.com", "senha-errada");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidCredentialsException>();
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UsuarioInativo_LancaInactiveUserException()
    {
        var user = CreateTestUser(isActive: false);
        _userRepository.Setup(mock => mock.GetByEmailAsync("test@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(mock => mock.Verify("password123", "hashed-password")).Returns(true);

        var command = new LoginCommand("test@test.com", "password123");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InactiveUserException>();
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_EmailVazio_LancaInvalidOperationException()
    {
        var command = new LoginCommand("", "password123");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HandleAsync_SenhaVazia_LancaInvalidOperationException()
    {
        var command = new LoginCommand("test@test.com", "");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HandleAsync_EmailInvalido_LancaInvalidOperationException()
    {
        var command = new LoginCommand("email-invalido", "password123");
        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}

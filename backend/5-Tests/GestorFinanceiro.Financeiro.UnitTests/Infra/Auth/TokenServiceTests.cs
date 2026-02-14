using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Infra.Auth;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

namespace GestorFinanceiro.Financeiro.UnitTests.Infra.Auth;

public class TokenServiceTests
{
    private readonly JwtSettings _jwtSettings;
    private readonly TokenService _sut;

    public TokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "SuperSecretKeyForTestingPurposesOnly12345678",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenExpirationMinutes = 1440,
            RefreshTokenExpirationDays = 7
        };

        _sut = new TokenService(Options.Create(_jwtSettings));
    }

    [Fact]
    public void GenerateAccessToken_UsuarioValido_GeraJwtValido()
    {
        var user = User.Create("Test User", "test@test.com", "hashed-password", UserRole.Admin, "system");

        var token = _sut.GenerateAccessToken(user);

        token.Should().NotBeNullOrWhiteSpace();
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_UsuarioValido_ContemClaimsCorretas()
    {
        var user = User.Create("Test User", "test@test.com", "hashed-password", UserRole.Admin, "system");

        var token = _sut.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Name && c.Value == "Test User");
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@test.com");
        jwtToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Admin");
    }

    [Fact]
    public void GenerateAccessToken_UsuarioValido_TokenExpiracaoCorreta()
    {
        var user = User.Create("Test User", "test@test.com", "hashed-password", UserRole.Admin, "system");

        var token = _sut.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var expectedExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateAccessToken_UsuarioNulo_LancaArgumentNullException()
    {
        var action = () => _sut.GenerateAccessToken(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateRefreshToken_GeraTokenBase64NaoVazio()
    {
        var userId = Guid.NewGuid();

        var refreshToken = _sut.GenerateRefreshToken(userId);

        refreshToken.Should().NotBeNull();
        refreshToken.Token.Should().NotBeNullOrWhiteSpace();
        refreshToken.UserId.Should().Be(userId);
    }

    [Fact]
    public void GenerateRefreshToken_ExpiracaoCorreta()
    {
        var userId = Guid.NewGuid();

        var refreshToken = _sut.GenerateRefreshToken(userId);

        var expectedExpiration = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        refreshToken.ExpiresAt.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_TokensDistintos()
    {
        var userId = Guid.NewGuid();

        var token1 = _sut.GenerateRefreshToken(userId);
        var token2 = _sut.GenerateRefreshToken(userId);

        token1.Token.Should().NotBe(token2.Token);
    }

    [Fact]
    public void ValidateAccessToken_TokenValido_RetornaClaimsPrincipal()
    {
        var user = User.Create("Test User", "test@test.com", "hashed-password", UserRole.Admin, "system");
        var token = _sut.GenerateAccessToken(user);

        var principal = _sut.ValidateAccessToken(token);

        principal.Should().NotBeNull();
    }

    [Fact]
    public void ValidateAccessToken_TokenInvalido_RetornaNull()
    {
        var result = _sut.ValidateAccessToken("token-invalido");

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessToken_TokenVazio_RetornaNull()
    {
        var result = _sut.ValidateAccessToken("");

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessToken_TokenNulo_RetornaNull()
    {
        var result = _sut.ValidateAccessToken(null!);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessToken_TokenComChaveErrada_RetornaNull()
    {
        var wrongSettings = new JwtSettings
        {
            SecretKey = "WrongSecretKeyThatIsDifferentFromOriginal1234",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenExpirationMinutes = 1440,
            RefreshTokenExpirationDays = 7
        };
        var wrongService = new TokenService(Options.Create(wrongSettings));
        var user = User.Create("Test User", "test@test.com", "hashed-password", UserRole.Admin, "system");
        var token = wrongService.GenerateAccessToken(user);

        var result = _sut.ValidateAccessToken(token);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessToken_TokenExpirado_RetornaNull()
    {
        var expiredSettings = new JwtSettings
        {
            SecretKey = _jwtSettings.SecretKey,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            AccessTokenExpirationMinutes = -1,
            RefreshTokenExpirationDays = _jwtSettings.RefreshTokenExpirationDays
        };

        var expiredTokenService = new TokenService(Options.Create(expiredSettings));
        var user = User.Create("Test User", "test@test.com", "hashed-password", UserRole.Admin, "system");
        var expiredToken = expiredTokenService.GenerateAccessToken(user);

        var result = _sut.ValidateAccessToken(expiredToken);

        result.Should().BeNull();
    }
}

using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Entity;

public class RefreshTokenTests
{
    [Fact]
    public void Create_WithValidData_SetsPropertiesCorrectly()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var before = DateTime.UtcNow;

        var token = RefreshToken.Create(userId, "token-value", expiresAt, "system");

        var after = DateTime.UtcNow;
        token.UserId.Should().Be(userId);
        token.Token.Should().Be("token-value");
        token.TokenHash.Should().Be(RefreshToken.ComputeHash("token-value"));
        token.ExpiresAt.Should().Be(expiresAt);
        token.IsRevoked.Should().BeFalse();
        token.RevokedAt.Should().BeNull();
        token.CreatedBy.Should().Be("system");
        token.CreatedAt.Should().BeOnOrAfter(before);
        token.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsInPast_ReturnsTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddMinutes(-1), "system");

        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsInFuture_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7), "system");

        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void Revoke_SetsIsRevokedAndRevokedAt()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7), "system");
        var before = DateTime.UtcNow;

        token.Revoke();

        var after = DateTime.UtcNow;
        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();
        token.RevokedAt!.Value.Should().BeOnOrAfter(before);
        token.RevokedAt!.Value.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void IsActive_WhenNotRevokedAndNotExpired_ReturnsTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7), "system");

        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenRevoked_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7), "system");
        token.Revoke();

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenExpired_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "token", DateTime.UtcNow.AddMinutes(-1), "system");

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        var token1 = RefreshToken.Create(Guid.NewGuid(), "token1", DateTime.UtcNow.AddDays(7), "system");
        var token2 = RefreshToken.Create(Guid.NewGuid(), "token2", DateTime.UtcNow.AddDays(7), "system");

        token1.Id.Should().NotBe(token2.Id);
        token1.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void ComputeHash_SameToken_GeneratesSameHash()
    {
        var hash1 = RefreshToken.ComputeHash("token-value");
        var hash2 = RefreshToken.ComputeHash("token-value");

        hash1.Should().Be(hash2);
    }
}

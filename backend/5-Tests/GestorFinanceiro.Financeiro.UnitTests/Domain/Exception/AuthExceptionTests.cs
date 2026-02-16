using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Exception;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Exception;

public class AuthExceptionTests
{
    [Fact]
    public void UserNotFoundException_ContainsUserId()
    {
        var userId = Guid.NewGuid();

        var exception = new UserNotFoundException(userId);

        exception.Message.Should().Contain(userId.ToString());
        exception.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void UserEmailAlreadyExistsException_ContainsEmail()
    {
        var exception = new UserEmailAlreadyExistsException("test@test.com");

        exception.Message.Should().Contain("test@test.com");
        exception.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void InvalidCredentialsException_HasExpectedMessage()
    {
        var exception = new InvalidCredentialsException();

        exception.Message.Should().Be("Invalid email or password.");
        exception.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void InactiveUserException_ContainsUserId()
    {
        var userId = Guid.NewGuid();

        var exception = new InactiveUserException(userId);

        exception.Message.Should().Contain(userId.ToString());
        exception.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void InvalidRefreshTokenException_HasExpectedMessage()
    {
        var exception = new InvalidRefreshTokenException();

        exception.Message.Should().Contain("refresh token");
        exception.Should().BeAssignableTo<DomainException>();
    }
}

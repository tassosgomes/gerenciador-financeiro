using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Commands.Auth;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Auth;

public class AuthValidatorTests
{
    // ========== LoginCommandValidator ==========

    [Fact]
    public void LoginValidator_DadosValidos_PassaValidacao()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("user@test.com", "password123");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LoginValidator_EmailVazio_FalhaValidacao()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("", "password123");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void LoginValidator_EmailInvalido_FalhaValidacao()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("not-an-email", "password123");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void LoginValidator_SenhaVazia_FalhaValidacao()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("user@test.com", "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void LoginValidator_SenhaCurta_FalhaValidacao()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("user@test.com", "12345");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    // ========== RefreshTokenCommandValidator ==========

    [Fact]
    public void RefreshTokenValidator_TokenValido_PassaValidacao()
    {
        var validator = new RefreshTokenCommandValidator();
        var command = new RefreshTokenCommand("valid-refresh-token");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RefreshTokenValidator_TokenVazio_FalhaValidacao()
    {
        var validator = new RefreshTokenCommandValidator();
        var command = new RefreshTokenCommand("");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RefreshToken");
    }

    // ========== ChangePasswordCommandValidator ==========

    [Fact]
    public void ChangePasswordValidator_DadosValidos_PassaValidacao()
    {
        var validator = new ChangePasswordCommandValidator();
        var command = new ChangePasswordCommand(Guid.NewGuid(), "current-password", "NewPassword1!");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangePasswordValidator_SenhaAtualVazia_FalhaValidacao()
    {
        var validator = new ChangePasswordCommandValidator();
        var command = new ChangePasswordCommand(Guid.NewGuid(), "", "NewPassword1!");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrentPassword");
    }

    [Fact]
    public void ChangePasswordValidator_NovaSenhaVazia_FalhaValidacao()
    {
        var validator = new ChangePasswordCommandValidator();
        var command = new ChangePasswordCommand(Guid.NewGuid(), "current-password", "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void ChangePasswordValidator_NovaSenhaCurta_FalhaValidacao()
    {
        var validator = new ChangePasswordCommandValidator();
        var command = new ChangePasswordCommand(Guid.NewGuid(), "current-password", "short");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Theory]
    [InlineData("newpassword1!")]
    [InlineData("NEWPASSWORD1!")]
    [InlineData("NewPassword!!")]
    [InlineData("NewPassword11")]
    public void ChangePasswordValidator_NovaSenhaSemComplexidade_FalhaValidacao(string newPassword)
    {
        var validator = new ChangePasswordCommandValidator();
        var command = new ChangePasswordCommand(Guid.NewGuid(), "current-password", newPassword);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }
}

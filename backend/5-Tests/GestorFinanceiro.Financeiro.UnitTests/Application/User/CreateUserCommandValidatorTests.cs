using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Commands.User;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.User;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_DadosValidos_PassaValidacao()
    {
        var command = new CreateUserCommand("Test User", "test@test.com", "Password1!", "Admin", "creator-id");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NomeVazio_FalhaValidacao()
    {
        var command = new CreateUserCommand("", "test@test.com", "Password1!", "Admin", "creator-id");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_NomeCurto_FalhaValidacao()
    {
        var command = new CreateUserCommand("AB", "test@test.com", "Password1!", "Admin", "creator-id");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_NomeLongo_FalhaValidacao()
    {
        var nomeLongo = new string('A', 151);
        var command = new CreateUserCommand(nomeLongo, "test@test.com", "Password1!", "Admin", "creator-id");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_EmailVazio_FalhaValidacao()
    {
        var command = new CreateUserCommand("Test User", "", "Password1!", "Admin", "creator-id");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_EmailInvalido_FalhaValidacao()
    {
        var command = new CreateUserCommand("Test User", "not-email", "Password1!", "Admin", "creator-id");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_SenhaVazia_FalhaValidacao()
    {
        var command = new CreateUserCommand("Test User", "test@test.com", "", "Admin", "creator-id");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_SenhaCurta_FalhaValidacao()
    {
        var command = new CreateUserCommand("Test User", "test@test.com", "short", "Admin", "creator-id");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Theory]
    [InlineData("password1!")]
    [InlineData("PASSWORD1!")]
    [InlineData("Password!!")]
    [InlineData("Password11")]
    public void Validate_SenhaSemComplexidade_FalhaValidacao(string password)
    {
        var command = new CreateUserCommand("Test User", "test@test.com", password, "Admin", "creator-id");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_RoleVazia_FalhaValidacao()
    {
        var command = new CreateUserCommand("Test User", "test@test.com", "Password1!", "", "creator-id");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }

    [Fact]
    public void Validate_RoleInvalida_FalhaValidacao()
    {
        var command = new CreateUserCommand("Test User", "test@test.com", "Password1!", "InvalidRole", "creator-id");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }

    [Fact]
    public void Validate_CreatedByVazio_FalhaValidacao()
    {
        var command = new CreateUserCommand("Test User", "test@test.com", "Password1!", "Admin", "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CreatedByUserId");
    }

    [Fact]
    public void Validate_RoleMember_PassaValidacao()
    {
        var command = new CreateUserCommand("Test User", "test@test.com", "Password1!", "Member", "creator-id");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}

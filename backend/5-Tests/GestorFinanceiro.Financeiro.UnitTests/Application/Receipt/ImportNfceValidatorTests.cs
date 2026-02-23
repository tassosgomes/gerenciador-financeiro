using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Commands.Receipt;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Receipt;

public class ImportNfceValidatorTests
{
    private readonly ImportNfceValidator _validator = new();

    [Fact]
    public void Validate_AllFieldsValid_ShouldBeValid()
    {
        var command = CreateValidCommand();

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("1234567890123456789012345678901234567890123A")]
    public void Validate_InvalidAccessKey_ShouldBeInvalid(string accessKey)
    {
        var command = CreateValidCommand() with { AccessKey = accessKey };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "AccessKey");
    }

    [Fact]
    public void Validate_EmptyAccountId_ShouldBeInvalid()
    {
        var command = CreateValidCommand() with { AccountId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "AccountId");
    }

    [Fact]
    public void Validate_EmptyCategoryId_ShouldBeInvalid()
    {
        var command = CreateValidCommand() with { CategoryId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "CategoryId");
    }

    [Fact]
    public void Validate_EmptyDescription_ShouldBeInvalid()
    {
        var command = CreateValidCommand() with { Description = string.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Description");
    }

    private static ImportNfceCommand CreateValidCommand()
    {
        return new ImportNfceCommand(
            "12345678901234567890123456789012345678901234",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Compra mercado",
            DateTime.UtcNow.Date,
            "user-1");
    }
}

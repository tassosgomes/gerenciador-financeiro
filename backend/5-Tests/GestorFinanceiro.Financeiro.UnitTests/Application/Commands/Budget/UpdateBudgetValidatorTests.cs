using FluentValidation.TestHelper;
using GestorFinanceiro.Financeiro.Application.Commands.Budget;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Commands.Budget;

public class UpdateBudgetValidatorTests
{
    private readonly UpdateBudgetValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = BuildValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyId_ShouldFail()
    {
        var command = BuildValidCommand() with { Id = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(item => item.Id);
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldFail()
    {
        var command = BuildValidCommand() with { Name = string.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(item => item.Name);
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldFail()
    {
        var command = BuildValidCommand() with { Name = new string('a', 151) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(item => item.Name);
    }

    [Fact]
    public void Validate_WithZeroPercentage_ShouldFail()
    {
        var command = BuildValidCommand() with { Percentage = 0m };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(item => item.Percentage);
    }

    [Fact]
    public void Validate_WithPercentageOver100_ShouldFail()
    {
        var command = BuildValidCommand() with { Percentage = 101m };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(item => item.Percentage);
    }

    [Fact]
    public void Validate_WithEmptyCategoryIds_ShouldFail()
    {
        var command = BuildValidCommand() with { CategoryIds = [] };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(item => item.CategoryIds);
    }

    private static UpdateBudgetCommand BuildValidCommand()
    {
        return new UpdateBudgetCommand(
            Guid.NewGuid(),
            "Orçamento Moradia",
            25m,
            [Guid.NewGuid()],
            true,
            "user-1");
    }
}

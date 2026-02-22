using FluentValidation;

namespace GestorFinanceiro.Financeiro.Application.Commands.Budget;

public class UpdateBudgetValidator : AbstractValidator<UpdateBudgetCommand>
{
    public UpdateBudgetValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(150);

        RuleFor(command => command.Percentage)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);

        RuleFor(command => command.CategoryIds)
            .NotEmpty();
    }
}

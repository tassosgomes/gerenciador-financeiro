using FluentValidation;

namespace GestorFinanceiro.Financeiro.Application.Commands.Budget;

public class CreateBudgetValidator : AbstractValidator<CreateBudgetCommand>
{
    public CreateBudgetValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(150);

        RuleFor(command => command.Percentage)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);

        RuleFor(command => command.ReferenceYear)
            .GreaterThan(2000);

        RuleFor(command => command.ReferenceMonth)
            .InclusiveBetween(1, 12);

        RuleFor(command => command.CategoryIds)
            .NotEmpty();
    }
}

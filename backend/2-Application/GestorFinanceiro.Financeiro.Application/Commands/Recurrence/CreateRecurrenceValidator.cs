using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Commands.Recurrence;

namespace GestorFinanceiro.Financeiro.Application.Commands.Recurrence;

public class CreateRecurrenceValidator : AbstractValidator<CreateRecurrenceCommand>
{
    public CreateRecurrenceValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid transaction type.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(200).WithMessage("Description cannot exceed 200 characters.");

        RuleFor(x => x.DayOfMonth)
            .InclusiveBetween(1, 31).WithMessage("Day of month must be between 1 and 31.");

        RuleFor(x => x.DefaultStatus)
            .IsInEnum().WithMessage("Invalid default status.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
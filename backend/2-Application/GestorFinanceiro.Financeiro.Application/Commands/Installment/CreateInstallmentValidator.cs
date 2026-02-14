using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Commands.Installment;

namespace GestorFinanceiro.Financeiro.Application.Commands.Installment;

public class CreateInstallmentValidator : AbstractValidator<CreateInstallmentCommand>
{
    public CreateInstallmentValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid transaction type.");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("Total amount must be greater than zero.");

        RuleFor(x => x.InstallmentCount)
            .GreaterThan(0).WithMessage("Installment count must be greater than zero.")
            .LessThanOrEqualTo(120).WithMessage("Installment count cannot exceed 120.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(200).WithMessage("Description cannot exceed 200 characters.");

        RuleFor(x => x.FirstCompetenceDate)
            .NotEmpty().WithMessage("First competence date is required.");

        RuleFor(x => x.FirstDueDate)
            .NotEmpty().WithMessage("First due date is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
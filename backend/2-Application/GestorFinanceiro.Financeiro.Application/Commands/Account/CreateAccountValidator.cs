using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Commands.Account;

namespace GestorFinanceiro.Financeiro.Application.Commands.Account;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid account type");

        RuleFor(x => x.InitialBalance)
            .GreaterThanOrEqualTo(0).WithMessage("Initial balance must be greater than or equal to 0");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");
    }
}
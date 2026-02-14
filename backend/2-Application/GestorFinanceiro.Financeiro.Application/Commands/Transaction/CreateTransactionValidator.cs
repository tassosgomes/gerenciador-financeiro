using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Commands.Transaction;

namespace GestorFinanceiro.Financeiro.Application.Commands.Transaction;

public class CreateTransactionValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("AccountId is required");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid transaction type");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.CompetenceDate)
            .NotEmpty().WithMessage("CompetenceDate is required");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid transaction status");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");
    }
}
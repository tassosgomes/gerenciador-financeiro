using FluentValidation;

namespace GestorFinanceiro.Financeiro.Application.Commands.Invoice;

public class PayInvoiceCommandValidator : AbstractValidator<PayInvoiceCommand>
{
    public PayInvoiceCommandValidator()
    {
        RuleFor(x => x.CreditCardAccountId)
            .NotEmpty().WithMessage("Credit card account ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.CompetenceDate)
            .NotEmpty().WithMessage("Competence date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Competence date cannot be in the future.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}

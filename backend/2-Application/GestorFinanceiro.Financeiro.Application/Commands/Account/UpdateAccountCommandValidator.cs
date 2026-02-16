using FluentValidation;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Commands.Account;

public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("AccountId is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        // Regras condicionais para quando os campos de cartão são fornecidos
        When(x => x.CreditLimit.HasValue, () =>
        {
            RuleFor(x => x.CreditLimit!.Value)
                .GreaterThan(0).WithMessage("Limite deve ser maior que zero.");
        });

        When(x => x.ClosingDay.HasValue, () =>
        {
            RuleFor(x => x.ClosingDay!.Value)
                .InclusiveBetween(1, 28).WithMessage("Dia de fechamento deve estar entre 1 e 28.");
        });

        When(x => x.DueDay.HasValue, () =>
        {
            RuleFor(x => x.DueDay!.Value)
                .InclusiveBetween(1, 28).WithMessage("Dia de vencimento deve estar entre 1 e 28.");
        });

        When(x => x.DebitAccountId.HasValue, () =>
        {
            RuleFor(x => x.DebitAccountId!.Value)
                .NotEqual(Guid.Empty).WithMessage("Conta de débito é obrigatória.");
        });
    }
}

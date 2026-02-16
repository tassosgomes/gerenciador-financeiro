using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Commands.Account;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Commands.Account;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid account type");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        // Regras específicas para Cartão de Crédito
        When(x => x.Type == AccountType.Cartao, () =>
        {
            RuleFor(x => x.CreditLimit)
                .NotNull().WithMessage("Limite de crédito é obrigatório para cartão.")
                .GreaterThan(0).WithMessage("Limite deve ser maior que zero.");

            RuleFor(x => x.ClosingDay)
                .NotNull().WithMessage("Dia de fechamento é obrigatório.")
                .InclusiveBetween(1, 28).WithMessage("Dia de fechamento deve estar entre 1 e 28.");

            RuleFor(x => x.DueDay)
                .NotNull().WithMessage("Dia de vencimento é obrigatório.")
                .InclusiveBetween(1, 28).WithMessage("Dia de vencimento deve estar entre 1 e 28.");

            RuleFor(x => x.DebitAccountId)
                .NotNull().WithMessage("Conta de débito é obrigatória.")
                .NotEqual(Guid.Empty).WithMessage("Conta de débito é obrigatória.");
        });

        // Regras para outros tipos de conta
        When(x => x.Type != AccountType.Cartao, () =>
        {
            RuleFor(x => x.InitialBalance)
                .GreaterThanOrEqualTo(0).WithMessage("Initial balance must be greater than or equal to 0");
        });
    }
}

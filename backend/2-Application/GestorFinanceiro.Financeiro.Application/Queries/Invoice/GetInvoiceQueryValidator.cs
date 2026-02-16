using FluentValidation;

namespace GestorFinanceiro.Financeiro.Application.Queries.Invoice;

public class GetInvoiceQueryValidator : AbstractValidator<GetInvoiceQuery>
{
    public GetInvoiceQueryValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("AccountId não pode ser vazio.");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("Month deve estar entre 1 e 12.");

        RuleFor(x => x.Year)
            .GreaterThan(0)
            .WithMessage("Year deve ser maior que 0.");
    }
}

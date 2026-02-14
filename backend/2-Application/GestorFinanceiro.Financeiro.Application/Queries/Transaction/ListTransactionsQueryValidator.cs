using FluentValidation;

namespace GestorFinanceiro.Financeiro.Application.Queries.Transaction;

public class ListTransactionsQueryValidator : AbstractValidator<ListTransactionsQuery>
{
    public ListTransactionsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(query => query.Size)
            .InclusiveBetween(1, 100)
            .WithMessage("Size must be between 1 and 100.");

        RuleFor(query => query.Type)
            .IsInEnum()
            .When(query => query.Type.HasValue)
            .WithMessage("Type must be a valid transaction type.");

        RuleFor(query => query.Status)
            .IsInEnum()
            .When(query => query.Status.HasValue)
            .WithMessage("Status must be a valid transaction status.");

        RuleFor(query => query)
            .Must(query => !query.CompetenceDateFrom.HasValue || !query.CompetenceDateTo.HasValue || query.CompetenceDateFrom <= query.CompetenceDateTo)
            .WithMessage("CompetenceDateFrom must be less than or equal to CompetenceDateTo.");

        RuleFor(query => query)
            .Must(query => !query.DueDateFrom.HasValue || !query.DueDateTo.HasValue || query.DueDateFrom <= query.DueDateTo)
            .WithMessage("DueDateFrom must be less than or equal to DueDateTo.");
    }
}

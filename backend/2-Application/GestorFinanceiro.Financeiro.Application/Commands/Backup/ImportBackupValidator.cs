using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Dtos.Backup;

namespace GestorFinanceiro.Financeiro.Application.Commands.Backup;

public class ImportBackupValidator : AbstractValidator<ImportBackupCommand>
{
    public ImportBackupValidator()
    {
        RuleFor(command => command.Data).NotNull().WithMessage("Backup data is required.");

        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data.Users).NotNull().WithMessage("Users section is required.");
            RuleFor(command => command.Data.Accounts).NotNull().WithMessage("Accounts section is required.");
            RuleFor(command => command.Data.Categories).NotNull().WithMessage("Categories section is required.");
            RuleFor(command => command.Data.Transactions).NotNull().WithMessage("Transactions section is required.");
            RuleFor(command => command.Data.RecurrenceTemplates).NotNull().WithMessage("RecurrenceTemplates section is required.");

            RuleForEach(command => command.Data.Users).ChildRules(user =>
            {
                user.RuleFor(item => item.Id).NotEmpty();
                user.RuleFor(item => item.Name).NotEmpty().MaximumLength(150);
                user.RuleFor(item => item.Email).NotEmpty().EmailAddress().MaximumLength(255);
                user.RuleFor(item => item.Role).IsInEnum();
                user.RuleFor(item => item.CreatedBy).NotEmpty();
            });

            RuleForEach(command => command.Data.Accounts).ChildRules(account =>
            {
                account.RuleFor(item => item.Id).NotEmpty();
                account.RuleFor(item => item.Name).NotEmpty().MaximumLength(150);
                account.RuleFor(item => item.Type).IsInEnum();
                account.RuleFor(item => item.CreatedBy).NotEmpty();
            });

            RuleForEach(command => command.Data.Categories).ChildRules(category =>
            {
                category.RuleFor(item => item.Id).NotEmpty();
                category.RuleFor(item => item.Name).NotEmpty().MaximumLength(150);
                category.RuleFor(item => item.Type).IsInEnum();
                category.RuleFor(item => item.CreatedBy).NotEmpty();
            });

            RuleForEach(command => command.Data.Transactions).ChildRules(transaction =>
            {
                transaction.RuleFor(item => item.Id).NotEmpty();
                transaction.RuleFor(item => item.AccountId).NotEmpty();
                transaction.RuleFor(item => item.CategoryId).NotEmpty();
                transaction.RuleFor(item => item.Type).IsInEnum();
                transaction.RuleFor(item => item.Status).IsInEnum();
                transaction.RuleFor(item => item.Description).NotEmpty().MaximumLength(500);
                transaction.RuleFor(item => item.Amount).GreaterThan(0);
                transaction.RuleFor(item => item.CreatedBy).NotEmpty();
            });

            RuleForEach(command => command.Data.RecurrenceTemplates).ChildRules(template =>
            {
                template.RuleFor(item => item.Id).NotEmpty();
                template.RuleFor(item => item.AccountId).NotEmpty();
                template.RuleFor(item => item.CategoryId).NotEmpty();
                template.RuleFor(item => item.Type).IsInEnum();
                template.RuleFor(item => item.DefaultStatus).IsInEnum();
                template.RuleFor(item => item.Amount).GreaterThan(0);
                template.RuleFor(item => item.DayOfMonth).InclusiveBetween(1, 31);
                template.RuleFor(item => item.Description).NotEmpty().MaximumLength(500);
                template.RuleFor(item => item.CreatedBy).NotEmpty();
            });

            RuleFor(command => command.Data.Transactions)
                .Must(NoDuplicatedIds)
                .WithMessage("Transactions section contains duplicated ids.");
        });
    }

    private static bool NoDuplicatedIds(IReadOnlyList<TransactionBackupDto>? transactions)
    {
        if (transactions is null)
        {
            return true;
        }

        return transactions.Select(transaction => transaction.Id).Distinct().Count() == transactions.Count;
    }
}

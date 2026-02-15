using GestorFinanceiro.Financeiro.Application.Dtos.Backup;

namespace GestorFinanceiro.Financeiro.Application.Common;

public class BackupIntegrityValidator : IBackupIntegrityValidator
{
    public IReadOnlyList<string> Validate(BackupDataDto data)
    {
        var errors = new List<string>();

        ValidateDuplicates(data, errors);
        ValidateTransactionReferences(data, errors);
        ValidateRecurrenceTemplateReferences(data, errors);
        ValidateTransferGroups(data, errors);

        return errors;
    }

    private static void ValidateDuplicates(BackupDataDto data, ICollection<string> errors)
    {
        ValidateDuplicates(data.Users.Select(user => user.Id), "users", errors);
        ValidateDuplicates(data.Accounts.Select(account => account.Id), "accounts", errors);
        ValidateDuplicates(data.Categories.Select(category => category.Id), "categories", errors);
        ValidateDuplicates(data.Transactions.Select(transaction => transaction.Id), "transactions", errors);
        ValidateDuplicates(data.RecurrenceTemplates.Select(template => template.Id), "recurrenceTemplates", errors);
    }

    private static void ValidateDuplicates(IEnumerable<Guid> ids, string section, ICollection<string> errors)
    {
        var duplicatedIds = ids
            .GroupBy(id => id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        foreach (var duplicatedId in duplicatedIds)
        {
            errors.Add($"Duplicate id '{duplicatedId}' found in section '{section}'.");
        }
    }

    private static void ValidateTransactionReferences(BackupDataDto data, ICollection<string> errors)
    {
        var accountIds = data.Accounts.Select(account => account.Id).ToHashSet();
        var categoryIds = data.Categories.Select(category => category.Id).ToHashSet();
        var transactionIds = data.Transactions.Select(transaction => transaction.Id).ToHashSet();
        var recurrenceTemplateIds = data.RecurrenceTemplates.Select(template => template.Id).ToHashSet();

        foreach (var transaction in data.Transactions)
        {
            if (!accountIds.Contains(transaction.AccountId))
            {
                errors.Add($"Transaction '{transaction.Id}' references unknown account '{transaction.AccountId}'.");
            }

            if (!categoryIds.Contains(transaction.CategoryId))
            {
                errors.Add($"Transaction '{transaction.Id}' references unknown category '{transaction.CategoryId}'.");
            }

            if (transaction.OriginalTransactionId.HasValue && !transactionIds.Contains(transaction.OriginalTransactionId.Value))
            {
                errors.Add($"Transaction '{transaction.Id}' references unknown original transaction '{transaction.OriginalTransactionId}'.");
            }

            if (transaction.RecurrenceTemplateId.HasValue && !recurrenceTemplateIds.Contains(transaction.RecurrenceTemplateId.Value))
            {
                errors.Add($"Transaction '{transaction.Id}' references unknown recurrence template '{transaction.RecurrenceTemplateId}'.");
            }
        }
    }

    private static void ValidateRecurrenceTemplateReferences(BackupDataDto data, ICollection<string> errors)
    {
        var accountIds = data.Accounts.Select(account => account.Id).ToHashSet();
        var categoryIds = data.Categories.Select(category => category.Id).ToHashSet();

        foreach (var template in data.RecurrenceTemplates)
        {
            if (!accountIds.Contains(template.AccountId))
            {
                errors.Add($"RecurrenceTemplate '{template.Id}' references unknown account '{template.AccountId}'.");
            }

            if (!categoryIds.Contains(template.CategoryId))
            {
                errors.Add($"RecurrenceTemplate '{template.Id}' references unknown category '{template.CategoryId}'.");
            }
        }
    }

    private static void ValidateTransferGroups(BackupDataDto data, ICollection<string> errors)
    {
        var transferGroups = data.Transactions
            .Where(transaction => transaction.TransferGroupId.HasValue)
            .GroupBy(transaction => transaction.TransferGroupId!.Value)
            .ToList();

        foreach (var transferGroup in transferGroups)
        {
            var distinctAccounts = transferGroup.Select(transaction => transaction.AccountId).Distinct().Count();

            if (distinctAccounts < 2)
            {
                errors.Add($"Transfer group '{transferGroup.Key}' must reference at least two different accounts.");
            }
        }
    }
}

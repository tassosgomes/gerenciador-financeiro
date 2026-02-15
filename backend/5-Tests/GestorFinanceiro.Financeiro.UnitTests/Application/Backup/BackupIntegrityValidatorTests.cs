using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos.Backup;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Backup;

public class BackupIntegrityValidatorTests
{
    private readonly IBackupIntegrityValidator _sut = new BackupIntegrityValidator();

    [Fact]
    public void Validate_WithConsistentData_ShouldReturnNoErrors()
    {
        var data = BuildValidData();

        var result = _sut.Validate(data);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithTransactionReferencingUnknownAccount_ShouldReturnError()
    {
        var data = BuildValidData();
        data = data with
        {
            Transactions =
            [
                data.Transactions[0] with { AccountId = Guid.NewGuid() }
            ]
        };

        var result = _sut.Validate(data);

        result.Should().Contain(error => error.Contains("unknown account"));
    }

    [Fact]
    public void Validate_WithTransactionReferencingUnknownCategory_ShouldReturnError()
    {
        var data = BuildValidData();
        data = data with
        {
            Transactions =
            [
                data.Transactions[0] with { CategoryId = Guid.NewGuid() }
            ]
        };

        var result = _sut.Validate(data);

        result.Should().Contain(error => error.Contains("unknown category"));
    }

    [Fact]
    public void Validate_WithDuplicatedIds_ShouldReturnError()
    {
        var data = BuildValidData();
        var duplicated = data.Transactions[0];
        data = data with
        {
            Transactions = [duplicated, duplicated]
        };

        var result = _sut.Validate(data);

        result.Should().Contain(error => error.Contains("Duplicate id"));
    }

    private static BackupDataDto BuildValidData()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var recurrenceTemplateId = Guid.NewGuid();

        return new BackupDataDto(
            [new UserBackupDto(userId, "Admin", "admin@test.com", UserRole.Admin, true, true, "system", DateTime.UtcNow, null, null)],
            [new AccountBackupDto(accountId, "Conta", AccountType.Corrente, 100m, true, true, "system", DateTime.UtcNow, null, null)],
            [new CategoryBackupDto(categoryId, "Receita", CategoryType.Receita, true, "system", DateTime.UtcNow, null, null)],
            [
                new TransactionBackupDto(
                    transactionId,
                    accountId,
                    categoryId,
                    TransactionType.Credit,
                    10m,
                    "Movimento",
                    DateTime.UtcNow.Date,
                    null,
                    TransactionStatus.Pending,
                    false,
                    null,
                    false,
                    null,
                    null,
                    null,
                    false,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "system",
                    DateTime.UtcNow,
                    null,
                    null)
            ],
            [
                new RecurrenceTemplateBackupDto(
                    recurrenceTemplateId,
                    accountId,
                    categoryId,
                    TransactionType.Credit,
                    10m,
                    "Template",
                    10,
                    true,
                    null,
                    TransactionStatus.Pending,
                    "system",
                    DateTime.UtcNow,
                    null,
                    null)
            ]);
    }
}

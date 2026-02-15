using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Queries.Backup;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Moq;
using DomainAccount = GestorFinanceiro.Financeiro.Domain.Entity.Account;
using DomainCategory = GestorFinanceiro.Financeiro.Domain.Entity.Category;
using DomainRecurrenceTemplate = GestorFinanceiro.Financeiro.Domain.Entity.RecurrenceTemplate;
using DomainTransaction = GestorFinanceiro.Financeiro.Domain.Entity.Transaction;
using DomainUser = GestorFinanceiro.Financeiro.Domain.Entity.User;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Backup;

public class ExportBackupQueryHandlerTests
{
    private readonly Mock<IBackupRepository> _backupRepositoryMock = new();

    [Fact]
    public async Task HandleAsync_WithData_ShouldReturnAllSections()
    {
        var user = DomainUser.Create("Admin", "admin@test.com", "hash", UserRole.Admin, "system");
        var account = DomainAccount.Create("Conta", AccountType.Corrente, 100m, true, "system");
        var category = DomainCategory.Create("Categoria", CategoryType.Receita, "system");
        var transaction = DomainTransaction.Create(account.Id, category.Id, TransactionType.Credit, 10m, "Desc", DateTime.UtcNow.Date, null, TransactionStatus.Pending, "system");
        var recurrenceTemplate = DomainRecurrenceTemplate.Create(account.Id, category.Id, TransactionType.Credit, 10m, "Template", 5, TransactionStatus.Pending, "system");

        _backupRepositoryMock.Setup(mock => mock.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync([user]);
        _backupRepositoryMock.Setup(mock => mock.GetAccountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([account]);
        _backupRepositoryMock.Setup(mock => mock.GetCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([category]);
        _backupRepositoryMock.Setup(mock => mock.GetTransactionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([transaction]);
        _backupRepositoryMock.Setup(mock => mock.GetRecurrenceTemplatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([recurrenceTemplate]);

        var sut = new ExportBackupQueryHandler(_backupRepositoryMock.Object);

        var result = await sut.HandleAsync(new ExportBackupQuery(), CancellationToken.None);

        result.Version.Should().Be("1.0");
        result.Data.Users.Should().HaveCount(1);
        result.Data.Accounts.Should().HaveCount(1);
        result.Data.Categories.Should().HaveCount(1);
        result.Data.Transactions.Should().HaveCount(1);
        result.Data.RecurrenceTemplates.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WithData_ShouldExcludePasswordHashByContract()
    {
        var user = DomainUser.Create("Admin", "admin@test.com", "secret-hash", UserRole.Admin, "system");
        _backupRepositoryMock.Setup(mock => mock.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync([user]);
        _backupRepositoryMock.Setup(mock => mock.GetAccountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _backupRepositoryMock.Setup(mock => mock.GetCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _backupRepositoryMock.Setup(mock => mock.GetTransactionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _backupRepositoryMock.Setup(mock => mock.GetRecurrenceTemplatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var sut = new ExportBackupQueryHandler(_backupRepositoryMock.Object);

        var result = await sut.HandleAsync(new ExportBackupQuery(), CancellationToken.None);

        result.Data.Users.Single().Email.Should().Be("admin@test.com");
        typeof(GestorFinanceiro.Financeiro.Application.Dtos.Backup.UserBackupDto)
            .GetProperties()
            .Any(property => property.Name.Equals("PasswordHash", StringComparison.OrdinalIgnoreCase))
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WithNoData_ShouldReturnEmptyCollections()
    {
        _backupRepositoryMock.Setup(mock => mock.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _backupRepositoryMock.Setup(mock => mock.GetAccountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _backupRepositoryMock.Setup(mock => mock.GetCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _backupRepositoryMock.Setup(mock => mock.GetTransactionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _backupRepositoryMock.Setup(mock => mock.GetRecurrenceTemplatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var sut = new ExportBackupQueryHandler(_backupRepositoryMock.Object);

        var result = await sut.HandleAsync(new ExportBackupQuery(), CancellationToken.None);

        result.Data.Users.Should().BeEmpty();
        result.Data.Accounts.Should().BeEmpty();
        result.Data.Categories.Should().BeEmpty();
        result.Data.Transactions.Should().BeEmpty();
        result.Data.RecurrenceTemplates.Should().BeEmpty();
    }
}

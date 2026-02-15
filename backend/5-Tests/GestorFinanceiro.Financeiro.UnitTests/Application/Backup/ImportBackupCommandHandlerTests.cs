using AwesomeAssertions;
using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Commands.Backup;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos.Backup;
using GestorFinanceiro.Financeiro.Application.Mapping;
using GestorFinanceiro.Financeiro.Application.Services;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Backup;

public class ImportBackupCommandHandlerTests
{
    private readonly Mock<IBackupRepository> _backupRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly IValidator<ImportBackupCommand> _validator = new ImportBackupValidator();
    private readonly IBackupIntegrityValidator _integrityValidator = new BackupIntegrityValidator();

    public ImportBackupCommandHandlerTests()
    {
        MappingConfig.ConfigureMappings();
        _passwordHasherMock.Setup(mock => mock.Hash(It.IsAny<string>())).Returns("hashed-temp-password");
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ShouldImportAllEntities()
    {
        var command = new ImportBackupCommand(BuildValidData());
        var sut = CreateSut();

        var result = await sut.HandleAsync(command, CancellationToken.None);

        result.Users.Should().Be(1);
        result.Accounts.Should().Be(1);
        result.Categories.Should().Be(1);
        result.RecurrenceTemplates.Should().Be(1);
        result.Transactions.Should().Be(1);
        _backupRepositoryMock.Verify(mock => mock.TruncateAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _backupRepositoryMock.Verify(
            mock => mock.ImportAsync(
                It.IsAny<IReadOnlyCollection<GestorFinanceiro.Financeiro.Domain.Entity.User>>(),
                It.IsAny<IReadOnlyCollection<GestorFinanceiro.Financeiro.Domain.Entity.Account>>(),
                It.IsAny<IReadOnlyCollection<GestorFinanceiro.Financeiro.Domain.Entity.Category>>(),
                It.IsAny<IReadOnlyCollection<GestorFinanceiro.Financeiro.Domain.Entity.RecurrenceTemplate>>(),
                It.IsAny<IReadOnlyCollection<GestorFinanceiro.Financeiro.Domain.Entity.Transaction>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidReference_ShouldThrowValidationException()
    {
        var data = BuildValidData();
        data = data with
        {
            Transactions = [data.Transactions[0] with { AccountId = Guid.NewGuid() }]
        };

        var sut = CreateSut();
        var action = () => sut.HandleAsync(new ImportBackupCommand(data), CancellationToken.None);

        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage("*unknown account*");
        _backupRepositoryMock.Verify(mock => mock.TruncateAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenImportFails_ShouldRollbackTransaction()
    {
        var command = new ImportBackupCommand(BuildValidData());
        _backupRepositoryMock
            .Setup(mock => mock.ImportAsync(
                It.IsAny<IReadOnlyCollection<GestorFinanceiro.Financeiro.Domain.Entity.User>>(),
                It.IsAny<IReadOnlyCollection<GestorFinanceiro.Financeiro.Domain.Entity.Account>>(),
                It.IsAny<IReadOnlyCollection<GestorFinanceiro.Financeiro.Domain.Entity.Category>>(),
                It.IsAny<IReadOnlyCollection<GestorFinanceiro.Financeiro.Domain.Entity.RecurrenceTemplate>>(),
                It.IsAny<IReadOnlyCollection<GestorFinanceiro.Financeiro.Domain.Entity.Transaction>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("import failed"));

        var sut = CreateSut();
        var action = () => sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _unitOfWorkMock.Verify(mock => mock.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private ImportBackupCommandHandler CreateSut()
    {
        _unitOfWorkMock.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWorkMock.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        return new ImportBackupCommandHandler(
            _backupRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _validator,
            _integrityValidator);
    }

    private static BackupDataDto BuildValidData()
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var recurrenceTemplateId = Guid.NewGuid();

        return new BackupDataDto(
            [new UserBackupDto(userId, "Admin", "admin@test.com", UserRole.Admin, true, true, "system", now, null, null)],
            [new AccountBackupDto(accountId, "Conta", AccountType.Corrente, 100m, true, true, "system", now, null, null)],
            [new CategoryBackupDto(categoryId, "Categoria", CategoryType.Receita, true, "system", now, null, null)],
            [
                new TransactionBackupDto(
                    transactionId,
                    accountId,
                    categoryId,
                    TransactionType.Credit,
                    10m,
                    "Movimento",
                    now.Date,
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
                    now,
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
                    5,
                    true,
                    null,
                    TransactionStatus.Pending,
                    "system",
                    now,
                    null,
                    null)
            ]);
    }
}

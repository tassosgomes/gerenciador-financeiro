using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Commands.Receipt;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Dto;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Receipt;

public class ImportNfceCommandHandlerTests
{
    private const string AccessKey = "12345678901234567890123456789012345678901234";

    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IEstablishmentRepository> _establishmentRepository = new();
    private readonly Mock<IReceiptItemRepository> _receiptItemRepository = new();
    private readonly Mock<IOperationLogRepository> _operationLogRepository = new();
    private readonly Mock<ISefazNfceService> _sefazNfceService = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<ImportNfceCommandHandler>> _logger = new();

    private readonly ImportNfceCommandHandler _sut;

    public ImportNfceCommandHandlerTests()
    {
        _auditService
            .Setup(mock => mock.LogAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _transactionRepository
            .Setup(mock => mock.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction transaction, CancellationToken _) => transaction);

        _establishmentRepository
            .Setup(mock => mock.AddAsync(It.IsAny<Establishment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Establishment establishment, CancellationToken _) => establishment);

        _sut = new ImportNfceCommandHandler(
            _accountRepository.Object,
            _categoryRepository.Object,
            _transactionRepository.Object,
            _establishmentRepository.Object,
            _receiptItemRepository.Object,
            _operationLogRepository.Object,
            _sefazNfceService.Object,
            _auditService.Object,
            _unitOfWork.Object,
            new TransactionDomainService(),
            new ImportNfceValidator(),
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ImportsNfceWithTransactionEstablishmentAndItems()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 500m, false, "user-1");
        var category = Category.Create("Mercado", CategoryType.Despesa, "user-1");
        var command = CreateCommand(account.Id, category.Id);

        _establishmentRepository.Setup(mock => mock.ExistsByAccessKeyAsync(AccessKey, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _sefazNfceService.Setup(mock => mock.LookupAsync(AccessKey, It.IsAny<CancellationToken>())).ReturnsAsync(CreateNfceData(10m));
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _categoryRepository.Setup(mock => mock.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Transaction.Amount.Should().Be(90m);
        response.Transaction.HasReceipt.Should().BeTrue();
        response.Establishment.AccessKey.Should().Be(AccessKey);
        response.Items.Should().HaveCount(2);
        _receiptItemRepository.Verify(mock => mock.AddRangeAsync(It.IsAny<IEnumerable<ReceiptItem>>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _auditService.Verify(mock => mock.LogAsync("Transaction", It.IsAny<Guid>(), "ImportedNfce", "user-1", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithDiscount_UsesPaidAmountAndIncludesDiscountNoteOnDescription()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 500m, false, "user-1");
        var category = Category.Create("Mercado", CategoryType.Despesa, "user-1");
        var command = CreateCommand(account.Id, category.Id, "Compra mensal");

        _establishmentRepository.Setup(mock => mock.ExistsByAccessKeyAsync(AccessKey, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _sefazNfceService.Setup(mock => mock.LookupAsync(AccessKey, It.IsAny<CancellationToken>())).ReturnsAsync(CreateNfceData(12.5m));
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _categoryRepository.Setup(mock => mock.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Transaction.Amount.Should().Be(87.5m);
        response.Transaction.Description.Should().Contain("Desconto de");
        response.Transaction.Description.Should().Contain("Valor original");
    }

    [Fact]
    public async Task HandleAsync_DuplicateAccessKey_ThrowsDuplicateReceiptException()
    {
        var command = CreateCommand(Guid.NewGuid(), Guid.NewGuid());
        _establishmentRepository.Setup(mock => mock.ExistsByAccessKeyAsync(AccessKey, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<DuplicateReceiptException>();
    }

    [Fact]
    public async Task HandleAsync_WhenAccountDoesNotExist_ThrowsAccountNotFoundException()
    {
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var command = CreateCommand(accountId, categoryId);

        _establishmentRepository.Setup(mock => mock.ExistsByAccessKeyAsync(AccessKey, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _sefazNfceService.Setup(mock => mock.LookupAsync(AccessKey, It.IsAny<CancellationToken>())).ReturnsAsync(CreateNfceData(0m));
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(accountId, It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<AccountNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_WhenCategoryDoesNotExist_ThrowsCategoryNotFoundException()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 500m, false, "user-1");
        var categoryId = Guid.NewGuid();
        var command = CreateCommand(account.Id, categoryId);

        _establishmentRepository.Setup(mock => mock.ExistsByAccessKeyAsync(AccessKey, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _sefazNfceService.Setup(mock => mock.LookupAsync(AccessKey, It.IsAny<CancellationToken>())).ReturnsAsync(CreateNfceData(0m));
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _categoryRepository.Setup(mock => mock.GetByIdAsync(categoryId, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<CategoryNotFoundException>();
    }

    private static ImportNfceCommand CreateCommand(Guid accountId, Guid categoryId, string description = "Supermercado")
    {
        return new ImportNfceCommand(
            AccessKey,
            accountId,
            categoryId,
            description,
            DateTime.UtcNow.Date,
            "user-1");
    }

    private static NfceData CreateNfceData(decimal discount)
    {
        var total = 100m;
        var paid = total - discount;
        return new NfceData(
            AccessKey,
            "SUPERMERCADO TESTE",
            "12345678000190",
            DateTime.UtcNow,
            total,
            discount,
            paid,
            [
                new NfceItemData("Item 1", "P1", 1m, "UN", 30m, 30m),
                new NfceItemData("Item 2", "P2", 2m, "UN", 35m, 70m)
            ]);
    }
}

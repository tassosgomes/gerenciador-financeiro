using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Commands.Transaction;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application;

public class CreateTransactionCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IOperationLogRepository> _operationLogRepository = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CreateTransactionCommandHandler>> _logger = new();

    private readonly CreateTransactionCommandHandler _sut;

    public CreateTransactionCommandHandlerTests()
    {
        _auditService.Setup(mock => mock.LogAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _sut = new CreateTransactionCommandHandler(
            _accountRepository.Object,
            _categoryRepository.Object,
            _transactionRepository.Object,
            _operationLogRepository.Object,
            _auditService.Object,
            _unitOfWork.Object,
            new TransactionDomainService(),
            new CreateTransactionValidator(),
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_ComandoValido_CriaTransacaoEAtualizaSaldoDaConta()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");
        var category = Category.Create("Alimentacao", CategoryType.Despesa, "user-1");
        var command = new CreateTransactionCommand(
            account.Id,
            category.Id,
            TransactionType.Debit,
            40m,
            "Mercado",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _categoryRepository.Setup(mock => mock.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _transactionRepository
            .Setup(mock => mock.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction transaction, CancellationToken _) => transaction);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Amount.Should().Be(40m);
        response.Status.Should().Be(TransactionStatus.Paid);
        account.Balance.Should().Be(60m);
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionRepository.Verify(mock => mock.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_OperationIdDuplicado_LancaDuplicateOperationException()
    {
        var command = new CreateTransactionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Debit,
            10m,
            "Teste",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1",
            "operation-1");

        _operationLogRepository
            .Setup(mock => mock.ExistsByOperationIdAsync("operation-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<DuplicateOperationException>();
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ContaInativa_LancaInactiveAccountException()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");
        account.Deactivate("user-1");
        var category = Category.Create("Alimentacao", CategoryType.Despesa, "user-1");
        var command = new CreateTransactionCommand(
            account.Id,
            category.Id,
            TransactionType.Debit,
            20m,
            "Despesa",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _categoryRepository.Setup(mock => mock.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InactiveAccountException>();
        _unitOfWork.Verify(mock => mock.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DespesaEmContaCorrenteSemSaldo_LancaInsufficientBalanceException()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 10m, false, "user-1");
        var category = Category.Create("Alimentacao", CategoryType.Despesa, "user-1");
        var command = new CreateTransactionCommand(
            account.Id,
            category.Id,
            TransactionType.Debit,
            20m,
            "Despesa",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _categoryRepository.Setup(mock => mock.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InsufficientBalanceException>();
        _unitOfWork.Verify(mock => mock.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ContaCartao_ComStatusPendente_ForcaStatusPago()
    {
        var account = Account.Create("Cartao", AccountType.Cartao, 0m, true, "user-1");
        var category = Category.Create("Alimentacao", CategoryType.Despesa, "user-1");
        var command = new CreateTransactionCommand(
            account.Id,
            category.Id,
            TransactionType.Debit,
            40m,
            "Mercado",
            DateTime.UtcNow,
            null,
            TransactionStatus.Pending,
            "user-1");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _categoryRepository.Setup(mock => mock.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _transactionRepository
            .Setup(mock => mock.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction transaction, CancellationToken _) => transaction);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Status.Should().Be(TransactionStatus.Paid);
    }
}

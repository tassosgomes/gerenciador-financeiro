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

public class CancelTransactionCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IReceiptItemRepository> _receiptItemRepository = new();
    private readonly Mock<IEstablishmentRepository> _establishmentRepository = new();
    private readonly Mock<IOperationLogRepository> _operationLogRepository = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CancelTransactionCommandHandler>> _logger = new();

    private readonly CancelTransactionCommandHandler _sut;

    public CancelTransactionCommandHandlerTests()
    {
        _auditService.Setup(mock => mock.LogAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _sut = new CancelTransactionCommandHandler(
            _accountRepository.Object,
            _transactionRepository.Object,
            _receiptItemRepository.Object,
            _establishmentRepository.Object,
            _operationLogRepository.Object,
            _auditService.Object,
            _unitOfWork.Object,
            new TransactionDomainService(),
            _logger.Object);

        _receiptItemRepository
            .Setup(mock => mock.GetByTransactionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _establishmentRepository
            .Setup(mock => mock.GetByTransactionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Establishment?)null);
    }

    [Fact]
    public async Task HandleAsync_TransacaoPaid_CancelaEReverteSaldo()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 70m, false, "user-1");
        var transaction = Transaction.Create(
            account.Id,
            Guid.NewGuid(),
            TransactionType.Debit,
            30m,
            "Despesa",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");
        var command = new CancelTransactionCommand(transaction.Id, "user-2", "Erro de lancamento");

        _transactionRepository.Setup(mock => mock.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.Status.Should().Be(TransactionStatus.Cancelled);
        transaction.Status.Should().Be(TransactionStatus.Cancelled);
        account.Balance.Should().Be(100m);
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TransacaoPending_CancelaSemReverterSaldo()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");
        var transaction = Transaction.Create(
            account.Id,
            Guid.NewGuid(),
            TransactionType.Debit,
            30m,
            "Despesa",
            DateTime.UtcNow,
            null,
            TransactionStatus.Pending,
            "user-1");
        var command = new CancelTransactionCommand(transaction.Id, "user-2");

        _transactionRepository.Setup(mock => mock.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        await _sut.HandleAsync(command, CancellationToken.None);

        transaction.Status.Should().Be(TransactionStatus.Cancelled);
        account.Balance.Should().Be(100m);
    }

    [Fact]
    public async Task HandleAsync_TransacaoJaCancelada_LancaTransactionAlreadyCancelledException()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");
        var transaction = Transaction.Create(
            account.Id,
            Guid.NewGuid(),
            TransactionType.Debit,
            30m,
            "Despesa",
            DateTime.UtcNow,
            null,
            TransactionStatus.Pending,
            "user-1");
        transaction.Cancel("user-1", "Cancelada antes");
        var command = new CancelTransactionCommand(transaction.Id, "user-2");

        _transactionRepository.Setup(mock => mock.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<TransactionAlreadyCancelledException>();
        _unitOfWork.Verify(mock => mock.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TransacaoComCupom_RemoveItensEEstabelecimento()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");
        var transaction = Transaction.Create(
            account.Id,
            Guid.NewGuid(),
            TransactionType.Debit,
            20m,
            "Despesa",
            DateTime.UtcNow,
            null,
            TransactionStatus.Paid,
            "user-1");
        var items = new List<ReceiptItem>
        {
            ReceiptItem.Create(transaction.Id, "Item 1", "I1", 1m, "UN", 10m, 10m, 1, "user-1")
        };
        var establishment = Establishment.Create(
            transaction.Id,
            "Mercado",
            "12345678000190",
            "12345678901234567890123456789012345678901234",
            "user-1");

        _transactionRepository.Setup(mock => mock.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _receiptItemRepository.Setup(mock => mock.GetByTransactionIdAsync(transaction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(items);
        _establishmentRepository.Setup(mock => mock.GetByTransactionIdAsync(transaction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(establishment);

        await _sut.HandleAsync(new CancelTransactionCommand(transaction.Id, "user-2"), CancellationToken.None);

        _receiptItemRepository.Verify(mock => mock.RemoveRange(items), Times.Once);
        _establishmentRepository.Verify(mock => mock.Remove(establishment), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TransacaoSemCupom_MantemComportamentoSemRemocoes()
    {
        var account = Account.Create("Conta", AccountType.Corrente, 100m, false, "user-1");
        var transaction = Transaction.Create(
            account.Id,
            Guid.NewGuid(),
            TransactionType.Debit,
            20m,
            "Despesa",
            DateTime.UtcNow,
            null,
            TransactionStatus.Pending,
            "user-1");

        _transactionRepository.Setup(mock => mock.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _receiptItemRepository.Setup(mock => mock.GetByTransactionIdAsync(transaction.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _establishmentRepository.Setup(mock => mock.GetByTransactionIdAsync(transaction.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Establishment?)null);

        await _sut.HandleAsync(new CancelTransactionCommand(transaction.Id, "user-2"), CancellationToken.None);

        _receiptItemRepository.Verify(mock => mock.RemoveRange(It.IsAny<IEnumerable<ReceiptItem>>()), Times.Never);
        _establishmentRepository.Verify(mock => mock.Remove(It.IsAny<Establishment>()), Times.Never);
    }
}

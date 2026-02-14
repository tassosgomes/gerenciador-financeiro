using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Commands.Transfer;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application;

public class CreateTransferCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IOperationLogRepository> _operationLogRepository = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CreateTransferCommandHandler>> _logger = new();

    private readonly CreateTransferCommandHandler _sut;

    public CreateTransferCommandHandlerTests()
    {
        _auditService.Setup(mock => mock.LogAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _transactionRepository
            .Setup(mock => mock.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction transaction, CancellationToken _) => transaction);

        _sut = new CreateTransferCommandHandler(
            _accountRepository.Object,
            _transactionRepository.Object,
            _operationLogRepository.Object,
            _auditService.Object,
            _unitOfWork.Object,
            new TransferDomainService(new TransactionDomainService()),
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_ComandoValido_CriaDebitoCreditoEAtualizaSaldos()
    {
        var sourceAccount = Account.Create("Origem", AccountType.Corrente, 100m, false, "user-1");
        var destinationAccount = Account.Create("Destino", AccountType.Corrente, 50m, false, "user-1");
        var command = new CreateTransferCommand(
            sourceAccount.Id,
            destinationAccount.Id,
            Guid.NewGuid(),
            30m,
            "Transferencia",
            DateTime.UtcNow,
            "user-1");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(sourceAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sourceAccount);
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(destinationAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(destinationAccount);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Should().HaveCount(2);
        sourceAccount.Balance.Should().Be(70m);
        destinationAccount.Balance.Should().Be(80m);
        _transactionRepository.Verify(mock => mock.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_SaldoInsuficienteNaContaOrigem_LancaInsufficientBalanceException()
    {
        var sourceAccount = Account.Create("Origem", AccountType.Corrente, 10m, false, "user-1");
        var destinationAccount = Account.Create("Destino", AccountType.Corrente, 50m, false, "user-1");
        var command = new CreateTransferCommand(
            sourceAccount.Id,
            destinationAccount.Id,
            Guid.NewGuid(),
            30m,
            "Transferencia",
            DateTime.UtcNow,
            "user-1");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(sourceAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sourceAccount);
        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(destinationAccount.Id, It.IsAny<CancellationToken>())).ReturnsAsync(destinationAccount);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InsufficientBalanceException>();
        _unitOfWork.Verify(mock => mock.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

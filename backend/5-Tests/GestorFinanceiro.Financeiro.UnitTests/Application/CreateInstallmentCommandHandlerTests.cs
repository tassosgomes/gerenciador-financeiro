using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Commands.Installment;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application;

public class CreateInstallmentCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IOperationLogRepository> _operationLogRepository = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CreateInstallmentCommandHandler>> _logger = new();

    private readonly CreateInstallmentCommandHandler _sut;

    public CreateInstallmentCommandHandlerTests()
    {
        _auditService.Setup(mock => mock.LogAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _transactionRepository
            .Setup(mock => mock.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction transaction, CancellationToken _) => transaction);

        _sut = new CreateInstallmentCommandHandler(
            _accountRepository.Object,
            _transactionRepository.Object,
            _operationLogRepository.Object,
            _auditService.Object,
            _unitOfWork.Object,
            new InstallmentDomainService(new TransactionDomainService()),
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_ComandoValido_CriaMultiplasParcelasComValoresCorretos()
    {
        var account = Account.Create("Cartao", AccountType.Cartao, 0m, true, "user-1");
        var command = new CreateInstallmentCommand(
            account.Id,
            Guid.NewGuid(),
            TransactionType.Debit,
            120m,
            3,
            "Compra parcelada",
            new DateTime(2026, 1, 10),
            new DateTime(2026, 1, 15),
            "user-1");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Should().HaveCount(3);
        response.Select(item => item.Amount).Should().BeEquivalentTo([40m, 40m, 40m]);
        response.Select(item => item.InstallmentNumber).Should().BeEquivalentTo([1, 2, 3]);
        _transactionRepository.Verify(mock => mock.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task HandleAsync_ValorComArredondamento_AplicaDiferencaNaUltimaParcela()
    {
        var account = Account.Create("Cartao", AccountType.Cartao, 0m, true, "user-1");
        var command = new CreateInstallmentCommand(
            account.Id,
            Guid.NewGuid(),
            TransactionType.Debit,
            100m,
            3,
            "Compra parcelada",
            new DateTime(2026, 1, 10),
            new DateTime(2026, 1, 15),
            "user-1");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Should().HaveCount(3);
        response[0].Amount.Should().Be(33.33m);
        response[1].Amount.Should().Be(33.33m);
        response[2].Amount.Should().Be(33.34m);
    }
}

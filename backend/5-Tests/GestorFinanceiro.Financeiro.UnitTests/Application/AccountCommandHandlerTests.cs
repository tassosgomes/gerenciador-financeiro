using AwesomeAssertions;
using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Commands.Account;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application;

public class AccountCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<IOperationLogRepository> _operationLogRepository = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CreateAccountCommandHandler>> _logger = new();

    private readonly CreateAccountCommandHandler _sut;

    public AccountCommandHandlerTests()
    {
        _auditService.Setup(mock => mock.LogAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _accountRepository
            .Setup(mock => mock.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account account, CancellationToken _) => account);

        _sut = new CreateAccountCommandHandler(
            _accountRepository.Object,
            _operationLogRepository.Object,
            _auditService.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_ComandoValido_CriaContaComSucesso()
    {
        var command = new CreateAccountCommand("Conta Principal", AccountType.Corrente, 100m, false, "user-1");

        _accountRepository.Setup(mock => mock.ExistsByNameAsync("Conta Principal", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Name.Should().Be("Conta Principal");
        response.Balance.Should().Be(100m);
        _accountRepository.Verify(mock => mock.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ComandoInvalido_LancaValidationException()
    {
        var command = new CreateAccountCommand(string.Empty, AccountType.Corrente, -1m, false, string.Empty);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<ValidationException>();
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithTypeCarto_ShouldCreateCreditCardAccount()
    {
        var debitAccountId = Guid.NewGuid();
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 1000m, false, "user-1");
        typeof(Account).GetProperty("Id")!.SetValue(debitAccount, debitAccountId);

        var command = new CreateAccountCommand(
            "Cartão Visa",
            AccountType.Cartao,
            0m,
            false,
            "user-1",
            null,
            5000m,
            15,
            25,
            debitAccountId,
            true);

        _accountRepository.Setup(mock => mock.ExistsByNameAsync("Cartão Visa", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _accountRepository.Setup(mock => mock.GetByIdAsync(debitAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(debitAccount);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Name.Should().Be("Cartão Visa");
        response.Type.Should().Be(AccountType.Cartao);
        response.CreditCard.Should().NotBeNull();
        response.CreditCard!.CreditLimit.Should().Be(5000m);
        response.CreditCard.ClosingDay.Should().Be(15);
        response.CreditCard.DueDay.Should().Be(25);
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithTypeCarto_ShouldSetBalanceToZero()
    {
        var debitAccountId = Guid.NewGuid();
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 1000m, false, "user-1");
        typeof(Account).GetProperty("Id")!.SetValue(debitAccount, debitAccountId);

        var command = new CreateAccountCommand(
            "Cartão Master",
            AccountType.Cartao,
            500m,
            false,
            "user-1",
            null,
            3000m,
            10,
            20,
            debitAccountId,
            true);

        _accountRepository.Setup(mock => mock.ExistsByNameAsync("Cartão Master", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _accountRepository.Setup(mock => mock.GetByIdAsync(debitAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(debitAccount);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Balance.Should().Be(0m);
        response.AllowNegativeBalance.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithTypeCarto_InvalidDebitAccountId_ShouldThrow()
    {
        var command = new CreateAccountCommand(
            "Cartão Visa",
            AccountType.Cartao,
            0m,
            false,
            "user-1",
            null,
            5000m,
            15,
            25,
            Guid.NewGuid(),
            true);

        _accountRepository.Setup(mock => mock.ExistsByNameAsync("Cartão Visa", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _accountRepository.Setup(mock => mock.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<GestorFinanceiro.Financeiro.Domain.Exception.AccountNotFoundException>();
        _unitOfWork.Verify(mock => mock.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithTypeCarto_DebitAccountInactive_ShouldThrow()
    {
        var debitAccountId = Guid.NewGuid();
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 1000m, false, "user-1");
        typeof(Account).GetProperty("Id")!.SetValue(debitAccount, debitAccountId);
        debitAccount.Deactivate("user-1");

        var command = new CreateAccountCommand(
            "Cartão Visa",
            AccountType.Cartao,
            0m,
            false,
            "user-1",
            null,
            5000m,
            15,
            25,
            debitAccountId,
            true);

        _accountRepository.Setup(mock => mock.ExistsByNameAsync("Cartão Visa", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _accountRepository.Setup(mock => mock.GetByIdAsync(debitAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(debitAccount);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<GestorFinanceiro.Financeiro.Domain.Exception.InvalidCreditCardConfigException>();
        _unitOfWork.Verify(mock => mock.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithTypeCarto_DebitAccountIsInvestimento_ShouldThrow()
    {
        var debitAccountId = Guid.NewGuid();
        var debitAccount = Account.Create("Conta Investimento", AccountType.Investimento, 5000m, false, "user-1");
        typeof(Account).GetProperty("Id")!.SetValue(debitAccount, debitAccountId);

        var command = new CreateAccountCommand(
            "Cartão Visa",
            AccountType.Cartao,
            0m,
            false,
            "user-1",
            null,
            5000m,
            15,
            25,
            debitAccountId,
            true);

        _accountRepository.Setup(mock => mock.ExistsByNameAsync("Cartão Visa", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _accountRepository.Setup(mock => mock.GetByIdAsync(debitAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(debitAccount);

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<GestorFinanceiro.Financeiro.Domain.Exception.InvalidCreditCardConfigException>();
        _unitOfWork.Verify(mock => mock.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithTypeCorrente_ShouldMaintainExistingBehavior()
    {
        var command = new CreateAccountCommand("Conta Poupança", AccountType.Corrente, 250m, true, "user-1");

        _accountRepository.Setup(mock => mock.ExistsByNameAsync("Conta Poupança", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Name.Should().Be("Conta Poupança");
        response.Balance.Should().Be(250m);
        response.AllowNegativeBalance.Should().BeTrue();
        response.CreditCard.Should().BeNull();
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithTypeCarto_ShouldAuditLog()
    {
        var debitAccountId = Guid.NewGuid();
        var debitAccount = Account.Create("Conta Corrente", AccountType.Corrente, 1000m, false, "user-1");
        typeof(Account).GetProperty("Id")!.SetValue(debitAccount, debitAccountId);

        var command = new CreateAccountCommand(
            "Cartão Visa",
            AccountType.Cartao,
            0m,
            false,
            "user-1",
            null,
            5000m,
            15,
            25,
            debitAccountId,
            true);

        _accountRepository.Setup(mock => mock.ExistsByNameAsync("Cartão Visa", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _accountRepository.Setup(mock => mock.GetByIdAsync(debitAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(debitAccount);

        await _sut.HandleAsync(command, CancellationToken.None);

        _auditService.Verify(
            mock => mock.LogAsync("Account", It.IsAny<Guid>(), "Created", "user-1", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

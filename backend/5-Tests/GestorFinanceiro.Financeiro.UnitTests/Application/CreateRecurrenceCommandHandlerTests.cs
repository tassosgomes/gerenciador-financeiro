using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Commands.Recurrence;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application;

public class CreateRecurrenceCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IRecurrenceTemplateRepository> _recurrenceTemplateRepository = new();
    private readonly Mock<IOperationLogRepository> _operationLogRepository = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CreateRecurrenceCommandHandler>> _logger = new();

    private readonly CreateRecurrenceCommandHandler _sut;

    public CreateRecurrenceCommandHandlerTests()
    {
        _auditService.Setup(mock => mock.LogAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _recurrenceTemplateRepository
            .Setup(mock => mock.AddAsync(It.IsAny<RecurrenceTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurrenceTemplate template, CancellationToken _) => template);

        _sut = new CreateRecurrenceCommandHandler(
            _accountRepository.Object,
            _categoryRepository.Object,
            _recurrenceTemplateRepository.Object,
            _operationLogRepository.Object,
            _auditService.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_ContaCartao_ComStatusPendente_ForcaStatusPadraoPago()
    {
        var account = Account.Create("Cartao", AccountType.Cartao, 0m, true, "user-1");
        var category = Category.Create("Assinaturas", CategoryType.Despesa, "user-1");
        var command = new CreateRecurrenceCommand(
            account.Id,
            category.Id,
            TransactionType.Debit,
            100m,
            "Plano mensal",
            10,
            TransactionStatus.Pending,
            "user-1");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _categoryRepository.Setup(mock => mock.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.DefaultStatus.Should().Be(TransactionStatus.Paid);
    }

    [Fact]
    public async Task HandleAsync_ContaNaoCartao_MantemStatusPadraoDoComando()
    {
        var account = Account.Create("Conta corrente", AccountType.Corrente, 100m, true, "user-1");
        var category = Category.Create("Assinaturas", CategoryType.Despesa, "user-1");
        var command = new CreateRecurrenceCommand(
            account.Id,
            category.Id,
            TransactionType.Debit,
            100m,
            "Plano mensal",
            10,
            TransactionStatus.Pending,
            "user-1");

        _accountRepository.Setup(mock => mock.GetByIdWithLockAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _categoryRepository.Setup(mock => mock.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.DefaultStatus.Should().Be(TransactionStatus.Pending);
    }
}

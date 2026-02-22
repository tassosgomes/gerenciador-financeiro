using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Commands.Budget;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Commands.Budget;

public class DeleteBudgetCommandHandlerTests
{
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    private readonly DeleteBudgetCommandHandler _sut;

    public DeleteBudgetCommandHandlerTests()
    {
        _auditService
            .LogAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.RollbackAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        _sut = new DeleteBudgetCommandHandler(
            _budgetRepository,
            _unitOfWork,
            _auditService,
            new BudgetDomainService(),
            NullLogger<DeleteBudgetCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithValidId_ShouldDeleteBudget()
    {
        var budget = BuildBudget(DateTime.UtcNow.AddMonths(1));
        var command = new DeleteBudgetCommand(budget.Id, "user-1");

        _budgetRepository.GetByIdWithCategoriesAsync(command.Id, Arg.Any<CancellationToken>()).Returns(budget);

        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _budgetRepository.Received(1).Remove(budget);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBudgetNotFound_ShouldThrowBudgetNotFoundException()
    {
        var command = new DeleteBudgetCommand(Guid.NewGuid(), "user-1");

        _budgetRepository.GetByIdWithCategoriesAsync(command.Id, Arg.Any<CancellationToken>()).Returns((GestorFinanceiro.Financeiro.Domain.Entity.Budget?)null);

        var action = async () => await _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<BudgetNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenPastMonth_ShouldThrowBudgetPeriodLockedException()
    {
        var budget = BuildBudget(DateTime.UtcNow.AddMonths(-1));
        var command = new DeleteBudgetCommand(budget.Id, "user-1");

        _budgetRepository.GetByIdWithCategoriesAsync(command.Id, Arg.Any<CancellationToken>()).Returns(budget);

        var action = async () => await _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<BudgetPeriodLockedException>();
    }

    [Fact]
    public async Task Handle_ShouldCallAuditService()
    {
        var budget = BuildBudget(DateTime.UtcNow.AddMonths(1));
        var command = new DeleteBudgetCommand(budget.Id, "user-1");

        _budgetRepository.GetByIdWithCategoriesAsync(command.Id, Arg.Any<CancellationToken>()).Returns(budget);

        await _sut.HandleAsync(command, CancellationToken.None);

        await _auditService.Received(1)
            .LogAsync("Budget", budget.Id, "Deleted", command.UserId, Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }

    private static GestorFinanceiro.Financeiro.Domain.Entity.Budget BuildBudget(DateTime referenceDate)
    {
        return GestorFinanceiro.Financeiro.Domain.Entity.Budget.Create(
            "Orçamento",
            20m,
            referenceDate.Year,
            referenceDate.Month,
            [Guid.NewGuid()],
            false,
            "user-1");
    }
}

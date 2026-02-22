using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Commands.Budget;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Commands.Budget;

public class UpdateBudgetCommandHandlerTests
{
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    private readonly UpdateBudgetCommandHandler _sut;

    public UpdateBudgetCommandHandlerTests()
    {
        _auditService
            .LogAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.RollbackAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        _budgetRepository
            .ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _budgetRepository
            .GetTotalPercentageForMonthAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(0m);

        _budgetRepository
            .IsCategoryUsedInMonthAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _budgetRepository
            .GetMonthlyIncomeAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(3000m);

        _budgetRepository
            .GetConsumedAmountAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(900m);

        _sut = new UpdateBudgetCommandHandler(
            _budgetRepository,
            _categoryRepository,
            _unitOfWork,
            _auditService,
            new BudgetDomainService(),
            new UpdateBudgetValidator(),
            NullLogger<UpdateBudgetCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateBudgetAndReturnResponse()
    {
        var categoryId = Guid.NewGuid();
        var existingBudget = BuildBudget(DateTime.UtcNow.AddMonths(1), [categoryId]);
        var command = new UpdateBudgetCommand(existingBudget.Id, "Orçamento Atualizado", 45m, [categoryId], true, "user-1");

        _budgetRepository.GetByIdWithCategoriesAsync(existingBudget.Id, Arg.Any<CancellationToken>()).Returns(existingBudget);
        SetupCategory(categoryId, CategoryType.Despesa);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Name.Should().Be("Orçamento Atualizado");
        response.Percentage.Should().Be(45m);
        response.IsRecurrent.Should().BeTrue();

        _budgetRepository.Received(1).Update(existingBudget);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBudgetNotFound_ShouldThrowBudgetNotFoundException()
    {
        var command = new UpdateBudgetCommand(Guid.NewGuid(), "Orçamento", 30m, [Guid.NewGuid()], false, "user-1");

        _budgetRepository.GetByIdWithCategoriesAsync(command.Id, Arg.Any<CancellationToken>()).Returns((GestorFinanceiro.Financeiro.Domain.Entity.Budget?)null);

        var action = async () => await _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<BudgetNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenPastMonth_ShouldThrowBudgetPeriodLockedException()
    {
        var categoryId = Guid.NewGuid();
        var existingBudget = BuildBudget(DateTime.UtcNow.AddMonths(-1), [categoryId]);
        var command = new UpdateBudgetCommand(existingBudget.Id, "Orçamento", 30m, [categoryId], false, "user-1");

        _budgetRepository.GetByIdWithCategoriesAsync(existingBudget.Id, Arg.Any<CancellationToken>()).Returns(existingBudget);

        var action = async () => await _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<BudgetPeriodLockedException>();
    }

    [Fact]
    public async Task Handle_WhenNewNameAlreadyExists_ShouldThrowBudgetNameAlreadyExistsException()
    {
        var categoryId = Guid.NewGuid();
        var existingBudget = BuildBudget(DateTime.UtcNow.AddMonths(1), [categoryId]);
        var command = new UpdateBudgetCommand(existingBudget.Id, "Nome Duplicado", 30m, [categoryId], false, "user-1");

        _budgetRepository.GetByIdWithCategoriesAsync(existingBudget.Id, Arg.Any<CancellationToken>()).Returns(existingBudget);
        _budgetRepository.ExistsByNameAsync("Nome Duplicado", existingBudget.Id, Arg.Any<CancellationToken>()).Returns(true);

        var action = async () => await _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<BudgetNameAlreadyExistsException>();
    }

    [Fact]
    public async Task Handle_WhenPercentageExceeds100_ShouldExcludeCurrentBudgetFromSum()
    {
        var categoryId = Guid.NewGuid();
        var existingBudget = BuildBudget(DateTime.UtcNow.AddMonths(1), [categoryId]);
        var command = new UpdateBudgetCommand(existingBudget.Id, "Orçamento", 25m, [categoryId], false, "user-1");

        _budgetRepository.GetByIdWithCategoriesAsync(existingBudget.Id, Arg.Any<CancellationToken>()).Returns(existingBudget);
        SetupCategory(categoryId, CategoryType.Despesa);

        _budgetRepository
            .GetTotalPercentageForMonthAsync(existingBudget.ReferenceYear, existingBudget.ReferenceMonth, existingBudget.Id, Arg.Any<CancellationToken>())
            .Returns(80m);

        var action = async () => await _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<BudgetPercentageExceededException>();

        await _budgetRepository.Received(1)
            .GetTotalPercentageForMonthAsync(existingBudget.ReferenceYear, existingBudget.ReferenceMonth, existingBudget.Id, Arg.Any<CancellationToken>());
    }

    private static GestorFinanceiro.Financeiro.Domain.Entity.Budget BuildBudget(DateTime referenceDate, List<Guid> categoryIds)
    {
        return GestorFinanceiro.Financeiro.Domain.Entity.Budget.Create(
            "Orçamento Atual",
            20m,
            referenceDate.Year,
            referenceDate.Month,
            categoryIds,
            false,
            "user-1");
    }

    private void SetupCategory(Guid categoryId, CategoryType type)
    {
        var category = Category.Restore(
            categoryId,
            $"Categoria-{categoryId}",
            type,
            true,
            false,
            "system",
            DateTime.UtcNow,
            null,
            null);

        _categoryRepository
            .GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(category);
    }
}

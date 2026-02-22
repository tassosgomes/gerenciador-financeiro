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

public class CreateBudgetCommandHandlerTests
{
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    private readonly CreateBudgetCommandHandler _sut;

    public CreateBudgetCommandHandlerTests()
    {
        _auditService
            .LogAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.RollbackAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        _budgetRepository
            .AddAsync(Arg.Any<GestorFinanceiro.Financeiro.Domain.Entity.Budget>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<GestorFinanceiro.Financeiro.Domain.Entity.Budget>());

        _budgetRepository
            .ExistsByNameAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>())
            .Returns(false);

        _budgetRepository
            .GetTotalPercentageForMonthAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(0m);

        _budgetRepository
            .IsCategoryUsedInMonthAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _budgetRepository
            .GetMonthlyIncomeAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(5000m);

        _budgetRepository
            .GetConsumedAmountAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(300m);

        _sut = new CreateBudgetCommandHandler(
            _budgetRepository,
            _categoryRepository,
            _unitOfWork,
            _auditService,
            new BudgetDomainService(),
            new CreateBudgetValidator(),
            NullLogger<CreateBudgetCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateBudgetAndReturnResponse()
    {
        var categoryId = Guid.NewGuid();
        var command = BuildCommand([categoryId]);
        SetupCategory(categoryId, CategoryType.Despesa);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

        response.Name.Should().Be(command.Name);
        response.Percentage.Should().Be(command.Percentage);
        response.MonthlyIncome.Should().Be(5000m);
        response.LimitAmount.Should().Be(2500m);
        response.ConsumedAmount.Should().Be(300m);

        await _budgetRepository.Received(1)
            .AddAsync(Arg.Any<GestorFinanceiro.Financeiro.Domain.Entity.Budget>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ShouldThrowBudgetNameAlreadyExistsException()
    {
        var command = BuildCommand([Guid.NewGuid()]);

        _budgetRepository
            .ExistsByNameAsync(command.Name, null, Arg.Any<CancellationToken>())
            .Returns(true);

        var action = async () => await _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<BudgetNameAlreadyExistsException>();
    }

    [Fact]
    public async Task Handle_WhenCategoryNotExpense_ShouldThrowInvalidBudgetCategoryTypeException()
    {
        var categoryId = Guid.NewGuid();
        var command = BuildCommand([categoryId]);
        SetupCategory(categoryId, CategoryType.Receita);

        var action = async () => await _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidBudgetCategoryTypeException>();
    }

    [Fact]
    public async Task Handle_WhenPercentageExceeds100_ShouldThrowBudgetPercentageExceededException()
    {
        var categoryId = Guid.NewGuid();
        var command = BuildCommand([categoryId], percentage: 25m);
        SetupCategory(categoryId, CategoryType.Despesa);

        _budgetRepository
            .GetTotalPercentageForMonthAsync(command.ReferenceYear, command.ReferenceMonth, null, Arg.Any<CancellationToken>())
            .Returns(80m);

        var action = async () => await _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<BudgetPercentageExceededException>();
    }

    [Fact]
    public async Task Handle_WhenCategoryAlreadyBudgeted_ShouldThrowCategoryAlreadyBudgetedException()
    {
        var categoryId = Guid.NewGuid();
        var command = BuildCommand([categoryId]);
        SetupCategory(categoryId, CategoryType.Despesa);

        _budgetRepository
            .IsCategoryUsedInMonthAsync(categoryId, command.ReferenceYear, command.ReferenceMonth, null, Arg.Any<CancellationToken>())
            .Returns(true);

        var action = async () => await _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<CategoryAlreadyBudgetedException>();
    }

    [Fact]
    public async Task Handle_WhenPastMonth_ShouldThrowBudgetPeriodLockedException()
    {
        var pastDate = DateTime.UtcNow.AddMonths(-1);
        var categoryId = Guid.NewGuid();
        var command = BuildCommand([categoryId], referenceYear: pastDate.Year, referenceMonth: pastDate.Month);
        SetupCategory(categoryId, CategoryType.Despesa);

        var action = async () => await _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<BudgetPeriodLockedException>();
    }

    [Fact]
    public async Task Handle_ShouldCallAuditService()
    {
        var categoryId = Guid.NewGuid();
        var command = BuildCommand([categoryId]);
        SetupCategory(categoryId, CategoryType.Despesa);

        await _sut.HandleAsync(command, CancellationToken.None);

        await _auditService.Received(1)
            .LogAsync("Budget", Arg.Any<Guid>(), "Created", command.UserId, null, Arg.Any<CancellationToken>());
    }

    private static CreateBudgetCommand BuildCommand(
        List<Guid> categoryIds,
        decimal percentage = 50m,
        int? referenceYear = null,
        int? referenceMonth = null)
    {
        var baseDate = DateTime.UtcNow;

        return new CreateBudgetCommand(
            "Orçamento Lazer",
            percentage,
            referenceYear ?? baseDate.Year,
            referenceMonth ?? baseDate.Month,
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

using System.Reflection;
using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.StartupTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GestorFinanceiro.Financeiro.UnitTests.Infra.StartupTasks;

public class BudgetRecurrenceWorkerTests
{
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TestLogger<BudgetRecurrenceWorker> _logger = new();

    [Fact]
    public async Task ProcessRecurrence_WithRecurrentBudgets_ShouldCreateForCurrentMonth()
    {
        var now = DateTime.UtcNow;
        var previous = now.AddMonths(-1);
        var categoryId = Guid.NewGuid();
        var recurrentBudget = BuildRecurrentBudget("Lazer", previous.Year, previous.Month, [categoryId]);

        _budgetRepository
            .GetRecurrentBudgetsForMonthAsync(previous.Year, previous.Month, Arg.Any<CancellationToken>())
            .Returns([recurrentBudget]);
        _budgetRepository
            .ExistsByNameAsync(recurrentBudget.Name, null, Arg.Any<CancellationToken>())
            .Returns(false);
        _budgetRepository
            .GetTotalPercentageForMonthAsync(now.Year, now.Month, null, Arg.Any<CancellationToken>())
            .Returns(40m);
        _categoryRepository
            .GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(BuildCategory(categoryId, isActive: true));
        _budgetRepository
            .AddAsync(Arg.Any<Budget>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Budget>());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var sut = CreateSut();

        await InvokeProcessRecurrenceAsync(sut);

        await _budgetRepository.Received(1).AddAsync(
            Arg.Is<Budget>(budget =>
                budget.Name == recurrentBudget.Name
                && budget.ReferenceYear == now.Year
                && budget.ReferenceMonth == now.Month
                && budget.IsRecurrent
                && budget.CategoryIds.SequenceEqual(new[] { categoryId })),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRecurrence_WhenBudgetAlreadyExists_ShouldSkip()
    {
        var previous = DateTime.UtcNow.AddMonths(-1);
        var categoryId = Guid.NewGuid();
        var recurrentBudget = BuildRecurrentBudget("Moradia", previous.Year, previous.Month, [categoryId]);

        _budgetRepository
            .GetRecurrentBudgetsForMonthAsync(previous.Year, previous.Month, Arg.Any<CancellationToken>())
            .Returns([recurrentBudget]);
        _budgetRepository
            .ExistsByNameAsync(recurrentBudget.Name, null, Arg.Any<CancellationToken>())
            .Returns(true);
        _budgetRepository
            .GetTotalPercentageForMonthAsync(Arg.Any<int>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(30m);

        var sut = CreateSut();

        await InvokeProcessRecurrenceAsync(sut);

        await _budgetRepository.DidNotReceive().AddAsync(Arg.Any<Budget>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRecurrence_WhenCategoryInactive_ShouldExcludeFromCopy()
    {
        var now = DateTime.UtcNow;
        var previous = now.AddMonths(-1);
        var activeCategoryId = Guid.NewGuid();
        var inactiveCategoryId = Guid.NewGuid();
        var recurrentBudget = BuildRecurrentBudget("Saúde", previous.Year, previous.Month, [activeCategoryId, inactiveCategoryId]);

        _budgetRepository
            .GetRecurrentBudgetsForMonthAsync(previous.Year, previous.Month, Arg.Any<CancellationToken>())
            .Returns([recurrentBudget]);
        _budgetRepository
            .ExistsByNameAsync(recurrentBudget.Name, null, Arg.Any<CancellationToken>())
            .Returns(false);
        _budgetRepository
            .GetTotalPercentageForMonthAsync(now.Year, now.Month, null, Arg.Any<CancellationToken>())
            .Returns(45m);
        _categoryRepository
            .GetByIdAsync(activeCategoryId, Arg.Any<CancellationToken>())
            .Returns(BuildCategory(activeCategoryId, isActive: true));
        _categoryRepository
            .GetByIdAsync(inactiveCategoryId, Arg.Any<CancellationToken>())
            .Returns(BuildCategory(inactiveCategoryId, isActive: false));
        _budgetRepository
            .AddAsync(Arg.Any<Budget>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Budget>());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var sut = CreateSut();

        await InvokeProcessRecurrenceAsync(sut);

        await _budgetRepository.Received(1).AddAsync(
            Arg.Is<Budget>(budget => budget.CategoryIds.SequenceEqual(new[] { activeCategoryId })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRecurrence_WhenAllCategoriesInactive_ShouldSkipBudget()
    {
        var previous = DateTime.UtcNow.AddMonths(-1);
        var categoryId = Guid.NewGuid();
        var recurrentBudget = BuildRecurrentBudget("Transporte", previous.Year, previous.Month, [categoryId]);

        _budgetRepository
            .GetRecurrentBudgetsForMonthAsync(previous.Year, previous.Month, Arg.Any<CancellationToken>())
            .Returns([recurrentBudget]);
        _budgetRepository
            .ExistsByNameAsync(recurrentBudget.Name, null, Arg.Any<CancellationToken>())
            .Returns(false);
        _budgetRepository
            .GetTotalPercentageForMonthAsync(Arg.Any<int>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(25m);
        _categoryRepository
            .GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(BuildCategory(categoryId, isActive: false));

        var sut = CreateSut();

        await InvokeProcessRecurrenceAsync(sut);

        await _budgetRepository.DidNotReceive().AddAsync(Arg.Any<Budget>(), Arg.Any<CancellationToken>());
        _logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("ignorado: todas as categorias estão inativas"));
    }

    [Fact]
    public async Task ProcessRecurrence_WhenPercentageExceeds100_ShouldCreateAndLogWarning()
    {
        var previous = DateTime.UtcNow.AddMonths(-1);
        var categoryId = Guid.NewGuid();
        var recurrentBudget = BuildRecurrentBudget("Educação", previous.Year, previous.Month, [categoryId]);

        _budgetRepository
            .GetRecurrentBudgetsForMonthAsync(previous.Year, previous.Month, Arg.Any<CancellationToken>())
            .Returns([recurrentBudget]);
        _budgetRepository
            .ExistsByNameAsync(recurrentBudget.Name, null, Arg.Any<CancellationToken>())
            .Returns(false);
        _budgetRepository
            .GetTotalPercentageForMonthAsync(Arg.Any<int>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(120m);
        _categoryRepository
            .GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(BuildCategory(categoryId, isActive: true));
        _budgetRepository
            .AddAsync(Arg.Any<Budget>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Budget>());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var sut = CreateSut();

        await InvokeProcessRecurrenceAsync(sut);

        await _budgetRepository.Received(1).AddAsync(Arg.Any<Budget>(), Arg.Any<CancellationToken>());
        _logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("excede 100%"));
    }

    [Fact]
    public async Task ProcessRecurrence_WithNoBudgets_ShouldDoNothing()
    {
        var previous = DateTime.UtcNow.AddMonths(-1);

        _budgetRepository
            .GetRecurrentBudgetsForMonthAsync(previous.Year, previous.Month, Arg.Any<CancellationToken>())
            .Returns([]);
        _budgetRepository
            .GetTotalPercentageForMonthAsync(Arg.Any<int>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(0m);

        var sut = CreateSut();

        await InvokeProcessRecurrenceAsync(sut);

        await _budgetRepository.DidNotReceive().AddAsync(Arg.Any<Budget>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRecurrence_WhenExceptionThrown_ShouldLogErrorAndContinue()
    {
        var previous = DateTime.UtcNow.AddMonths(-1);
        var categoryId = Guid.NewGuid();
        var firstBudget = BuildRecurrentBudget("Primeiro", previous.Year, previous.Month, [categoryId]);
        var secondBudget = BuildRecurrentBudget("Segundo", previous.Year, previous.Month, [categoryId]);

        _budgetRepository
            .GetRecurrentBudgetsForMonthAsync(previous.Year, previous.Month, Arg.Any<CancellationToken>())
            .Returns([firstBudget, secondBudget]);
        _budgetRepository
            .ExistsByNameAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>())
            .Returns(false);
        _budgetRepository
            .GetTotalPercentageForMonthAsync(Arg.Any<int>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(60m);
        _categoryRepository
            .GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(BuildCategory(categoryId, isActive: true));

        var addCallCount = 0;
        _budgetRepository
            .AddAsync(Arg.Any<Budget>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                addCallCount++;
                if (addCallCount == 1)
                {
                    throw new InvalidOperationException("falha ao persistir");
                }

                return _.Arg<Budget>();
            });

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var sut = CreateSut();

        await InvokeProcessRecurrenceAsync(sut);

        await _budgetRepository.Received(2).AddAsync(Arg.Any<Budget>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Error
            && entry.Message.Contains("Erro ao replicar orçamento recorrente"));
    }

    private BudgetRecurrenceWorker CreateSut()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _budgetRepository);
        services.AddScoped(_ => _categoryRepository);
        services.AddScoped(_ => _unitOfWork);

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        return new BudgetRecurrenceWorker(scopeFactory, _logger);
    }

    private static async Task InvokeProcessRecurrenceAsync(BudgetRecurrenceWorker worker)
    {
        var method = typeof(BudgetRecurrenceWorker).GetMethod("ProcessRecurrenceAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var task = method!.Invoke(worker, [CancellationToken.None]) as Task;
        task.Should().NotBeNull();

        await task!;
    }

    private static Budget BuildRecurrentBudget(string name, int year, int month, IReadOnlyList<Guid> categoryIds)
    {
        return Budget.Create(
            name,
            25m,
            year,
            month,
            categoryIds,
            isRecurrent: true,
            userId: "user-1");
    }

    private static Category BuildCategory(Guid categoryId, bool isActive)
    {
        return Category.Restore(
            categoryId,
            $"Categoria-{categoryId}",
            CategoryType.Despesa,
            isActive,
            false,
            "system",
            DateTime.UtcNow,
            null,
            null);
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return EmptyDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }

        public sealed record LogEntry(LogLevel Level, string Message);

        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
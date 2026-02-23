using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Commands.Category;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GestorFinanceiro.Financeiro.UnitTests.Application.Commands.CategoryHandlers;

public class DeleteCategoryCommandHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TestLogger<DeleteCategoryCommandHandler> _logger = new();

    private readonly DeleteCategoryCommandHandler _sut;

    public DeleteCategoryCommandHandlerTests()
    {
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.RollbackAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        _sut = new DeleteCategoryCommandHandler(
            _categoryRepository,
            _budgetRepository,
            _unitOfWork,
            _logger);
    }

    [Fact]
    public async Task Handle_WhenCategoryLinkedToBudget_ShouldDesassociate()
    {
        var categoryId = Guid.NewGuid();
        var command = new DeleteCategoryCommand(categoryId, null, "user-1");
        var category = GestorFinanceiro.Financeiro.Domain.Entity.Category.Create("Lazer", CategoryType.Despesa, "user-1");
        var affectedBudget = BuildBudget("Lazer", categoryId, Guid.NewGuid());

        _categoryRepository.GetByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns(category);
        _budgetRepository.GetBudgetsByCategoryIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<GestorFinanceiro.Financeiro.Domain.Entity.Budget>>([affectedBudget]));
        _categoryRepository.HasLinkedDataAsync(categoryId, Arg.Any<CancellationToken>()).Returns(false);
        _budgetRepository.GetCategoryCountAsync(affectedBudget.Id, Arg.Any<CancellationToken>()).Returns(1);

        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        await _budgetRepository.Received(1).RemoveCategoryFromBudgetsAsync(categoryId, Arg.Any<CancellationToken>());
        _categoryRepository.Received(1).Remove(category);
        _logger.Entries.Should().NotContain(entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Handle_WhenCategoryLinkedToBudget_AndBudgetBecomesEmpty_ShouldLogWarning()
    {
        var categoryId = Guid.NewGuid();
        var command = new DeleteCategoryCommand(categoryId, null, "user-1");
        var category = GestorFinanceiro.Financeiro.Domain.Entity.Category.Create("Moradia", CategoryType.Despesa, "user-1");
        var affectedBudget = BuildBudget("Moradia", categoryId);

        _categoryRepository.GetByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns(category);
        _budgetRepository.GetBudgetsByCategoryIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<GestorFinanceiro.Financeiro.Domain.Entity.Budget>>([affectedBudget]));
        _categoryRepository.HasLinkedDataAsync(categoryId, Arg.Any<CancellationToken>()).Returns(false);
        _budgetRepository.GetCategoryCountAsync(affectedBudget.Id, Arg.Any<CancellationToken>()).Returns(0);

        await _sut.HandleAsync(command, CancellationToken.None);

        _logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("Orçamento 'Moradia' ficou sem categorias após remoção da categoria")
            && entry.Message.Contains(categoryId.ToString()));
    }

    [Fact]
    public async Task Handle_WhenCategoryNotLinkedToBudget_ShouldNotCallRemove()
    {
        var categoryId = Guid.NewGuid();
        var command = new DeleteCategoryCommand(categoryId, null, "user-1");
        var category = GestorFinanceiro.Financeiro.Domain.Entity.Category.Create("Assinaturas", CategoryType.Despesa, "user-1");

        _categoryRepository.GetByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns(category);
        _budgetRepository.GetBudgetsByCategoryIdAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<GestorFinanceiro.Financeiro.Domain.Entity.Budget>>([]));
        _categoryRepository.HasLinkedDataAsync(categoryId, Arg.Any<CancellationToken>()).Returns(false);

        await _sut.HandleAsync(command, CancellationToken.None);

        await _budgetRepository.DidNotReceive().RemoveCategoryFromBudgetsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static GestorFinanceiro.Financeiro.Domain.Entity.Budget BuildBudget(string name, params Guid[] categoryIds)
    {
        var now = DateTime.UtcNow;

        return GestorFinanceiro.Financeiro.Domain.Entity.Budget.Create(
            name,
            20m,
            now.Year,
            now.Month,
            categoryIds,
            false,
            "user-1");
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

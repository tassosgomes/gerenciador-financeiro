using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Commands.Category;

public class DeleteCategoryCommandHandler : ICommandHandler<DeleteCategoryCommand, Unit>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCategoryCommandHandler> _logger;

    public DeleteCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IBudgetRepository budgetRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCategoryCommandHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _budgetRepository = budgetRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting category with ID: {CategoryId}", command.CategoryId);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
            if (category is null)
            {
                throw new CategoryNotFoundException(command.CategoryId);
            }

            if (category.IsSystem)
            {
                throw new SystemCategoryCannotBeChangedException(command.CategoryId);
            }

            var affectedBudgets = await _budgetRepository.GetBudgetsByCategoryIdAsync(command.CategoryId, cancellationToken);

            if (command.MigrateToCategoryId.HasValue)
            {
                if (command.MigrateToCategoryId.Value == command.CategoryId)
                {
                    throw new InvalidCategoryMigrationTargetException("Target category must be different from source category.");
                }

                var targetCategory = await _categoryRepository.GetByIdAsync(command.MigrateToCategoryId.Value, cancellationToken);
                if (targetCategory is null)
                {
                    throw new CategoryNotFoundException(command.MigrateToCategoryId.Value);
                }

                if (targetCategory.Type != category.Type)
                {
                    throw new InvalidCategoryMigrationTargetException("Target category must have the same type (Receita/Despesa).");
                }

                await _categoryRepository.MigrateLinkedDataAsync(
                    command.CategoryId,
                    command.MigrateToCategoryId.Value,
                    command.UserId,
                    cancellationToken);
            }
            else
            {
                if (affectedBudgets.Count > 0)
                {
                    await _budgetRepository.RemoveCategoryFromBudgetsAsync(command.CategoryId, cancellationToken);
                }

                var hasLinkedData = await _categoryRepository.HasLinkedDataAsync(command.CategoryId, cancellationToken);
                if (hasLinkedData)
                {
                    throw new CategoryMigrationRequiredException(command.CategoryId);
                }
            }

            await LogBudgetsWithoutCategoriesWarningAsync(affectedBudgets, command.CategoryId, cancellationToken);

            _categoryRepository.Remove(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Category deleted successfully with ID: {CategoryId}", command.CategoryId);
            return Unit.Value;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task LogBudgetsWithoutCategoriesWarningAsync(
        IReadOnlyList<GestorFinanceiro.Financeiro.Domain.Entity.Budget> affectedBudgets,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        foreach (var budget in affectedBudgets)
        {
            var categoryCount = await _budgetRepository.GetCategoryCountAsync(budget.Id, cancellationToken);
            if (categoryCount == 0)
            {
                _logger.LogWarning(
                    "Orçamento '{BudgetName}' ficou sem categorias após remoção da categoria {CategoryId}",
                    budget.Name,
                    categoryId);
            }
        }
    }
}

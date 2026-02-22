using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Microsoft.Extensions.Logging;
using CategoryEntity = GestorFinanceiro.Financeiro.Domain.Entity.Category;

namespace GestorFinanceiro.Financeiro.Application.Commands.Budget;

public class UpdateBudgetCommandHandler : ICommandHandler<UpdateBudgetCommand, BudgetResponse>
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly BudgetDomainService _budgetDomainService;
    private readonly UpdateBudgetValidator _validator;
    private readonly ILogger<UpdateBudgetCommandHandler> _logger;

    public UpdateBudgetCommandHandler(
        IBudgetRepository budgetRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        BudgetDomainService budgetDomainService,
        UpdateBudgetValidator validator,
        ILogger<UpdateBudgetCommandHandler> logger)
    {
        _budgetRepository = budgetRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _budgetDomainService = budgetDomainService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<BudgetResponse> HandleAsync(UpdateBudgetCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating budget: {BudgetId}", command.Id);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var budget = await _budgetRepository.GetByIdWithCategoriesAsync(command.Id, cancellationToken);
        if (budget is null)
        {
            throw new BudgetNotFoundException(command.Id);
        }

        _budgetDomainService.ValidateReferenceMonth(budget.ReferenceYear, budget.ReferenceMonth);

        var normalizedName = command.Name.Trim();
        var nameAlreadyExists = await _budgetRepository.ExistsByNameAsync(normalizedName, budget.Id, cancellationToken);
        if (nameAlreadyExists)
        {
            throw new BudgetNameAlreadyExistsException(normalizedName);
        }

        var categories = await GetAndValidateCategoriesAsync(command.CategoryIds, cancellationToken);

        await _budgetDomainService.ValidatePercentageCapAsync(
            _budgetRepository,
            budget.ReferenceYear,
            budget.ReferenceMonth,
            command.Percentage,
            budget.Id,
            cancellationToken);

        await _budgetDomainService.ValidateCategoryUniquenessAsync(
            _budgetRepository,
            command.CategoryIds,
            budget.ReferenceYear,
            budget.ReferenceMonth,
            budget.Id,
            cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var previousData = new
            {
                budget.Name,
                budget.Percentage,
                CategoryIds = budget.CategoryIds.ToList(),
                budget.IsRecurrent
            };

            budget.Update(
                normalizedName,
                command.Percentage,
                command.CategoryIds,
                command.IsRecurrent,
                command.UserId);

            _budgetRepository.Update(budget);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Budget", budget.Id, "Updated", command.UserId, previousData, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Budget updated successfully: {BudgetId}", budget.Id);

            return await BuildResponseAsync(budget, categories, cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<IReadOnlyList<CategoryEntity>> GetAndValidateCategoriesAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken)
    {
        var categories = new List<CategoryEntity>();

        foreach (var categoryId in categoryIds.Distinct())
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is null)
            {
                throw new CategoryNotFoundException(categoryId);
            }

            if (category.Type != CategoryType.Despesa)
            {
                throw new InvalidBudgetCategoryTypeException(categoryId);
            }

            categories.Add(category);
        }

        return categories;
    }

    private async Task<BudgetResponse> BuildResponseAsync(
        Domain.Entity.Budget budget,
        IReadOnlyList<CategoryEntity> categories,
        CancellationToken cancellationToken)
    {
        var monthlyIncome = await _budgetRepository.GetMonthlyIncomeAsync(
            budget.ReferenceYear,
            budget.ReferenceMonth,
            cancellationToken);

        var consumedAmount = await _budgetRepository.GetConsumedAmountAsync(
            budget.CategoryIds,
            budget.ReferenceYear,
            budget.ReferenceMonth,
            cancellationToken);

        var limitAmount = budget.CalculateLimit(monthlyIncome);
        var remainingAmount = limitAmount - consumedAmount;
        var consumedPercentage = limitAmount <= 0m ? 0m : (consumedAmount / limitAmount) * 100m;

        var categoryDtos = categories
            .OrderBy(category => category.Name)
            .Select(category => new BudgetCategoryDto(category.Id, category.Name))
            .ToList();

        return new BudgetResponse(
            budget.Id,
            budget.Name,
            budget.Percentage,
            budget.ReferenceYear,
            budget.ReferenceMonth,
            budget.IsRecurrent,
            monthlyIncome,
            limitAmount,
            consumedAmount,
            remainingAmount,
            consumedPercentage,
            categoryDtos,
            budget.CreatedAt,
            budget.UpdatedAt);
    }
}

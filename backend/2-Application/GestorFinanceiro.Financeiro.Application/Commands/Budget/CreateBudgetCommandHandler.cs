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

public class CreateBudgetCommandHandler : ICommandHandler<CreateBudgetCommand, BudgetResponse>
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly BudgetDomainService _budgetDomainService;
    private readonly CreateBudgetValidator _validator;
    private readonly ILogger<CreateBudgetCommandHandler> _logger;

    public CreateBudgetCommandHandler(
        IBudgetRepository budgetRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        BudgetDomainService budgetDomainService,
        CreateBudgetValidator validator,
        ILogger<CreateBudgetCommandHandler> logger)
    {
        _budgetRepository = budgetRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _budgetDomainService = budgetDomainService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<BudgetResponse> HandleAsync(CreateBudgetCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating budget with name: {Name}", command.Name);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var normalizedName = command.Name.Trim();

        var nameAlreadyExists = await _budgetRepository.ExistsByNameAsync(normalizedName, null, cancellationToken);
        if (nameAlreadyExists)
        {
            throw new BudgetNameAlreadyExistsException(normalizedName);
        }

        var categories = await GetAndValidateCategoriesAsync(command.CategoryIds, cancellationToken);

        _budgetDomainService.ValidateReferenceMonth(command.ReferenceYear, command.ReferenceMonth);

        await _budgetDomainService.ValidatePercentageCapAsync(
            _budgetRepository,
            command.ReferenceYear,
            command.ReferenceMonth,
            command.Percentage,
            null,
            cancellationToken);

        await _budgetDomainService.ValidateCategoryUniquenessAsync(
            _budgetRepository,
            command.CategoryIds,
            command.ReferenceYear,
            command.ReferenceMonth,
            null,
            cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var budget = Domain.Entity.Budget.Create(
                normalizedName,
                command.Percentage,
                command.ReferenceYear,
                command.ReferenceMonth,
                command.CategoryIds,
                command.IsRecurrent,
                command.UserId);

            await _budgetRepository.AddAsync(budget, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Budget", budget.Id, "Created", command.UserId, null, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Budget created successfully with ID: {BudgetId}", budget.Id);

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

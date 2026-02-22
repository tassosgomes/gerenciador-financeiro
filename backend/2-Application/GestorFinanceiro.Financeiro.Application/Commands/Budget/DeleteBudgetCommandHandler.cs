using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Commands.Budget;

public class DeleteBudgetCommandHandler : ICommandHandler<DeleteBudgetCommand, Unit>
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly BudgetDomainService _budgetDomainService;
    private readonly ILogger<DeleteBudgetCommandHandler> _logger;

    public DeleteBudgetCommandHandler(
        IBudgetRepository budgetRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        BudgetDomainService budgetDomainService,
        ILogger<DeleteBudgetCommandHandler> logger)
    {
        _budgetRepository = budgetRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _budgetDomainService = budgetDomainService;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(DeleteBudgetCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting budget: {BudgetId}", command.Id);

        var budget = await _budgetRepository.GetByIdWithCategoriesAsync(command.Id, cancellationToken);
        if (budget is null)
        {
            throw new BudgetNotFoundException(command.Id);
        }

        _budgetDomainService.ValidateReferenceMonth(budget.ReferenceYear, budget.ReferenceMonth);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var previousData = new
            {
                budget.Name,
                budget.Percentage,
                budget.ReferenceYear,
                budget.ReferenceMonth,
                CategoryIds = budget.CategoryIds.ToList(),
                budget.IsRecurrent
            };

            _budgetRepository.Remove(budget);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Budget", budget.Id, "Deleted", command.UserId, previousData, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Budget deleted successfully: {BudgetId}", command.Id);

            return Unit.Value;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

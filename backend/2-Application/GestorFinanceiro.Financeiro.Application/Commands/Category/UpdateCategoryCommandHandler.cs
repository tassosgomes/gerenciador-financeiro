using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Commands.Category;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GestorFinanceiro.Financeiro.Application.Commands.Category;

public class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand, CategoryResponse>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateCategoryCommandHandler> _logger;

    public UpdateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IOperationLogRepository operationLogRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateCategoryCommandHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _operationLogRepository = operationLogRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CategoryResponse> HandleAsync(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating category with ID: {CategoryId}", command.CategoryId);

        var validator = new UpdateCategoryCommandValidator();
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        // Check idempotência
        if (!string.IsNullOrEmpty(command.OperationId))
        {
            var existingLog = await _operationLogRepository.ExistsByOperationIdAsync(
                command.OperationId, cancellationToken);
            if (existingLog)
                throw new DuplicateOperationException(command.OperationId);
        }

        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Load category
            var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
            if (category == null)
                throw new CategoryNotFoundException(command.CategoryId);

            var previousData = category.Adapt<CategoryResponse>();

            // Update name
            category.UpdateName(command.Name, command.UserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Category", category.Id, "Updated", command.UserId, previousData, cancellationToken);

            // Log operation
            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "UpdateCategory",
                    ResultEntityId = category.Id,
                    ResultPayload = JsonSerializer.Serialize(category.Adapt<CategoryResponse>())
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Commit
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Category updated successfully with ID: {Id}", category.Id);

            return category.Adapt<CategoryResponse>();
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

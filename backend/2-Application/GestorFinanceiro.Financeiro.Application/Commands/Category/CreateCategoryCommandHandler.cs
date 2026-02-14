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

public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, CategoryResponse>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IOperationLogRepository operationLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateCategoryCommandHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _operationLogRepository = operationLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CategoryResponse> HandleAsync(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating category with name: {Name}", command.Name);

        var validator = new CreateCategoryValidator();
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        // Check idempotência
        if (!string.IsNullOrEmpty(command.OperationId))
        {
            var existingLog = await _operationLogRepository.ExistsByOperationIdAsync(
                command.OperationId, cancellationToken);
            if (existingLog)
                throw new DuplicateOperationException(command.OperationId);
        }

        // Check if name exists
        var nameExists = await _categoryRepository.ExistsByNameAndTypeAsync(command.Name, command.Type, cancellationToken);
        if (nameExists)
            throw new CategoryNameAlreadyExistsException(command.Name, command.Type);

        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Create category
            var category = GestorFinanceiro.Financeiro.Domain.Entity.Category.Create(command.Name, command.Type, command.UserId);

            await _categoryRepository.AddAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Log operation
            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "CreateCategory",
                    ResultEntityId = category.Id,
                    ResultPayload = JsonSerializer.Serialize(category.Adapt<CategoryResponse>())
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Commit
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Category created successfully with ID: {Id}", category.Id);

            return category.Adapt<CategoryResponse>();
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

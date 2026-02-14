using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GestorFinanceiro.Financeiro.Application.Commands.Recurrence;

public class CreateRecurrenceCommandHandler : ICommandHandler<CreateRecurrenceCommand, RecurrenceTemplateResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IRecurrenceTemplateRepository _recurrenceTemplateRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateRecurrenceCommandHandler> _logger;

    public CreateRecurrenceCommandHandler(
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository,
        IRecurrenceTemplateRepository recurrenceTemplateRepository,
        IOperationLogRepository operationLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateRecurrenceCommandHandler> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _recurrenceTemplateRepository = recurrenceTemplateRepository ?? throw new ArgumentNullException(nameof(recurrenceTemplateRepository));
        _operationLogRepository = operationLogRepository ?? throw new ArgumentNullException(nameof(operationLogRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RecurrenceTemplateResponse> HandleAsync(CreateRecurrenceCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating recurrence template for account: {AccountId}", command.AccountId);

        if (!string.IsNullOrEmpty(command.OperationId))
        {
            var existingLog = await _operationLogRepository.ExistsByOperationIdAsync(command.OperationId, cancellationToken);
            if (existingLog)
            {
                throw new DuplicateOperationException(command.OperationId);
            }
        }

        var validator = new CreateRecurrenceValidator();
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var account = await _accountRepository.GetByIdWithLockAsync(command.AccountId, cancellationToken);
            if (account == null)
            {
                throw new AccountNotFoundException(command.AccountId);
            }

            var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
            if (category == null)
            {
                throw new CategoryNotFoundException(command.CategoryId);
            }

            var recurrenceTemplate = RecurrenceTemplate.Create(
                command.AccountId,
                command.CategoryId,
                command.Type,
                command.Amount,
                command.Description,
                command.DayOfMonth,
                command.DefaultStatus,
                command.UserId);

            await _recurrenceTemplateRepository.AddAsync(recurrenceTemplate, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "CreateRecurrence",
                    ResultEntityId = recurrenceTemplate.Id,
                    ResultPayload = JsonSerializer.Serialize(recurrenceTemplate)
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            var response = recurrenceTemplate.Adapt<RecurrenceTemplateResponse>();

            _logger.LogInformation("Recurrence template created successfully with ID: {Id}", recurrenceTemplate.Id);

            return response;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

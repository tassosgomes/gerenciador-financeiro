using GestorFinanceiro.Financeiro.Application.Commands.Recurrence;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GestorFinanceiro.Financeiro.Application.Commands.Recurrence;

public class DeactivateRecurrenceCommandHandler : ICommandHandler<DeactivateRecurrenceCommand, Unit>
{
    private readonly IRecurrenceTemplateRepository _recurrenceTemplateRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeactivateRecurrenceCommandHandler> _logger;

    public DeactivateRecurrenceCommandHandler(
        IRecurrenceTemplateRepository recurrenceTemplateRepository,
        IOperationLogRepository operationLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeactivateRecurrenceCommandHandler> logger)
    {
        _recurrenceTemplateRepository = recurrenceTemplateRepository ?? throw new ArgumentNullException(nameof(recurrenceTemplateRepository));
        _operationLogRepository = operationLogRepository ?? throw new ArgumentNullException(nameof(operationLogRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Unit> HandleAsync(
        DeactivateRecurrenceCommand command, CancellationToken cancellationToken)
    {
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
            // Load recurrence template
            var template = await _recurrenceTemplateRepository.GetByIdAsync(command.RecurrenceId, cancellationToken);
            if (template == null)
                throw new RecurrenceTemplateNotFoundException(command.RecurrenceId); // Need to create this exception

            // Deactivate
            template.Deactivate(command.UserId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Log operation
            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "DeactivateRecurrence",
                    ResultEntityId = command.RecurrenceId,
                    ResultPayload = JsonSerializer.Serialize(new { Deactivated = true })
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Commit
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Recurrence template {Id} deactivated successfully", command.RecurrenceId);

            return Unit.Value;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

using GestorFinanceiro.Financeiro.Application.Commands.Recurrence;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GestorFinanceiro.Financeiro.Application.Commands.Recurrence;

public class GenerateRecurrenceCommandHandler : ICommandHandler<GenerateRecurrenceCommand, Unit>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IRecurrenceTemplateRepository _recurrenceTemplateRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RecurrenceDomainService _recurrenceDomainService;
    private readonly ILogger<GenerateRecurrenceCommandHandler> _logger;

    public GenerateRecurrenceCommandHandler(
        IAccountRepository accountRepository,
        IRecurrenceTemplateRepository recurrenceTemplateRepository,
        ITransactionRepository transactionRepository,
        IOperationLogRepository operationLogRepository,
        IUnitOfWork unitOfWork,
        RecurrenceDomainService recurrenceDomainService,
        ILogger<GenerateRecurrenceCommandHandler> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _recurrenceTemplateRepository = recurrenceTemplateRepository ?? throw new ArgumentNullException(nameof(recurrenceTemplateRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _operationLogRepository = operationLogRepository ?? throw new ArgumentNullException(nameof(operationLogRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _recurrenceDomainService = recurrenceDomainService ?? throw new ArgumentNullException(nameof(recurrenceDomainService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Unit> HandleAsync(
        GenerateRecurrenceCommand command, CancellationToken cancellationToken)
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
                throw new RecurrenceTemplateNotFoundException(command.RecurrenceId);

            // Load account with lock
            var account = await _accountRepository.GetByIdWithLockAsync(template.AccountId, cancellationToken);
            if (account == null)
                throw new AccountNotFoundException(template.AccountId);

            // Generate occurrence
            var transaction = _recurrenceDomainService.GenerateNextOccurrence(
                template,
                account,
                command.ReferenceDate,
                command.UserId);

            if (transaction != null)
            {
                await _transactionRepository.AddAsync(transaction, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Recurrence transaction generated for template {TemplateId}", command.RecurrenceId);
            }
            else
            {
                _logger.LogInformation("No recurrence transaction generated for template {TemplateId}", command.RecurrenceId);
            }

            // Log operation
            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "GenerateRecurrence",
                    ResultEntityId = command.RecurrenceId,
                    ResultPayload = JsonSerializer.Serialize(new { Generated = transaction != null })
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Commit
            await _unitOfWork.CommitAsync(cancellationToken);

            return Unit.Value;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

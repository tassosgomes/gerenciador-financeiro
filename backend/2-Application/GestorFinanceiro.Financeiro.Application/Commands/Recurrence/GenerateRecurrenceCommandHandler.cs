using GestorFinanceiro.Financeiro.Application.Commands.Recurrence;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
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
    private readonly TransactionDomainService _transactionDomainService;
    private readonly ILogger<GenerateRecurrenceCommandHandler> _logger;

    public GenerateRecurrenceCommandHandler(
        IAccountRepository accountRepository,
        IRecurrenceTemplateRepository recurrenceTemplateRepository,
        ITransactionRepository transactionRepository,
        IOperationLogRepository operationLogRepository,
        IUnitOfWork unitOfWork,
        RecurrenceDomainService recurrenceDomainService,
        TransactionDomainService transactionDomainService,
        ILogger<GenerateRecurrenceCommandHandler> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _recurrenceTemplateRepository = recurrenceTemplateRepository ?? throw new ArgumentNullException(nameof(recurrenceTemplateRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _operationLogRepository = operationLogRepository ?? throw new ArgumentNullException(nameof(operationLogRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _recurrenceDomainService = recurrenceDomainService ?? throw new ArgumentNullException(nameof(recurrenceDomainService));
        _transactionDomainService = transactionDomainService ?? throw new ArgumentNullException(nameof(transactionDomainService));
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

            var referenceDate = command.ReferenceDate.Date;
            var recurrenceTransactions = (await _transactionRepository
                    .GetByRecurrenceTemplateIdAsync(command.RecurrenceId, cancellationToken))
                .ToList();

            var paidNow = 0;
            foreach (var transaction in recurrenceTransactions.Where(transaction =>
                         transaction.Status == TransactionStatus.Pending
                         && ResolveDueDate(transaction) <= referenceDate))
            {
                _transactionDomainService.MarkTransactionAsPaid(account, transaction, command.UserId);
                paidNow++;
            }

            var generated = 0;
            for (var monthOffset = 1; monthOffset <= 12; monthOffset++)
            {
                var targetMonth = referenceDate.AddMonths(monthOffset);
                var competenceDate = BuildCompetenceDate(template, targetMonth);

                var alreadyExists = recurrenceTransactions.Any(transaction => transaction.CompetenceDate.Date == competenceDate.Date);
                if (alreadyExists)
                {
                    continue;
                }

                var futureTransaction = _transactionDomainService.CreateTransaction(
                    account,
                    template.CategoryId,
                    template.Type,
                    template.Amount,
                    template.Description,
                    competenceDate,
                    competenceDate,
                    TransactionStatus.Pending,
                    command.UserId);

                futureTransaction.SetRecurrenceInfo(template.Id);
                await _transactionRepository.AddAsync(futureTransaction, cancellationToken);
                recurrenceTransactions.Add(futureTransaction);
                generated++;
            }

            var maxCompetenceDate = recurrenceTransactions
                .Select(transaction => transaction.CompetenceDate)
                .DefaultIfEmpty(referenceDate)
                .Max();
            template.MarkGenerated(maxCompetenceDate, command.UserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Recurrence maintenance completed for template {TemplateId}. Paid={Paid}, Generated={Generated}",
                command.RecurrenceId,
                paidNow,
                generated);

            // Log operation
            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "GenerateRecurrence",
                    ResultEntityId = command.RecurrenceId,
                    ResultPayload = JsonSerializer.Serialize(new { Generated = generated, Paid = paidNow })
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

    private static DateTime BuildCompetenceDate(RecurrenceTemplate template, DateTime monthDate)
    {
        var day = Math.Min(template.DayOfMonth, DateTime.DaysInMonth(monthDate.Year, monthDate.Month));
        return new DateTime(monthDate.Year, monthDate.Month, day);
    }

    private static DateTime ResolveDueDate(GestorFinanceiro.Financeiro.Domain.Entity.Transaction transaction)
    {
        return (transaction.DueDate ?? transaction.CompetenceDate).Date;
    }
}

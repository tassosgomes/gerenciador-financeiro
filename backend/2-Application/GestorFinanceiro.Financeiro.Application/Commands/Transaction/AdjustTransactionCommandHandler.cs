using GestorFinanceiro.Financeiro.Application.Commands.Transaction;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Mapster;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GestorFinanceiro.Financeiro.Application.Commands.Transaction;

public class AdjustTransactionCommandHandler : ICommandHandler<AdjustTransactionCommand, TransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TransactionDomainService _transactionDomainService;
    private readonly ILogger<AdjustTransactionCommandHandler> _logger;

    public AdjustTransactionCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IOperationLogRepository operationLogRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        TransactionDomainService transactionDomainService,
        ILogger<AdjustTransactionCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _operationLogRepository = operationLogRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _transactionDomainService = transactionDomainService;
        _logger = logger;
    }

    public async Task<TransactionResponse> HandleAsync(AdjustTransactionCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adjusting transaction with ID: {TransactionId}", command.TransactionId);

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
            // Load transaction
            var originalTransaction = await _transactionRepository.GetByIdAsync(command.TransactionId, cancellationToken);
            if (originalTransaction == null)
                throw new TransactionNotFoundException(command.TransactionId);

            var previousData = originalTransaction.Adapt<TransactionResponse>();

            if (originalTransaction.Status != TransactionStatus.Paid)
                throw new TransactionNotPaidException(command.TransactionId);

            if (originalTransaction.HasAdjustment)
                throw new TransactionAlreadyAdjustedException(command.TransactionId);

            // Load account with lock
            var account = await _accountRepository.GetByIdWithLockAsync(originalTransaction.AccountId, cancellationToken);
            if (account == null)
                throw new AccountNotFoundException(originalTransaction.AccountId);

            // Create adjustment via domain service
            var adjustment = _transactionDomainService.CreateAdjustment(
                account,
                originalTransaction,
                command.CorrectAmount,
                command.UserId,
                command.OperationId);

            await _transactionRepository.AddAsync(adjustment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Transaction", adjustment.Id, "Updated", command.UserId, previousData, cancellationToken);

            // Log operation
            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "AdjustTransaction",
                    ResultEntityId = adjustment.Id,
                    ResultPayload = JsonSerializer.Serialize(adjustment.Adapt<TransactionResponse>())
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Commit
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Transaction adjusted successfully with ID: {Id}", adjustment.Id);

            return adjustment.Adapt<TransactionResponse>();
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

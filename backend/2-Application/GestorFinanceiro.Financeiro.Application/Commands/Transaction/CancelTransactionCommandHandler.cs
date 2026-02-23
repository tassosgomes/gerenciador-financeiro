using GestorFinanceiro.Financeiro.Application.Commands.Transaction;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Mapster;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GestorFinanceiro.Financeiro.Application.Commands.Transaction;

public class CancelTransactionCommandHandler : ICommandHandler<CancelTransactionCommand, TransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IReceiptItemRepository _receiptItemRepository;
    private readonly IEstablishmentRepository _establishmentRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TransactionDomainService _transactionDomainService;
    private readonly ILogger<CancelTransactionCommandHandler> _logger;

    public CancelTransactionCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IReceiptItemRepository receiptItemRepository,
        IEstablishmentRepository establishmentRepository,
        IOperationLogRepository operationLogRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        TransactionDomainService transactionDomainService,
        ILogger<CancelTransactionCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _receiptItemRepository = receiptItemRepository;
        _establishmentRepository = establishmentRepository;
        _operationLogRepository = operationLogRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _transactionDomainService = transactionDomainService;
        _logger = logger;
    }

    public async Task<TransactionResponse> HandleAsync(CancelTransactionCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling transaction with ID: {TransactionId}", command.TransactionId);

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
            var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId, cancellationToken);
            if (transaction == null)
                throw new TransactionNotFoundException(command.TransactionId);

            var previousData = transaction.Adapt<TransactionResponse>();

            // Load account with lock
            var account = await _accountRepository.GetByIdWithLockAsync(transaction.AccountId, cancellationToken);
            if (account == null)
                throw new AccountNotFoundException(transaction.AccountId);

            // Cancel via domain service
            _transactionDomainService.CancelTransaction(account, transaction, command.UserId, command.Reason);

            var receiptItems = await _receiptItemRepository.GetByTransactionIdAsync(transaction.Id, cancellationToken);
            if (receiptItems.Count > 0)
            {
                _receiptItemRepository.RemoveRange(receiptItems);
            }

            var establishment = await _establishmentRepository.GetByTransactionIdAsync(transaction.Id, cancellationToken);
            if (establishment != null)
            {
                _establishmentRepository.Remove(establishment);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Transaction", transaction.Id, "Cancelled", command.UserId, previousData, cancellationToken);

            if (receiptItems.Count > 0 || establishment != null)
            {
                await _auditService.LogAsync("Receipt", transaction.Id, "CascadeDeletedOnTransactionCancellation", command.UserId, null, cancellationToken);
            }

            // Log operation
            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "CancelTransaction",
                    ResultEntityId = transaction.Id,
                    ResultPayload = JsonSerializer.Serialize(transaction.Adapt<TransactionResponse>())
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Commit
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Transaction cancelled successfully with ID: {Id}", transaction.Id);

            return transaction.Adapt<TransactionResponse>() with { HasReceipt = false };
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

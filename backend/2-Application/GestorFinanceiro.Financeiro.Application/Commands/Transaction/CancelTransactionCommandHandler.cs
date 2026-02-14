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

public class CancelTransactionCommandHandler : ICommandHandler<CancelTransactionCommand, Unit>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TransactionDomainService _transactionDomainService;
    private readonly ILogger<CancelTransactionCommandHandler> _logger;

    public CancelTransactionCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IOperationLogRepository operationLogRepository,
        IUnitOfWork unitOfWork,
        TransactionDomainService transactionDomainService,
        ILogger<CancelTransactionCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _operationLogRepository = operationLogRepository;
        _unitOfWork = unitOfWork;
        _transactionDomainService = transactionDomainService;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(CancelTransactionCommand command, CancellationToken cancellationToken)
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

            // Load account with lock
            var account = await _accountRepository.GetByIdWithLockAsync(transaction.AccountId, cancellationToken);
            if (account == null)
                throw new AccountNotFoundException(transaction.AccountId);

            // Cancel via domain service
            _transactionDomainService.CancelTransaction(account, transaction, command.UserId, command.Reason);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

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

            return Unit.Value;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

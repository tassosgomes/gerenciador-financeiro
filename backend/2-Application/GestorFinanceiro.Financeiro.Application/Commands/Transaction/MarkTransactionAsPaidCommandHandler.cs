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

public class MarkTransactionAsPaidCommandHandler : ICommandHandler<MarkTransactionAsPaidCommand, TransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TransactionDomainService _transactionDomainService;
    private readonly ILogger<MarkTransactionAsPaidCommandHandler> _logger;

    public MarkTransactionAsPaidCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IOperationLogRepository operationLogRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        TransactionDomainService transactionDomainService,
        ILogger<MarkTransactionAsPaidCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _operationLogRepository = operationLogRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _transactionDomainService = transactionDomainService;
        _logger = logger;
    }

    public async Task<TransactionResponse> HandleAsync(MarkTransactionAsPaidCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Marking transaction as paid: {TransactionId}", command.TransactionId);

        if (!string.IsNullOrEmpty(command.OperationId))
        {
            var existingLog = await _operationLogRepository.ExistsByOperationIdAsync(
                command.OperationId,
                cancellationToken);

            if (existingLog)
            {
                throw new DuplicateOperationException(command.OperationId);
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId, cancellationToken);
            if (transaction is null)
            {
                throw new TransactionNotFoundException(command.TransactionId);
            }

            var previousData = transaction.Adapt<TransactionResponse>();

            var account = await _accountRepository.GetByIdWithLockAsync(transaction.AccountId, cancellationToken);
            if (account is null)
            {
                throw new AccountNotFoundException(transaction.AccountId);
            }

            _transactionDomainService.MarkTransactionAsPaid(account, transaction, command.UserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                "Transaction",
                transaction.Id,
                "StatusChanged",
                command.UserId,
                previousData,
                cancellationToken);

            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "MarkTransactionAsPaid",
                    ResultEntityId = transaction.Id,
                    ResultPayload = JsonSerializer.Serialize(transaction.Adapt<TransactionResponse>())
                }, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Transaction marked as paid successfully: {TransactionId}", transaction.Id);

            return transaction.Adapt<TransactionResponse>();
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

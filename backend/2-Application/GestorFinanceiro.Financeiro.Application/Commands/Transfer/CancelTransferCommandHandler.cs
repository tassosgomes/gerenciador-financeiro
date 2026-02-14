using GestorFinanceiro.Financeiro.Application.Commands.Transfer;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GestorFinanceiro.Financeiro.Application.Commands.Transfer;

public class CancelTransferCommandHandler : ICommandHandler<CancelTransferCommand, Unit>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TransferDomainService _transferDomainService;
    private readonly ILogger<CancelTransferCommandHandler> _logger;

    public CancelTransferCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IOperationLogRepository operationLogRepository,
        IUnitOfWork unitOfWork,
        TransferDomainService transferDomainService,
        ILogger<CancelTransferCommandHandler> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _operationLogRepository = operationLogRepository ?? throw new ArgumentNullException(nameof(operationLogRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _transferDomainService = transferDomainService ?? throw new ArgumentNullException(nameof(transferDomainService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Unit> HandleAsync(
        CancelTransferCommand command, CancellationToken cancellationToken)
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
            // Load transfer group transactions
            var transferTransactions = await _transactionRepository.GetByTransferGroupAsync(command.TransferGroupId, cancellationToken);
            var transactions = transferTransactions.ToList();
            if (transactions.Count != 2)
                throw new TransactionNotFoundException(command.TransferGroupId);

            var debit = transactions.First(t => t.Type == Domain.Enum.TransactionType.Debit);
            var credit = transactions.First(t => t.Type == Domain.Enum.TransactionType.Credit);

            // Load accounts with lock
            var sourceAccount = await _accountRepository.GetByIdWithLockAsync(debit.AccountId, cancellationToken);
            if (sourceAccount == null)
                throw new AccountNotFoundException(debit.AccountId);

            var destinationAccount = await _accountRepository.GetByIdWithLockAsync(credit.AccountId, cancellationToken);
            if (destinationAccount == null)
                throw new AccountNotFoundException(credit.AccountId);

            // Cancel transfer
            _transferDomainService.CancelTransfer(
                sourceAccount,
                destinationAccount,
                debit,
                credit,
                command.UserId,
                command.Reason);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Log operation
            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "CancelTransfer",
                    ResultEntityId = command.TransferGroupId,
                    ResultPayload = JsonSerializer.Serialize(new { Canceled = true })
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Commit
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Transfer {Id} canceled successfully", command.TransferGroupId);

            return Unit.Value;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

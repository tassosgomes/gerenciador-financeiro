using GestorFinanceiro.Financeiro.Application.Commands.Installment;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Mapster;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GestorFinanceiro.Financeiro.Application.Commands.Installment;

public class CancelInstallmentGroupCommandHandler : ICommandHandler<CancelInstallmentGroupCommand, IReadOnlyList<TransactionResponse>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly InstallmentDomainService _installmentDomainService;
    private readonly ILogger<CancelInstallmentGroupCommandHandler> _logger;

    public CancelInstallmentGroupCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IOperationLogRepository operationLogRepository,
        IUnitOfWork unitOfWork,
        InstallmentDomainService installmentDomainService,
        ILogger<CancelInstallmentGroupCommandHandler> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _operationLogRepository = operationLogRepository ?? throw new ArgumentNullException(nameof(operationLogRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _installmentDomainService = installmentDomainService ?? throw new ArgumentNullException(nameof(installmentDomainService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<TransactionResponse>> HandleAsync(
        CancelInstallmentGroupCommand command, CancellationToken cancellationToken)
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
            // Load group transactions
            var groupTransactions = (await _transactionRepository.GetByInstallmentGroupAsync(command.GroupId, cancellationToken)).ToList();
            if (groupTransactions.Count == 0)
                throw new TransactionNotFoundException(command.GroupId);

            // Load account with lock
            var accountId = groupTransactions.First().AccountId;
            var account = await _accountRepository.GetByIdWithLockAsync(accountId, cancellationToken);
            if (account == null)
                throw new AccountNotFoundException(accountId);

            // Cancel installment group
            _installmentDomainService.CancelInstallmentGroup(
                account,
                groupTransactions,
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
                    OperationType = "CancelInstallmentGroup",
                    ResultEntityId = command.GroupId,
                    ResultPayload = JsonSerializer.Serialize(new { Canceled = true })
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Commit
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Installment group {Id} canceled successfully", command.GroupId);

            var cancelledTransactions = groupTransactions
                .Where(transaction => transaction.Status == Domain.Enum.TransactionStatus.Cancelled)
                .Adapt<IReadOnlyList<TransactionResponse>>();

            return cancelledTransactions;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

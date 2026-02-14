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

public class AdjustInstallmentGroupCommandHandler : ICommandHandler<AdjustInstallmentGroupCommand, IReadOnlyList<TransactionResponse>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly InstallmentDomainService _installmentDomainService;
    private readonly ILogger<AdjustInstallmentGroupCommandHandler> _logger;

    public AdjustInstallmentGroupCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IOperationLogRepository operationLogRepository,
        IUnitOfWork unitOfWork,
        InstallmentDomainService installmentDomainService,
        ILogger<AdjustInstallmentGroupCommandHandler> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _operationLogRepository = operationLogRepository ?? throw new ArgumentNullException(nameof(operationLogRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _installmentDomainService = installmentDomainService ?? throw new ArgumentNullException(nameof(installmentDomainService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<TransactionResponse>> HandleAsync(
        AdjustInstallmentGroupCommand command, CancellationToken cancellationToken)
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
            var groupTransactions = await _transactionRepository.GetByInstallmentGroupAsync(command.GroupId, cancellationToken);
            if (!groupTransactions.Any())
                throw new TransactionNotFoundException(command.GroupId); // Or specific exception

            // Load account with lock (use the account from first transaction)
            var accountId = groupTransactions.First().AccountId;
            var account = await _accountRepository.GetByIdWithLockAsync(accountId, cancellationToken);
            if (account == null)
                throw new AccountNotFoundException(accountId);

            // Adjust installment group
            var adjustments = _installmentDomainService.AdjustInstallmentGroup(
                account,
                groupTransactions,
                command.NewTotalAmount,
                command.UserId,
                command.OperationId);

            // Add adjustment transactions
            foreach (var adjustment in adjustments)
            {
                await _transactionRepository.AddAsync(adjustment, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Log operation
            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "AdjustInstallmentGroup",
                    ResultEntityId = adjustments.FirstOrDefault()?.Id ?? command.GroupId,
                    ResultPayload = JsonSerializer.Serialize(adjustments.Adapt<IReadOnlyList<TransactionResponse>>())
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Commit
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Installment group adjusted successfully with {Count} adjustments", adjustments.Count);

            return adjustments.Adapt<IReadOnlyList<TransactionResponse>>();
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

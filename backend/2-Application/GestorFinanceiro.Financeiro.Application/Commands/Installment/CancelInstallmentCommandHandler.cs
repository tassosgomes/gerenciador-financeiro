using GestorFinanceiro.Financeiro.Application.Commands.Installment;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GestorFinanceiro.Financeiro.Application.Commands.Installment;

public class CancelInstallmentCommandHandler : ICommandHandler<CancelInstallmentCommand, Unit>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly InstallmentDomainService _installmentDomainService;
    private readonly ILogger<CancelInstallmentCommandHandler> _logger;

    public CancelInstallmentCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IOperationLogRepository operationLogRepository,
        IUnitOfWork unitOfWork,
        InstallmentDomainService installmentDomainService,
        ILogger<CancelInstallmentCommandHandler> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _operationLogRepository = operationLogRepository ?? throw new ArgumentNullException(nameof(operationLogRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _installmentDomainService = installmentDomainService ?? throw new ArgumentNullException(nameof(installmentDomainService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Unit> HandleAsync(
        CancelInstallmentCommand command, CancellationToken cancellationToken)
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
            // Load installment
            var installment = await _transactionRepository.GetByIdAsync(command.InstallmentId, cancellationToken);
            if (installment == null)
                throw new TransactionNotFoundException(command.InstallmentId);

            // Load account with lock
            var account = await _accountRepository.GetByIdWithLockAsync(installment.AccountId, cancellationToken);
            if (account == null)
                throw new AccountNotFoundException(installment.AccountId);

            // Cancel installment
            _installmentDomainService.CancelSingleInstallment(
                account,
                installment,
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
                    OperationType = "CancelInstallment",
                    ResultEntityId = command.InstallmentId,
                    ResultPayload = JsonSerializer.Serialize(new { Canceled = true })
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Commit
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Installment {Id} canceled successfully", command.InstallmentId);

            return Unit.Value;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

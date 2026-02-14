using GestorFinanceiro.Financeiro.Application.Commands.Account;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GestorFinanceiro.Financeiro.Application.Commands.Account;

public class DeactivateAccountCommandHandler : ICommandHandler<DeactivateAccountCommand, Unit>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeactivateAccountCommandHandler> _logger;

    public DeactivateAccountCommandHandler(
        IAccountRepository accountRepository,
        IOperationLogRepository operationLogRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ILogger<DeactivateAccountCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _operationLogRepository = operationLogRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(DeactivateAccountCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating account with ID: {AccountId}", command.AccountId);

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
            // Load account with lock
            var account = await _accountRepository.GetByIdWithLockAsync(command.AccountId, cancellationToken);
            if (account == null)
                throw new AccountNotFoundException(command.AccountId);

            var previousData = account.Adapt<AccountResponse>();

            // Deactivate
            account.Deactivate(command.UserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Account", account.Id, "Deactivated", command.UserId, previousData, cancellationToken);

            // Log operation
            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "DeactivateAccount",
                    ResultEntityId = account.Id,
                    ResultPayload = JsonSerializer.Serialize(account.Adapt<AccountResponse>())
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Commit
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Account deactivated successfully with ID: {Id}", account.Id);

            return Unit.Value;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

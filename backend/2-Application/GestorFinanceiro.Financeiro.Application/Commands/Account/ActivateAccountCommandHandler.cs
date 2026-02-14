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

public class ActivateAccountCommandHandler : ICommandHandler<ActivateAccountCommand, Unit>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActivateAccountCommandHandler> _logger;

    public ActivateAccountCommandHandler(
        IAccountRepository accountRepository,
        IOperationLogRepository operationLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<ActivateAccountCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _operationLogRepository = operationLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(ActivateAccountCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activating account with ID: {AccountId}", command.AccountId);

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

            // Activate
            account.Activate(command.UserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Log operation
            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "ActivateAccount",
                    ResultEntityId = account.Id,
                    ResultPayload = JsonSerializer.Serialize(account.Adapt<AccountResponse>())
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Commit
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Account activated successfully with ID: {Id}", account.Id);

            return Unit.Value;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

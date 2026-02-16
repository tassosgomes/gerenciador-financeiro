using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GestorFinanceiro.Financeiro.Application.Commands.Account;

public class UpdateAccountCommandHandler : ICommandHandler<UpdateAccountCommand, AccountResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateAccountCommandHandler> _logger;

    public UpdateAccountCommandHandler(
        IAccountRepository accountRepository,
        IOperationLogRepository operationLogRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateAccountCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _operationLogRepository = operationLogRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AccountResponse> HandleAsync(UpdateAccountCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating account with ID: {AccountId}", command.AccountId);

        var validator = new UpdateAccountCommandValidator();
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        if (!string.IsNullOrEmpty(command.OperationId))
        {
            var existingLog = await _operationLogRepository.ExistsByOperationIdAsync(command.OperationId, cancellationToken);
            if (existingLog)
            {
                throw new DuplicateOperationException(command.OperationId);
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var account = await _accountRepository.GetByIdWithLockAsync(command.AccountId, cancellationToken);
            if (account == null)
            {
                throw new AccountNotFoundException(command.AccountId);
            }

            var previousData = account.Adapt<AccountResponse>();

            // Bifurcação por tipo de conta
            if (account.CreditCard != null)
            {
                // É cartão - validar conta de débito se fornecida
                if (command.DebitAccountId.HasValue)
                {
                    var debitAccount = await _accountRepository.GetByIdAsync(command.DebitAccountId.Value, cancellationToken);
                    if (debitAccount == null)
                        throw new AccountNotFoundException(command.DebitAccountId.Value);
                    if (!debitAccount.IsActive)
                        throw new InvalidCreditCardConfigException("Conta de débito está inativa.");
                    if (debitAccount.Type != AccountType.Corrente && debitAccount.Type != AccountType.Carteira)
                        throw new InvalidCreditCardConfigException("Conta de débito deve ser do tipo Corrente ou Carteira.");
                }

                // Usar valores existentes se não fornecidos
                var creditLimit = command.CreditLimit ?? account.CreditCard.CreditLimit;
                var closingDay = command.ClosingDay ?? account.CreditCard.ClosingDay;
                var dueDay = command.DueDay ?? account.CreditCard.DueDay;
                var debitAccountId = command.DebitAccountId ?? account.CreditCard.DebitAccountId;
                var enforceCreditLimit = command.EnforceCreditLimit ?? account.CreditCard.EnforceCreditLimit;

                account.UpdateCreditCard(
                    command.Name,
                    creditLimit,
                    closingDay,
                    dueDay,
                    debitAccountId,
                    enforceCreditLimit,
                    command.UserId);
            }
            else
            {
                // Fluxo existente para contas regulares
                account.Update(command.Name, command.AllowNegativeBalance, command.UserId);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Account", account.Id, "Updated", command.UserId, previousData, cancellationToken);

            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "UpdateAccount",
                    ResultEntityId = account.Id,
                    ResultPayload = JsonSerializer.Serialize(account.Adapt<AccountResponse>())
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Account updated successfully with ID: {Id}", account.Id);

            return account.Adapt<AccountResponse>();
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Commands.Account;
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

public class CreateAccountCommandHandler : ICommandHandler<CreateAccountCommand, AccountResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAccountCommandHandler> _logger;

    public CreateAccountCommandHandler(
        IAccountRepository accountRepository,
        IOperationLogRepository operationLogRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ILogger<CreateAccountCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _operationLogRepository = operationLogRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AccountResponse> HandleAsync(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating account with name: {Name}", command.Name);

        var validator = new CreateAccountCommandValidator();
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        // Check idempotência
        if (!string.IsNullOrEmpty(command.OperationId))
        {
            var existingLog = await _operationLogRepository.ExistsByOperationIdAsync(
                command.OperationId, cancellationToken);
            if (existingLog)
                throw new DuplicateOperationException(command.OperationId);
        }

        // Check if name exists
        var nameExists = await _accountRepository.ExistsByNameAsync(command.Name, cancellationToken);
        if (nameExists)
            throw new AccountNameAlreadyExistsException(command.Name);

        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Bifurcação por tipo de conta
            GestorFinanceiro.Financeiro.Domain.Entity.Account account;

            if (command.Type == AccountType.Cartao)
            {
                // Validar conta de débito
                var debitAccount = await _accountRepository.GetByIdAsync(command.DebitAccountId!.Value, cancellationToken);
                if (debitAccount == null)
                    throw new AccountNotFoundException(command.DebitAccountId!.Value);
                if (!debitAccount.IsActive)
                    throw new InvalidCreditCardConfigException("Conta de débito está inativa.");
                if (debitAccount.Type != AccountType.Corrente && debitAccount.Type != AccountType.Carteira)
                    throw new InvalidCreditCardConfigException("Conta de débito deve ser do tipo Corrente ou Carteira.");

                // Criar cartão de crédito
                account = GestorFinanceiro.Financeiro.Domain.Entity.Account.CreateCreditCard(
                    command.Name,
                    command.CreditLimit!.Value,
                    command.ClosingDay!.Value,
                    command.DueDay!.Value,
                    command.DebitAccountId!.Value,
                    command.EnforceCreditLimit ?? true,
                    command.UserId);
            }
            else
            {
                // Fluxo existente para outros tipos
                account = GestorFinanceiro.Financeiro.Domain.Entity.Account.Create(
                    command.Name,
                    command.Type,
                    command.InitialBalance,
                    command.AllowNegativeBalance,
                    command.UserId);
            }

            await _accountRepository.AddAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Account", account.Id, "Created", command.UserId, null, cancellationToken);

            // Log operation
            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "CreateAccount",
                    ResultEntityId = account.Id,
                    ResultPayload = JsonSerializer.Serialize(account.Adapt<AccountResponse>())
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Commit
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Account created successfully with ID: {Id}", account.Id);

            return account.Adapt<AccountResponse>();
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

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

public class CreateAccountCommandHandler : ICommandHandler<CreateAccountCommand, AccountResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAccountCommandHandler> _logger;

    public CreateAccountCommandHandler(
        IAccountRepository accountRepository,
        IOperationLogRepository operationLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateAccountCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _operationLogRepository = operationLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AccountResponse> HandleAsync(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating account with name: {Name}", command.Name);

        var validator = new CreateAccountCommandValidator();
        var validationResult = validator.Validate(command);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Validation failed: {string.Join(", ", validationResult.Errors.Select(error => error.ErrorMessage))}");
        }

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
            // Create account
            var account = GestorFinanceiro.Financeiro.Domain.Entity.Account.Create(
                command.Name,
                command.Type,
                command.InitialBalance,
                command.AllowNegativeBalance,
                command.UserId);

            await _accountRepository.AddAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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

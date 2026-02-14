using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Commands.Transaction;
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

public class CreateTransactionCommandHandler : ICommandHandler<CreateTransactionCommand, TransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TransactionDomainService _transactionDomainService;
    private readonly CreateTransactionValidator _validator;
    private readonly ILogger<CreateTransactionCommandHandler> _logger;

    public CreateTransactionCommandHandler(
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository,
        ITransactionRepository transactionRepository,
        IOperationLogRepository operationLogRepository,
        IUnitOfWork unitOfWork,
        TransactionDomainService transactionDomainService,
        CreateTransactionValidator validator,
        ILogger<CreateTransactionCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _transactionRepository = transactionRepository;
        _operationLogRepository = operationLogRepository;
        _unitOfWork = unitOfWork;
        _transactionDomainService = transactionDomainService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<TransactionResponse> HandleAsync(CreateTransactionCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating transaction for account: {AccountId}", command.AccountId);

        // Check idempotência
        if (!string.IsNullOrEmpty(command.OperationId))
        {
            var existingLog = await _operationLogRepository.ExistsByOperationIdAsync(
                command.OperationId, cancellationToken);
            if (existingLog)
                throw new DuplicateOperationException(command.OperationId);
        }

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Load account with lock
            var account = await _accountRepository.GetByIdWithLockAsync(command.AccountId, cancellationToken);
            if (account == null)
                throw new AccountNotFoundException(command.AccountId);

            // Validate category exists
            var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
            if (category == null)
                throw new CategoryNotFoundException(command.CategoryId);

            // Create transaction via domain service
            var transaction = _transactionDomainService.CreateTransaction(
                account,
                command.CategoryId,
                command.Type,
                command.Amount,
                command.Description,
                command.CompetenceDate,
                command.DueDate,
                command.Status,
                command.UserId,
                command.OperationId);

            await _transactionRepository.AddAsync(transaction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = transaction.Adapt<TransactionResponse>();

            // Log operation
            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "CreateTransaction",
                    ResultEntityId = transaction.Id,
                    ResultPayload = JsonSerializer.Serialize(response)
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Commit
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Transaction created successfully with ID: {Id}", transaction.Id);

            return response;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

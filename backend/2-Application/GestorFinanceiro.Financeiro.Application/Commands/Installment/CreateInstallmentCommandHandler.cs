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

public class CreateInstallmentCommandHandler : ICommandHandler<CreateInstallmentCommand, IReadOnlyList<TransactionResponse>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly InstallmentDomainService _installmentDomainService;
    private readonly ILogger<CreateInstallmentCommandHandler> _logger;

    public CreateInstallmentCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IOperationLogRepository operationLogRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        InstallmentDomainService installmentDomainService,
        ILogger<CreateInstallmentCommandHandler> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _operationLogRepository = operationLogRepository ?? throw new ArgumentNullException(nameof(operationLogRepository));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _installmentDomainService = installmentDomainService ?? throw new ArgumentNullException(nameof(installmentDomainService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<TransactionResponse>> HandleAsync(
        CreateInstallmentCommand command, CancellationToken cancellationToken)
    {
        // Validate command
        var validator = new CreateInstallmentValidator();
        var validationResult = validator.Validate(command);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

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

            // Create installment group
            var installments = _installmentDomainService.CreateInstallmentGroup(
                account,
                command.CategoryId,
                command.Type,
                command.TotalAmount,
                command.InstallmentCount,
                command.Description,
                command.FirstCompetenceDate,
                command.FirstDueDate,
                command.UserId,
                command.OperationId);

            // Add transactions
            foreach (var installment in installments)
            {
                await _transactionRepository.AddAsync(installment, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var installment in installments)
            {
                await _auditService.LogAsync("Transaction", installment.Id, "Created", command.UserId, null, cancellationToken);
            }

            // Log operation
            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "CreateInstallment",
                    ResultEntityId = installments.First().Id,
                    ResultPayload = JsonSerializer.Serialize(installments.Adapt<IReadOnlyList<TransactionResponse>>())
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Commit
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Installment group created successfully with {Count} installments", installments.Count);

            return installments.Adapt<IReadOnlyList<TransactionResponse>>();
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

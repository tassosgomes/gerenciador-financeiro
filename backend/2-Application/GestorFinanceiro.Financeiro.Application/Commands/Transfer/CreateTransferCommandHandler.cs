using GestorFinanceiro.Financeiro.Application.Commands.Transfer;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Mapster;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GestorFinanceiro.Financeiro.Application.Commands.Transfer;

public class CreateTransferCommandHandler : ICommandHandler<CreateTransferCommand, IReadOnlyList<TransactionResponse>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TransferDomainService _transferDomainService;
    private readonly ILogger<CreateTransferCommandHandler> _logger;

    public CreateTransferCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IOperationLogRepository operationLogRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        TransferDomainService transferDomainService,
        ILogger<CreateTransferCommandHandler> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _operationLogRepository = operationLogRepository ?? throw new ArgumentNullException(nameof(operationLogRepository));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _transferDomainService = transferDomainService ?? throw new ArgumentNullException(nameof(transferDomainService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<TransactionResponse>> HandleAsync(
        CreateTransferCommand command,
        CancellationToken cancellationToken)
    {
        var validator = new CreateTransferValidator();
        var validationResult = validator.Validate(command);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Validation failed: {string.Join(", ", validationResult.Errors.Select(error => error.ErrorMessage))}");
        }

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
            var sourceAccount = await _accountRepository.GetByIdWithLockAsync(command.SourceAccountId, cancellationToken);
            if (sourceAccount == null)
            {
                throw new AccountNotFoundException(command.SourceAccountId);
            }

            var destinationAccount = await _accountRepository.GetByIdWithLockAsync(command.DestinationAccountId, cancellationToken);
            if (destinationAccount == null)
            {
                throw new AccountNotFoundException(command.DestinationAccountId);
            }

            var transfer = _transferDomainService.CreateTransfer(
                sourceAccount,
                destinationAccount,
                command.CategoryId,
                command.Amount,
                command.Description,
                command.CompetenceDate,
                command.UserId,
                command.OperationId);

            await _transactionRepository.AddAsync(transfer.debit, cancellationToken);
            await _transactionRepository.AddAsync(transfer.credit, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Transaction", transfer.debit.Id, "Created", command.UserId, null, cancellationToken);
            await _auditService.LogAsync("Transaction", transfer.credit.Id, "Created", command.UserId, null, cancellationToken);

            var response = new[]
            {
                transfer.debit.Adapt<TransactionResponse>(),
                transfer.credit.Adapt<TransactionResponse>(),
            };

            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "CreateTransfer",
                    ResultEntityId = transfer.debit.TransferGroupId ?? transfer.debit.Id,
                    ResultPayload = JsonSerializer.Serialize(response),
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Transfer created successfully. Source: {SourceAccountId}, Destination: {DestinationAccountId}",
                command.SourceAccountId,
                command.DestinationAccountId);

            return response;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

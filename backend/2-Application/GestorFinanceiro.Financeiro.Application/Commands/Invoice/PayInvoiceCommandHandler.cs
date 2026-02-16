using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Mapster;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GestorFinanceiro.Financeiro.Application.Commands.Invoice;

public class PayInvoiceCommandHandler : ICommandHandler<PayInvoiceCommand, IReadOnlyList<TransactionResponse>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TransferDomainService _transferDomainService;
    private readonly ILogger<PayInvoiceCommandHandler> _logger;

    public PayInvoiceCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository,
        IOperationLogRepository operationLogRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        TransferDomainService transferDomainService,
        ILogger<PayInvoiceCommandHandler> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _operationLogRepository = operationLogRepository ?? throw new ArgumentNullException(nameof(operationLogRepository));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _transferDomainService = transferDomainService ?? throw new ArgumentNullException(nameof(transferDomainService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<TransactionResponse>> HandleAsync(
        PayInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        var validator = new PayInvoiceCommandValidator();
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
            var creditCardAccount = await _accountRepository.GetByIdWithLockAsync(command.CreditCardAccountId, cancellationToken);
            if (creditCardAccount == null)
            {
                throw new AccountNotFoundException(command.CreditCardAccountId);
            }

            if (creditCardAccount.CreditCard == null)
            {
                throw new AccountIsNotCreditCardException(command.CreditCardAccountId);
            }

            var debitAccountId = creditCardAccount.CreditCard.DebitAccountId;
            var debitAccount = await _accountRepository.GetByIdWithLockAsync(debitAccountId, cancellationToken);
            if (debitAccount == null)
            {
                throw new AccountNotFoundException(debitAccountId);
            }

            if (!debitAccount.IsActive)
            {
                throw new InactiveAccountException(debitAccountId);
            }

            var allCategories = await _categoryRepository.GetAllAsync(cancellationToken);
            var invoicePaymentCategory = allCategories.FirstOrDefault(c =>
                c.Name == "Pagamento de Fatura" &&
                c.Type == CategoryType.Despesa &&
                c.IsSystem);

            if (invoicePaymentCategory == null)
            {
                throw new InvoicePaymentCategoryNotFoundException();
            }

            var transactions = _transferDomainService.CreateInvoicePayment(
                debitAccount,
                creditCardAccount,
                command.Amount,
                command.CompetenceDate,
                invoicePaymentCategory.Id,
                command.UserId,
                command.OperationId);

            foreach (var transaction in transactions)
            {
                await _transactionRepository.AddAsync(transaction, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var transaction in transactions)
            {
                await _auditService.LogAsync("Transaction", transaction.Id, "Created", command.UserId, null, cancellationToken);
            }

            var response = transactions.Select(t => t.Adapt<TransactionResponse>()).ToList();

            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "PayInvoice",
                    ResultEntityId = transactions.First().TransferGroupId ?? transactions.First().Id,
                    ResultPayload = JsonSerializer.Serialize(response),
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Invoice payment created successfully. Credit Card: {CreditCardAccountId}, Amount: {Amount}",
                command.CreditCardAccountId,
                command.Amount);

            return response;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

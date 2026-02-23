using System.Globalization;
using System.Text.Json;
using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Mapster;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Commands.Receipt;

public class ImportNfceCommandHandler : ICommandHandler<ImportNfceCommand, ImportNfceResponse>
{
    private static readonly CultureInfo PtBrCulture = new("pt-BR");

    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IEstablishmentRepository _establishmentRepository;
    private readonly IReceiptItemRepository _receiptItemRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly ISefazNfceService _sefazNfceService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TransactionDomainService _transactionDomainService;
    private readonly IValidator<ImportNfceCommand> _validator;
    private readonly ILogger<ImportNfceCommandHandler> _logger;

    public ImportNfceCommandHandler(
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository,
        ITransactionRepository transactionRepository,
        IEstablishmentRepository establishmentRepository,
        IReceiptItemRepository receiptItemRepository,
        IOperationLogRepository operationLogRepository,
        ISefazNfceService sefazNfceService,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        TransactionDomainService transactionDomainService,
        IValidator<ImportNfceCommand> validator,
        ILogger<ImportNfceCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _transactionRepository = transactionRepository;
        _establishmentRepository = establishmentRepository;
        _receiptItemRepository = receiptItemRepository;
        _operationLogRepository = operationLogRepository;
        _sefazNfceService = sefazNfceService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _transactionDomainService = transactionDomainService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ImportNfceResponse> HandleAsync(ImportNfceCommand command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        if (!string.IsNullOrEmpty(command.OperationId))
        {
            var existingOperation = await _operationLogRepository.ExistsByOperationIdAsync(command.OperationId, cancellationToken);
            if (existingOperation)
            {
                throw new DuplicateOperationException(command.OperationId);
            }
        }

        var alreadyImported = await _establishmentRepository.ExistsByAccessKeyAsync(command.AccessKey, cancellationToken);
        if (alreadyImported)
        {
            throw new DuplicateReceiptException(command.AccessKey);
        }

        var nfceData = await _sefazNfceService.LookupAsync(command.AccessKey, cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var account = await _accountRepository.GetByIdWithLockAsync(command.AccountId, cancellationToken);
            if (account == null)
            {
                throw new AccountNotFoundException(command.AccountId);
            }

            var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
            if (category == null)
            {
                throw new CategoryNotFoundException(command.CategoryId);
            }

            var description = BuildDescription(command.Description, nfceData.DiscountAmount, nfceData.TotalAmount);

            var transaction = _transactionDomainService.CreateTransaction(
                account,
                command.CategoryId,
                TransactionType.Debit,
                nfceData.PaidAmount,
                description,
                command.CompetenceDate,
                null,
                TransactionStatus.Paid,
                command.UserId,
                command.OperationId);

            await _transactionRepository.AddAsync(transaction, cancellationToken);

            var establishment = Establishment.Create(
                transaction.Id,
                nfceData.EstablishmentName,
                nfceData.EstablishmentCnpj,
                nfceData.AccessKey,
                command.UserId);

            await _establishmentRepository.AddAsync(establishment, cancellationToken);

            var receiptItems = nfceData.Items
                .Select((item, index) => ReceiptItem.Create(
                    transaction.Id,
                    item.Description,
                    item.ProductCode,
                    item.Quantity,
                    item.UnitOfMeasure,
                    item.UnitPrice,
                    item.TotalPrice,
                    index + 1,
                    command.UserId))
                .ToList();

            await _receiptItemRepository.AddRangeAsync(receiptItems, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Transaction", transaction.Id, "ImportedNfce", command.UserId, null, cancellationToken);

            var transactionResponse = transaction.Adapt<TransactionResponse>() with { HasReceipt = true };
            var response = new ImportNfceResponse(
                transactionResponse,
                establishment.Adapt<EstablishmentResponse>(),
                receiptItems.Adapt<IReadOnlyList<ReceiptItemResponse>>());

            if (!string.IsNullOrEmpty(command.OperationId))
            {
                await _operationLogRepository.AddAsync(new OperationLog
                {
                    OperationId = command.OperationId,
                    OperationType = "ImportNfce",
                    ResultEntityId = transaction.Id,
                    ResultPayload = JsonSerializer.Serialize(response)
                }, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("NFC-e imported successfully for access key {AccessKey} and transaction {TransactionId}", command.AccessKey, transaction.Id);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string BuildDescription(string baseDescription, decimal discountAmount, decimal totalAmount)
    {
        if (discountAmount <= 0)
        {
            return baseDescription;
        }

        var discountText = discountAmount.ToString("C2", PtBrCulture);
        var totalText = totalAmount.ToString("C2", PtBrCulture);
        return $"{baseDescription} — Desconto de {discountText} aplicado. Valor original: {totalText}";
    }
}

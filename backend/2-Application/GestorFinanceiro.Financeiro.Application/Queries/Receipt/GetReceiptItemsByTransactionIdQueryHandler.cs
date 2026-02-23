using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Receipt;

public class GetReceiptItemsByTransactionIdQueryHandler : IQueryHandler<GetReceiptItemsByTransactionIdQuery, TransactionReceiptResponse>
{
    private readonly IEstablishmentRepository _establishmentRepository;
    private readonly IReceiptItemRepository _receiptItemRepository;
    private readonly ILogger<GetReceiptItemsByTransactionIdQueryHandler> _logger;

    public GetReceiptItemsByTransactionIdQueryHandler(
        IEstablishmentRepository establishmentRepository,
        IReceiptItemRepository receiptItemRepository,
        ILogger<GetReceiptItemsByTransactionIdQueryHandler> logger)
    {
        _establishmentRepository = establishmentRepository;
        _receiptItemRepository = receiptItemRepository;
        _logger = logger;
    }

    public async Task<TransactionReceiptResponse> HandleAsync(GetReceiptItemsByTransactionIdQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting receipt items for transaction {TransactionId}", query.TransactionId);

        var establishment = await _establishmentRepository.GetByTransactionIdAsync(query.TransactionId, cancellationToken);
        if (establishment == null)
        {
            throw new NfceNotFoundException(query.TransactionId.ToString());
        }

        var receiptItems = await _receiptItemRepository.GetByTransactionIdAsync(query.TransactionId, cancellationToken);

        return new TransactionReceiptResponse(
            establishment.Adapt<EstablishmentResponse>(),
            receiptItems.Adapt<IReadOnlyList<ReceiptItemResponse>>());
    }
}

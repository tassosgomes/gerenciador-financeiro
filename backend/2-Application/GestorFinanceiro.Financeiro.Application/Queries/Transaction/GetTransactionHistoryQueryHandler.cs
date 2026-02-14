using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Transaction;

public class GetTransactionHistoryQueryHandler : IQueryHandler<GetTransactionHistoryQuery, TransactionHistoryResponse>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<GetTransactionHistoryQueryHandler> _logger;

    public GetTransactionHistoryQueryHandler(
        ITransactionRepository transactionRepository,
        ILogger<GetTransactionHistoryQueryHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<TransactionHistoryResponse> HandleAsync(GetTransactionHistoryQuery query, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == query.TransactionId, cancellationToken);

        if (transaction is null)
        {
            throw new TransactionNotFoundException(query.TransactionId);
        }

        var referenceTransactionId = transaction.OriginalTransactionId ?? transaction.Id;

        var transactions = await _transactionRepository.Query()
            .AsNoTracking()
            .Where(item => item.Id == referenceTransactionId || item.OriginalTransactionId == referenceTransactionId)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Loaded transaction history for reference {ReferenceTransactionId} with {Count} entries",
            referenceTransactionId,
            transactions.Count);

        var entries = transactions
            .Select(item =>
            {
                var actionType = GetActionType(item.Id == referenceTransactionId, item.Status);
                return new TransactionHistoryEntry(item.Adapt<TransactionResponse>(), actionType);
            })
            .ToList();

        return new TransactionHistoryResponse(entries);
    }

    private static string GetActionType(bool isReferenceTransaction, TransactionStatus status)
    {
        if (status == TransactionStatus.Cancelled)
        {
            return "Cancellation";
        }

        return isReferenceTransaction ? "Original" : "Adjustment";
    }
}

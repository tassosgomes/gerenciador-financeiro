using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Transaction;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Transaction;

public class ListTransactionsByAccountQueryHandler : IQueryHandler<ListTransactionsByAccountQuery, IReadOnlyList<TransactionResponse>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<ListTransactionsByAccountQueryHandler> _logger;

    public ListTransactionsByAccountQueryHandler(
        ITransactionRepository transactionRepository,
        ILogger<ListTransactionsByAccountQueryHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TransactionResponse>> HandleAsync(ListTransactionsByAccountQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing transactions for account: {AccountId}", query.AccountId);

        var transactions = await _transactionRepository.GetByAccountIdAsync(query.AccountId, cancellationToken);

        return transactions.Adapt<IReadOnlyList<TransactionResponse>>();
    }
}
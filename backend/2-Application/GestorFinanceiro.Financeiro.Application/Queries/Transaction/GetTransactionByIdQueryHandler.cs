using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Transaction;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Transaction;

public class GetTransactionByIdQueryHandler : IQueryHandler<GetTransactionByIdQuery, TransactionResponse>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IEstablishmentRepository _establishmentRepository;
    private readonly ILogger<GetTransactionByIdQueryHandler> _logger;

    public GetTransactionByIdQueryHandler(
        ITransactionRepository transactionRepository,
        IEstablishmentRepository establishmentRepository,
        ILogger<GetTransactionByIdQueryHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _establishmentRepository = establishmentRepository;
        _logger = logger;
    }

    public async Task<TransactionResponse> HandleAsync(GetTransactionByIdQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting transaction by ID: {TransactionId}", query.TransactionId);

        var transaction = await _transactionRepository.GetByIdAsync(query.TransactionId, cancellationToken);
        if (transaction == null)
            throw new TransactionNotFoundException(query.TransactionId);

        var hasReceipt = await _establishmentRepository.GetByTransactionIdAsync(query.TransactionId, cancellationToken) != null;
        return transaction.Adapt<TransactionResponse>() with { HasReceipt = hasReceipt };
    }
}
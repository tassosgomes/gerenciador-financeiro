using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Transaction;

public class ListTransactionsQueryHandler : IQueryHandler<ListTransactionsQuery, PagedResult<TransactionResponse>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IValidator<ListTransactionsQuery> _validator;
    private readonly ILogger<ListTransactionsQueryHandler> _logger;

    public ListTransactionsQueryHandler(
        ITransactionRepository transactionRepository,
        IValidator<ListTransactionsQuery> validator,
        ILogger<ListTransactionsQueryHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<PagedResult<TransactionResponse>> HandleAsync(ListTransactionsQuery query, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        _logger.LogInformation("Listing transactions with filters. Page: {Page}, Size: {Size}", query.Page, query.Size);

        var queryable = _transactionRepository.Query().AsNoTracking();

        if (query.AccountId.HasValue)
        {
            queryable = queryable.Where(transaction => transaction.AccountId == query.AccountId.Value);
        }

        if (query.CategoryId.HasValue)
        {
            queryable = queryable.Where(transaction => transaction.CategoryId == query.CategoryId.Value);
        }

        if (query.Type.HasValue)
        {
            queryable = queryable.Where(transaction => transaction.Type == query.Type.Value);
        }

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(transaction => transaction.Status == query.Status.Value);
        }

        if (query.CompetenceDateFrom.HasValue)
        {
            queryable = queryable.Where(transaction => transaction.CompetenceDate >= query.CompetenceDateFrom.Value.Date);
        }

        if (query.CompetenceDateTo.HasValue)
        {
            queryable = queryable.Where(transaction => transaction.CompetenceDate <= query.CompetenceDateTo.Value.Date);
        }

        if (query.DueDateFrom.HasValue)
        {
            queryable = queryable.Where(transaction => transaction.DueDate.HasValue && transaction.DueDate.Value >= query.DueDateFrom.Value.Date);
        }

        if (query.DueDateTo.HasValue)
        {
            queryable = queryable.Where(transaction => transaction.DueDate.HasValue && transaction.DueDate.Value <= query.DueDateTo.Value.Date);
        }

        var total = await queryable.CountAsync(cancellationToken);
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / query.Size);

        var transactions = await queryable
            .OrderByDescending(transaction => transaction.CompetenceDate)
            .ThenByDescending(transaction => transaction.CreatedAt)
            .Skip((query.Page - 1) * query.Size)
            .Take(query.Size)
            .ToListAsync(cancellationToken);

        var response = transactions.Adapt<IReadOnlyList<TransactionResponse>>();
        var pagination = new PaginationMetadata(query.Page, query.Size, total, totalPages);

        return new PagedResult<TransactionResponse>(response, pagination);
    }
}

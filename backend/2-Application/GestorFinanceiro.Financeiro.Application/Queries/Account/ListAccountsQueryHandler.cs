using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Account;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Account;

public class ListAccountsQueryHandler : IQueryHandler<ListAccountsQuery, IReadOnlyList<AccountResponse>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<ListAccountsQueryHandler> _logger;

    public ListAccountsQueryHandler(
        IAccountRepository accountRepository,
        ILogger<ListAccountsQueryHandler> logger)
    {
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AccountResponse>> HandleAsync(ListAccountsQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Listing accounts with filters. IsActive: {IsActive}, Type: {Type}",
            query.IsActive,
            query.Type);

        var accounts = await _accountRepository.GetAllAsync(cancellationToken);

        if (query.IsActive.HasValue)
        {
            accounts = accounts
                .Where(account => account.IsActive == query.IsActive.Value)
                .ToList();
        }

        if (query.Type.HasValue)
        {
            accounts = accounts
                .Where(account => account.Type == query.Type.Value)
                .ToList();
        }

        return accounts.Adapt<IReadOnlyList<AccountResponse>>();
    }
}

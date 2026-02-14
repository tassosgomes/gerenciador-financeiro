using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Account;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Account;

public class GetAccountByIdQueryHandler : IQueryHandler<GetAccountByIdQuery, AccountResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<GetAccountByIdQueryHandler> _logger;

    public GetAccountByIdQueryHandler(
        IAccountRepository accountRepository,
        ILogger<GetAccountByIdQueryHandler> logger)
    {
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<AccountResponse> HandleAsync(GetAccountByIdQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting account by ID: {AccountId}", query.AccountId);

        var account = await _accountRepository.GetByIdAsync(query.AccountId, cancellationToken);
        if (account == null)
            throw new AccountNotFoundException(query.AccountId);

        return account.Adapt<AccountResponse>();
    }
}
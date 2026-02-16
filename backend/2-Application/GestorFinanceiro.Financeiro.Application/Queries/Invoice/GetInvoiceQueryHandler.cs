using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Domain.Service;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Invoice;

public class GetInvoiceQueryHandler : IQueryHandler<GetInvoiceQuery, InvoiceResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly CreditCardDomainService _creditCardDomainService;
    private readonly IValidator<GetInvoiceQuery> _validator;
    private readonly ILogger<GetInvoiceQueryHandler> _logger;

    public GetInvoiceQueryHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        CreditCardDomainService creditCardDomainService,
        IValidator<GetInvoiceQuery> validator,
        ILogger<GetInvoiceQueryHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _creditCardDomainService = creditCardDomainService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<InvoiceResponse> HandleAsync(GetInvoiceQuery query, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        _logger.LogInformation(
            "Getting invoice for account {AccountId}, month {Month}/{Year}",
            query.AccountId,
            query.Month,
            query.Year);

        var account = await _accountRepository.GetByIdAsync(query.AccountId, cancellationToken);
        if (account == null)
        {
            throw new AccountNotFoundException(query.AccountId);
        }

        if (account.CreditCard == null)
        {
            throw new InvalidCreditCardConfigException("Conta não é um cartão de crédito.");
        }

        var (start, end) = _creditCardDomainService.CalculateInvoicePeriod(
            account.CreditCard.ClosingDay,
            query.Month,
            query.Year);

        var transactions = await _transactionRepository.GetByAccountAndPeriodAsync(
            account.Id,
            start,
            end,
            cancellationToken);

        var totalAmount = _creditCardDomainService.CalculateInvoiceTotal(transactions);

        var previousBalance = account.Balance > 0 ? account.Balance : 0;
        var amountDue = Math.Max(totalAmount - previousBalance, 0);

        var dueDate = new DateTime(query.Year, query.Month, account.CreditCard.DueDay);

        var transactionDtos = transactions.Select(t => new InvoiceTransactionDto(
            t.Id,
            t.Description,
            t.Amount,
            t.Type,
            t.CompetenceDate,
            t.InstallmentNumber,
            t.TotalInstallments
        )).ToList();

        _logger.LogInformation(
            "Invoice calculated for account {AccountId}: Total={TotalAmount}, PreviousBalance={PreviousBalance}, AmountDue={AmountDue}",
            account.Id,
            totalAmount,
            previousBalance,
            amountDue);

        return new InvoiceResponse(
            account.Id,
            account.Name,
            query.Month,
            query.Year,
            start,
            end,
            dueDate,
            totalAmount,
            previousBalance,
            amountDue,
            transactionDtos
        );
    }
}

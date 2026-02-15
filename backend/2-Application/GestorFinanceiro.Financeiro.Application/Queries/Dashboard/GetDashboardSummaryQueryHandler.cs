using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Dashboard;

public class GetDashboardSummaryQueryHandler : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryResponse>
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ILogger<GetDashboardSummaryQueryHandler> _logger;

    public GetDashboardSummaryQueryHandler(
        IDashboardRepository dashboardRepository,
        ILogger<GetDashboardSummaryQueryHandler> logger)
    {
        _dashboardRepository = dashboardRepository;
        _logger = logger;
    }

    public async Task<DashboardSummaryResponse> HandleAsync(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting dashboard summary for {Month}/{Year}",
            request.Month,
            request.Year);

        var totalBalance = await _dashboardRepository.GetTotalBalanceAsync(cancellationToken);
        var monthlyIncome = await _dashboardRepository.GetMonthlyIncomeAsync(
            request.Month,
            request.Year,
            cancellationToken);
        var monthlyExpenses = await _dashboardRepository.GetMonthlyExpensesAsync(
            request.Month,
            request.Year,
            cancellationToken);
        var creditCardDebt = await _dashboardRepository.GetCreditCardDebtAsync(cancellationToken);

        return new DashboardSummaryResponse(
            totalBalance,
            monthlyIncome,
            monthlyExpenses,
            Math.Abs(creditCardDebt));
    }
}

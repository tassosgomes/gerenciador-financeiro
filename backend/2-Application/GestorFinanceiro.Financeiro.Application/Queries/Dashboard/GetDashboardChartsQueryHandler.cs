using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Dashboard;

public class GetDashboardChartsQueryHandler : IQueryHandler<GetDashboardChartsQuery, DashboardChartsResponse>
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ILogger<GetDashboardChartsQueryHandler> _logger;

    public GetDashboardChartsQueryHandler(
        IDashboardRepository dashboardRepository,
        ILogger<GetDashboardChartsQueryHandler> logger)
    {
        _dashboardRepository = dashboardRepository;
        _logger = logger;
    }

    public async Task<DashboardChartsResponse> HandleAsync(
        GetDashboardChartsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting dashboard charts data for {Month}/{Year}",
            request.Month,
            request.Year);

        var revenueVsExpense = await _dashboardRepository.GetRevenueVsExpenseAsync(
            request.Month,
            request.Year,
            cancellationToken);

        var expenseByCategory = await _dashboardRepository.GetExpenseByCategoryAsync(
            request.Month,
            request.Year,
            cancellationToken);

        return new DashboardChartsResponse(revenueVsExpense, expenseByCategory);
    }
}

using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Queries.Dashboard;

public record GetDashboardSummaryQuery(
    int Month,
    int Year
) : IQuery<DashboardSummaryResponse>;

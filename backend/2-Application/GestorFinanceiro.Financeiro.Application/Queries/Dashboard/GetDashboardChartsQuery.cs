using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Queries.Dashboard;

public record GetDashboardChartsQuery(
    int Month,
    int Year
) : IQuery<DashboardChartsResponse>;

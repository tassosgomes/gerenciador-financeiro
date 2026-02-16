namespace GestorFinanceiro.Financeiro.Application.Dtos;

public record DashboardSummaryResponse(
    decimal TotalBalance,
    decimal MonthlyIncome,
    decimal MonthlyExpenses,
    decimal CreditCardDebt,
    decimal? TotalCreditLimit,
    decimal? CreditUtilizationPercent
);

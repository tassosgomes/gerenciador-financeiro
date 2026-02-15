namespace GestorFinanceiro.Financeiro.Domain.Dto;

public record MonthlyComparisonDto(
    string Month,
    decimal Income,
    decimal Expenses
);

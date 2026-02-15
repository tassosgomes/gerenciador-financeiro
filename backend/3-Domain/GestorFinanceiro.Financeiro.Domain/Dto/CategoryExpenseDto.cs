namespace GestorFinanceiro.Financeiro.Domain.Dto;

public record CategoryExpenseDto(
    Guid CategoryId,
    string CategoryName,
    decimal Total,
    decimal Percentage
);

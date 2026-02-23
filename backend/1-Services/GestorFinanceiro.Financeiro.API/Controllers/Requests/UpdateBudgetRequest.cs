namespace GestorFinanceiro.Financeiro.API.Controllers.Requests;

public record UpdateBudgetRequest(
    string Name,
    decimal Percentage,
    List<Guid> CategoryIds,
    bool IsRecurrent
);

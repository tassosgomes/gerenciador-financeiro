namespace GestorFinanceiro.Financeiro.API.Controllers.Requests;

public record CreateBudgetRequest(
    string Name,
    decimal Percentage,
    int ReferenceYear,
    int ReferenceMonth,
    List<Guid> CategoryIds,
    bool IsRecurrent = false
);

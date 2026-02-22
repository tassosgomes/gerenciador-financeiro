namespace GestorFinanceiro.Financeiro.Infra.Model;

public class BudgetCategoryLink
{
    public Guid BudgetId { get; set; }
    public Guid CategoryId { get; set; }
    public short ReferenceYear { get; set; }
    public short ReferenceMonth { get; set; }
}